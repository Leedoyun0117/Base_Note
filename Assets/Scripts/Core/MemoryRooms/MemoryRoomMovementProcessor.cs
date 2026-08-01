using System;
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
    public sealed class MemoryRoomMovementProcessor
    {
        private readonly MemoryRoomGraph _graph;
        private readonly IMemoryRoomRestorationTracker _restorationTracker;
        private readonly IMentalityGauge _mentalityGauge;
        private readonly IMentalityCostSettings _costSettings;
        private readonly IPlayerLocationMover _playerLocation;

        public MemoryRoomMovementProcessor(
            MemoryRoomGraph graph,
            IMemoryRoomRestorationTracker restorationTracker,
            IMentalityGauge mentalityGauge,
            IMentalityCostSettings costSettings,
            IPlayerLocationMover playerLocation)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _restorationTracker = restorationTracker ?? throw new ArgumentNullException(nameof(restorationTracker));
            _mentalityGauge = mentalityGauge ?? throw new ArgumentNullException(nameof(mentalityGauge));
            _costSettings = costSettings ?? throw new ArgumentNullException(nameof(costSettings));
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
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

            if (_graph.TryGetLadderLowerRoom(from, to, out var lowerRoomId))
            {
                if (!_restorationTracker.IsRestored(lowerRoomId))
                    return MemoryGraphMoveResult.Failure(MemoryGraphMoveFailureReason.LadderLocked);
            }
            else if (!_graph.AreOpenlyConnected(from, to))
            {
                return MemoryGraphMoveResult.Failure(MemoryGraphMoveFailureReason.NoConnection);
            }

            var bothAreMemoryRooms =
                fromNode.Type == MemoryGraphNodeType.MemoryRoom && toNode.Type == MemoryGraphNodeType.MemoryRoom;
            var cost = bothAreMemoryRooms ? _costSettings.MemoryRoomMoveCost : 0;

            // 정신력이 이미 바닥(0)이면 기억 방 이동도 무료로 전환한다. 그렇지
            // 않으면 이동 자체에 정신력이 필요해 플레이어가 출구로 돌아올 수단을
            // 잃고 영구히 갇힌다. 정신력이 남아 있는 동안에는 이 예외가 전혀
            // 적용되지 않으므로, "정신력이 있을 때 자유롭게 돌아다니는 것"은
            // 여전히 정상적으로 비용을 낸다.
            if (cost > 0 && _mentalityGauge.CurrentValue > 0)
            {
                if (!_mentalityGauge.Consume(cost))
                    return MemoryGraphMoveResult.Failure(MemoryGraphMoveFailureReason.InsufficientMentality);
            }

            // 여기까지 왔다는 것은 이동이 확정되었다는 뜻이다. 위치 갱신은 반드시
            // 성공 경로에서만 일어나야 하므로, 실패로 반환하는 모든 return문보다
            // 뒤에 둔다.
            _playerLocation.MoveTo(to);
            return MemoryGraphMoveResult.Success();
        }
    }
}
