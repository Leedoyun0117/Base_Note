using System;
using System.Collections.Generic;
using GameName.Core.Commissions;
using GameName.Core.Events;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using GameName.UI.Shared;

namespace GameName.UI.MemoryRoom
{
    // 기억 방 화면의 이동 수단을 지도로 제공한다. 예전의 "갈 수 있는 곳" 목록
    // (RoomNavigationPanelView.SetNeighbors)을 완전히 대체한다 — 같은 정보
    // (어디로 갈 수 있는지)를 목록과 지도 둘 다로 중복 표현할 이유가 없기
    // 때문이다. 다만 현재 위치/정신력 표시(RoomNavigationPanelView의 나머지
    // 부분)는 지도와 무관한 별개 정보라 그대로 재사용한다 — 그 View는 그대로
    // 두고 SetNeighbors만 안 부를 뿐이다.
    //
    // 인접하지 않은 노드도 클릭할 수 있게 열어 둔다: 지도는 이동 규칙을
    // 모르므로 "여기서 저기까지 갈 수 있는가"를 스스로 판단해 클릭을 막지
    // 않는다. 대신 클릭하면 그 자리에서 실제로 이동을 시도하고, 실패하면
    // MemoryRoomMovementProcessor가 이미 구분해 둔 사유(NoConnection 등)를
    // 그대로 보여준다 — 목록 기반 UI가 항상 인접 노드만 보여줬던 것과 달리
    // 지도는 전체 구조를 보여주므로 이 케이스가 처음 생기지만, 새로운 실패
    // 처리 경로를 만들 필요 없이 기존 Move() 결과 하나로 충분하다.
    //
    // 딱 하나, 이탈 노드(계단과는 별개의 공간, MemoryGraphNodeType.Exit) 클릭만
    // 예외다 — 일반 이동이 아니라 "이 기억에서 나가겠다"는 결심이므로 인접
    // 여부를 따지지 않고 곧장 계단(기억 진입/이탈 지점)으로 돌아간다
    // (CommissionSession.TryReturnToEntryPoint). 계단 자체는 평범한 허브
    // 노드로 남아 일반 이동만 한다. 그 뒤 실제 이탈 확인은 FlowOverlay가
    // 맡는다 — 여기서는 위치만 옮길 뿐이다.
    public sealed class MemoryRoomMapNavigationController : IDisposable
    {
        private readonly RoomNavigationPanelView _statusView;
        private readonly MemoryMapView _mapView;
        private readonly IMemoryRoomGraph _graph;
        private readonly IMemoryRoomRestorationTracker _restorationTracker;
        private readonly IMentalityGauge _mentalityGauge;
        private readonly IMentalityCostSettings _costSettings;
        private readonly MemoryRoomMovementProcessor _movementProcessor;
        private readonly IPlayerLocation _playerLocation;
        private readonly IReadOnlyList<MemoryRoomId> _roomIds;
        private readonly CommissionSession _commissionSession;
        private readonly MemoryGraphNodeId _memoryExitNodeId;

        private readonly IDisposable _moveSubscription;
        private readonly IDisposable _restoredSubscription;
        private readonly IDisposable _mentalitySubscription;

        public MemoryRoomMapNavigationController(
            RoomNavigationPanelView statusView,
            MemoryMapView mapView,
            IMemoryRoomGraph graph,
            IMemoryRoomRestorationTracker restorationTracker,
            IMentalityGauge mentalityGauge,
            IMentalityCostSettings costSettings,
            MemoryRoomMovementProcessor movementProcessor,
            IPlayerLocation playerLocation,
            IReadOnlyList<MemoryRoomId> roomIds,
            CommissionSession commissionSession,
            MemoryGraphNodeId memoryExitNodeId,
            IEventBus eventBus)
        {
            _statusView = statusView ?? throw new ArgumentNullException(nameof(statusView));
            _mapView = mapView ?? throw new ArgumentNullException(nameof(mapView));
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _restorationTracker = restorationTracker ?? throw new ArgumentNullException(nameof(restorationTracker));
            _mentalityGauge = mentalityGauge ?? throw new ArgumentNullException(nameof(mentalityGauge));
            _costSettings = costSettings ?? throw new ArgumentNullException(nameof(costSettings));
            _movementProcessor = movementProcessor ?? throw new ArgumentNullException(nameof(movementProcessor));
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
            _roomIds = roomIds ?? throw new ArgumentNullException(nameof(roomIds));
            _commissionSession = commissionSession ?? throw new ArgumentNullException(nameof(commissionSession));
            _memoryExitNodeId = memoryExitNodeId;
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _mapView.NodeSelected += OnNodeSelected;
            _moveSubscription = eventBus.Subscribe<MemoryRoomMoveCompletedEvent>(_ => Refresh());
            _restoredSubscription = eventBus.Subscribe<MemoryRoomRestoredEvent>(_ => Refresh());
            _mentalitySubscription = eventBus.Subscribe<MentalityChangedEvent>(_ => Refresh());

            Refresh();
        }

        private void OnNodeSelected(MemoryGraphNodeId nodeId)
        {
            if (nodeId.Equals(_memoryExitNodeId))
            {
                _commissionSession.TryReturnToEntryPoint();
                // 성공하면 MemoryRoomMoveCompletedEvent 구독으로 Refresh()가
                // 이미 호출된다. 이미 계단에 있었다면 위치가 안 바뀌므로 그
                // 이벤트조차 나가지 않지만, 그 경우도 다시 그릴 것이 없다.
                return;
            }

            // 이동 비용 미리보기를 지도 위에서 보여준다 — 실제 이동(Move) 전에
            // 같은 계산을 먼저 노출해 "지금 이 노드로 가면 얼마가 드는지"를
            // 알 수 있게 한다. 계산은 항상 처리기(PreviewCost) 하나만 신뢰한다.
            var previewCost = _movementProcessor.PreviewCost(nodeId);
            _mapView.SetSelectionInfo($"이동 비용: 정신력 {previewCost}");

            var result = _movementProcessor.Move(nodeId);

            // 성공하면 MemoryRoomMoveCompletedEvent 구독으로 Refresh()가 이미
            // 호출된다 — 여기서 다시 부르면 같은 렌더링이 중복된다.
            _statusView.SetMoveFailureMessage(result.Succeeded ? null : DescribeFailure(result.FailureReason.Value));
        }

        private void Refresh()
        {
            var current = _playerLocation.Current;
            if (!_graph.TryGetNode(current, out var currentNode))
                return;

            var isCurrentRoomRestored =
                CurrentRoomResolver.TryResolve(current, _roomIds, out var currentRoomId) &&
                _restorationTracker.IsRestored(currentRoomId);
            _statusView.SetCurrentRoom(
                $"{current.Value} ({MemoryGraphNodeTypeDisplay.Label(currentNode.Type)})", isCurrentRoomRestored);

            _statusView.SetMentality(_mentalityGauge.CurrentValue, _mentalityGauge.MaxValue);
            _statusView.SetMentalityNotice(_mentalityGauge.CanAct
                ? null
                : "정신력이 없습니다. 분석실 · 조향실에서는 더 이상 행동할 수 없습니다. 지도의 '나가기'를 누르면 언제든 현실로 돌아갈 수 있습니다.");

            var affordability = MentalityAffordabilityCalculator.Calculate(_mentalityGauge, _costSettings);
            _statusView.SetAvailableActions(DescribeAvailableActions(affordability));

            var nodes = MemoryMapDataBuilder.BuildNodes(
                _graph, _restorationTracker, _movementProcessor, current, selectedRoomId: null, _memoryExitNodeId);
            var connections = MemoryMapDataBuilder.BuildConnections(_graph, _restorationTracker);
            _mapView.SetMap(nodes, connections);
        }

        // 이동은 정신력이 0이어도 면제되어 항상 가능하므로 목록에 조건 없이
        // 넣는다 — 잔량으로 실제로 갈릴 수 있는 항목만 따로 붙인다.
        private static string DescribeAvailableActions(MentalityAffordability affordability)
        {
            var actions = new List<string> { "이동" };
            if (affordability.CanBasicAnalyze) actions.Add("일반 분석");
            if (affordability.CanAdvancedAnalyze) actions.Add("고급 분석");
            if (affordability.CanCraftAmpoule) actions.Add("조향");

            return "지금 가능한 행동: " + string.Join(" · ", actions);
        }

        private static string DescribeFailure(MemoryGraphMoveFailureReason reason)
        {
            switch (reason)
            {
                case MemoryGraphMoveFailureReason.NoConnection: return "그 곳으로 이어지는 길이 없습니다.";
                case MemoryGraphMoveFailureReason.LadderLocked: return "사다리가 잠겨 있습니다.";
                case MemoryGraphMoveFailureReason.InsufficientMentality: return "정신력이 부족합니다.";
                default: return "이동에 실패했습니다.";
            }
        }

        public void Dispose()
        {
            _mapView.NodeSelected -= OnNodeSelected;
            _moveSubscription.Dispose();
            _restoredSubscription.Dispose();
            _mentalitySubscription.Dispose();
        }
    }
}
