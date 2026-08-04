using System;
using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.Mentality;

namespace GameName.Core.MemoryRooms
{
    // 기억 방 그래프 위에서 실제 이동 시도를 처리한다.
    // 그래프(구조), 복원 상태(진행 상황), 현재 위치(IPlayerLocationMover)는 각자
    // 자기 몫만 알고 있고, 이 타입이 그것들을 합쳐 이동 가능 여부와 정신력 비용을
    // 계산한다. 출발지는 호출부가 넘기지 않는다 — IPlayerLocation이 "지금 어디에
    // 있는가"의 단일 진실 원천이므로, 호출부가 실제와 다른 출발지를 주장해서
    // 불가능한 이동이 통과하는 일 자체가 생길 수 없다. 시향 판정이나 조향에
    // 대해서도 전혀 알지 못한다 — 복원 여부는 IMemoryRoomRestorationTracker에게
    // 물어볼 뿐이다.
    //
    // IResettable도 구현한다 — 어떤 방을 이미 가 봤는지(_visitedRoomIds)는 이번
    // 의뢰 한 번의 탐험 진행 상태이지, 다음 의뢰로 넘어가도 유지될 값이 아니다.
    // 그 초기화 권한은 CommissionSession만 받는다(다른 IResettable 구현체와
    // 같은 이유).
    public sealed class MemoryRoomMovementProcessor : IResettable
    {
        private readonly IMemoryRoomGraph _graph;
        private readonly IMemoryRoomRestorationTracker _restorationTracker;
        private readonly IMentalityGauge _mentalityGauge;
        private readonly IMentalityCostSettings _costSettings;
        private readonly IPlayerLocationMover _playerLocation;
        private readonly IEventBus _eventBus;
        private readonly HashSet<MemoryRoomId> _visitedRoomIds = new HashSet<MemoryRoomId>();

        public MemoryRoomMovementProcessor(
            IMemoryRoomGraph graph,
            IMemoryRoomRestorationTracker restorationTracker,
            IMentalityGauge mentalityGauge,
            IMentalityCostSettings costSettings,
            IPlayerLocationMover playerLocation,
            IEventBus eventBus)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _restorationTracker = restorationTracker ?? throw new ArgumentNullException(nameof(restorationTracker));
            _mentalityGauge = mentalityGauge ?? throw new ArgumentNullException(nameof(mentalityGauge));
            _costSettings = costSettings ?? throw new ArgumentNullException(nameof(costSettings));
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        // 실제로 이동을 시도하지 않고 "지금 이 목적지로 가면 얼마가 드는가"만
        // 미리 알려준다. 화면이 이동 비용을 스스로 다시 계산(노드 종류를 보고
        // 조건문을 재현하는 등)하지 않고 항상 이 계산 하나만 신뢰하게 하기
        // 위함이다 — 0-정신력 면제처럼 화면이 놓치기 쉬운 예외까지 여기 하나에
        // 모여 있다.
        public int PreviewCost(MemoryGraphNodeId to)
        {
            var from = _playerLocation.Current;

            if (!_graph.TryGetNode(from, out var fromNode))
                throw new InvalidOperationException($"현재 위치({from})가 그래프에 없는 노드다.");
            if (!_graph.TryGetNode(to, out var toNode))
                throw new ArgumentException($"그래프에 없는 노드({to})다.", nameof(to));

            return CalculateEffectiveCost(fromNode, toNode);
        }

        public MemoryGraphMoveResult Move(MemoryGraphNodeId to)
        {
            var from = _playerLocation.Current;

            // 현재 위치가 이 그래프에 없다는 것은 호출부의 잘못된 인자가 아니라
            // 위치 추적기와 그래프가 서로 어긋난 내부 상태 불일치이므로, 인자
            // 예외가 아니라 상태 예외로 구분해서 드러낸다.
            if (!_graph.TryGetNode(from, out var fromNode))
                throw new InvalidOperationException($"현재 위치({from})가 그래프에 없는 노드다.");
            if (!_graph.TryGetNode(to, out var toNode))
                throw new ArgumentException($"그래프에 없는 노드({to})다.", nameof(to));

            // 지금 서 있는 방은 이동 성공 여부와 무관하게 이미 "가 본 곳"이다.
            // Move()로 도착한 적 없는 커미션 시작 지점(예: 첫 기억 방)도 이렇게
            // 표시해 둬야, 더 안쪽까지 갔다가 그 방으로 되돌아올 때 다시 비용이
            // 청구되지 않는다.
            if (fromNode.Type == MemoryGraphNodeType.MemoryRoom)
                _visitedRoomIds.Add(new MemoryRoomId(fromNode.Id.Value));

            if (_graph.TryGetLadderLowerRoom(from, to, out var lowerRoomId))
            {
                if (!_restorationTracker.IsRestored(lowerRoomId))
                    return MemoryGraphMoveResult.Failure(MemoryGraphMoveFailureReason.LadderLocked);
            }
            else if (!_graph.AreOpenlyConnected(from, to))
            {
                return MemoryGraphMoveResult.Failure(MemoryGraphMoveFailureReason.NoConnection);
            }

            var cost = CalculateNominalCost(fromNode, toNode);

            // 정신력이 0이면 기억 방 이동 비용이 면제된다. 이 면제는 출구 쪽으로
            // 가는 이동에만 적용되는 게 아니라, 기억 방 사이의 모든 방향에 동일하게
            // 적용된다 — 어느 쪽으로 움직이든 남은 정신력이 0이면 더 잃을 것이
            // 없기 때문이다.
            //
            // 무료 이동이 안전한 이유는 "정신력이 0이면 아무것도 못 한다"가 아니다
            // — 시향은 정신력과 무관하게 항상 가능하도록 확정되어 있다(복원이
            // 정신력을 회복하는 유일한 수단이므로, 시향까지 막으면 0에서 영영
            // 벗어날 수 없는 소프트락이 생긴다). 대신 다음 세 가지로 안전하다:
            // (1) 정신력 0에서는 조향(AmpouleCraftingProcessor)이 막혀 새 앰플을
            // 만들 수 없다. (2) 따라서 자유롭게 돌아다니며 할 수 있는 시향은
            // 이미 소지한 앰플 수만큼으로 유한하다 — 무한히 반복할 방법이 없다.
            // (3) 시향으로 복원에 성공하면 정신력이 회복되어 이 면제 상태 자체를
            // 벗어난다. 분석(ClueAnalyzer)과 단서 습득 관련 상호작용 중 정신력을
            // 쓰는 것들은 여전히 IMentalityGauge.CanAct == false일 때 각자 스스로
            // 차단해야 한다 — 이 처리기는 그 차단을 대신 해주지 않는다. 정신력이
            // 남아 있는 동안에는 이 면제가 전혀 적용되지 않으므로, "정신력이 있을
            // 때 자유롭게 돌아다니는 것"은 여전히 정상적으로 비용을 낸다.
            if (cost > 0 && _mentalityGauge.CurrentValue > 0)
            {
                if (!_mentalityGauge.Consume(cost))
                    return MemoryGraphMoveResult.Failure(MemoryGraphMoveFailureReason.InsufficientMentality);
            }

            // 여기까지 왔다는 것은 이동이 확정되었다는 뜻이다. 위치 갱신과 이벤트
            // 발행은 반드시 성공 경로에서만 일어나야 하므로, 실패로 반환하는 모든
            // return문보다 뒤에 둔다. 출발지와 목적지가 같다면 실제로는 위치가
            // 바뀐 게 아니므로 이벤트를 발행하지 않는다.
            _playerLocation.MoveTo(to);
            if (!to.Equals(from))
                _eventBus.Publish(new MemoryRoomMoveCompletedEvent(from, to));

            // 방금 도착한 곳이 기억 방이면 "가 본 곳"으로 기록한다 — 그래야
            // 다음에 같은 방으로 다시 들어올 때 CalculateNominalCost가 무료로
            // 처리한다. 실패한 시도에서는 여기까지 오지 않으므로 실제로 들어간
            // 적 없는 방이 방문 처리되는 일은 없다.
            if (toNode.Type == MemoryGraphNodeType.MemoryRoom)
                _visitedRoomIds.Add(new MemoryRoomId(toNode.Id.Value));

            return MemoryGraphMoveResult.Success();
        }

        public void Reset() => _visitedRoomIds.Clear();

        // 그 방에 한 번이라도 가 본 적이 있는지 읽기만 한다 — 지도가 "아직
        // 가보지 않은 방"을 다르게 표시하기 위한 조회 전용 메서드다. 이동
        // 비용 계산에 쓰는 것과 같은 상태를 그대로 노출할 뿐, 새 상태를
        // 만들지 않는다.
        public bool HasVisited(MemoryRoomId roomId) => _visitedRoomIds.Contains(roomId);

        // 노드 종류만으로 정해지는 "명목상" 비용. 0-정신력 면제는 여기에 넣지
        // 않는다 — 그 면제는 "지금 정신력이 얼마인가"라는 순간의 상태에 달려
        // 있어서 노드 종류만 보는 이 계산과는 층위가 다르기 때문이다.
        //
        // 목적지 방에 이미 한 번이라도 가 본 적이 있으면(방문했거나, 다른
        // 수단으로 이미 복원되어 있으면) 비용이 없다 — 정신력 비용은 "아직
        // 가보지 않은 곳을 처음 탐색하는" 대가이지, 이미 한 번 가 본 곳을
        // 다시 오가는 것까지 매번 청구할 이유가 없다. 그래서 한 번 들어간
        // 방부터는 완전히 복원했는지와 무관하게 몇 번을 오가도 그 방으로의
        // 이동만큼은 계속 무료다.
        private int CalculateNominalCost(MemoryGraphNode fromNode, MemoryGraphNode toNode)
        {
            var bothAreMemoryRooms =
                fromNode.Type == MemoryGraphNodeType.MemoryRoom && toNode.Type == MemoryGraphNodeType.MemoryRoom;
            if (!bothAreMemoryRooms)
                return 0;

            var toRoomId = new MemoryRoomId(toNode.Id.Value);
            if (_restorationTracker.IsRestored(toRoomId) || _visitedRoomIds.Contains(toRoomId))
                return 0;

            return _costSettings.MemoryRoomMoveCost;
        }

        // Move()가 실제로 청구하는 것과 같은 값을 미리 계산한다 — 0-정신력
        // 면제를 포함해서다. Move()의 소모 분기(cost > 0 && CurrentValue > 0)와
        // 정확히 같은 조건이어야 하므로, 두 곳이 각자 조건을 반복해 적다가
        // 어긋나지 않도록 이 하나의 메서드로 모은다.
        private int CalculateEffectiveCost(MemoryGraphNode fromNode, MemoryGraphNode toNode)
        {
            var nominalCost = CalculateNominalCost(fromNode, toNode);
            return _mentalityGauge.CurrentValue > 0 ? nominalCost : 0;
        }
    }
}
