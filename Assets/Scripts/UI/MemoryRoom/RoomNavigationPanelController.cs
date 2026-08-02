using System;
using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using GameName.UI.Shared;

namespace GameName.UI.MemoryRoom
{
    // 이동 패널의 입력 처리와 Core 연동을 담당한다. 인접 노드 목록, 사다리
    // 잠김 여부, 이동 비용은 전부 그래프/복원 트래커/이동 처리기에 물어서
    // 얻는다 — 이 타입이 직접 계산하는 값은 하나도 없다.
    //
    // 이동 완료 이벤트를 구독해서만 목록을 다시 그린다(폴링하지 않는다). 실패는
    // Move()의 반환값으로 그 자리에서 바로 알 수 있으므로 별도 이벤트가 필요
    // 없다.
    public sealed class RoomNavigationPanelController : IDisposable
    {
        private readonly RoomNavigationPanelView _view;
        private readonly MemoryRoomGraph _graph;
        private readonly IMemoryRoomRestorationTracker _restorationTracker;
        private readonly IMentalityGauge _mentalityGauge;
        private readonly MemoryRoomMovementProcessor _movementProcessor;
        private readonly IPlayerLocation _playerLocation;
        private readonly IReadOnlyList<MemoryRoomId> _roomIds;

        private readonly IDisposable _moveSubscription;
        private readonly IDisposable _restoredSubscription;
        private readonly IDisposable _mentalitySubscription;

        public RoomNavigationPanelController(
            RoomNavigationPanelView view,
            MemoryRoomGraph graph,
            IMemoryRoomRestorationTracker restorationTracker,
            IMentalityGauge mentalityGauge,
            MemoryRoomMovementProcessor movementProcessor,
            IPlayerLocation playerLocation,
            IReadOnlyList<MemoryRoomId> roomIds,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _restorationTracker = restorationTracker ?? throw new ArgumentNullException(nameof(restorationTracker));
            _mentalityGauge = mentalityGauge ?? throw new ArgumentNullException(nameof(mentalityGauge));
            _movementProcessor = movementProcessor ?? throw new ArgumentNullException(nameof(movementProcessor));
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
            _roomIds = roomIds ?? throw new ArgumentNullException(nameof(roomIds));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.MoveRequested += OnMoveRequested;
            _moveSubscription = eventBus.Subscribe<MemoryRoomMoveCompletedEvent>(_ => Refresh());
            _restoredSubscription = eventBus.Subscribe<MemoryRoomRestoredEvent>(_ => Refresh());
            _mentalitySubscription = eventBus.Subscribe<MentalityChangedEvent>(_ => Refresh());

            Refresh();
        }

        private void OnMoveRequested(MemoryGraphNodeId to)
        {
            var result = _movementProcessor.Move(to);

            // 성공하면 MemoryRoomMoveCompletedEvent 구독으로 Refresh()가 이미
            // 호출된다 — 여기서 다시 부르면 같은 렌더링이 중복된다. 실패는
            // 이벤트가 발행되지 않으므로 실패 메시지만 직접 갱신한다.
            _view.SetMoveFailureMessage(result.Succeeded ? null : DescribeFailure(result.FailureReason.Value));
        }

        private void Refresh()
        {
            var current = _playerLocation.Current;

            if (!_graph.TryGetNode(current, out var currentNode))
                return;

            var isCurrentRoomRestored =
                CurrentRoomResolver.TryResolve(current, _roomIds, out var currentRoomId) &&
                _restorationTracker.IsRestored(currentRoomId);
            _view.SetCurrentRoom(
                $"{current.Value} ({MemoryGraphNodeTypeDisplay.Label(currentNode.Type)})", isCurrentRoomRestored);

            _view.SetMentality(_mentalityGauge.CurrentValue, _mentalityGauge.MaxValue);
            _view.SetMentalityNotice(_mentalityGauge.CanAct
                ? null
                : "정신력이 없습니다. 분석실 · 조향실에서는 더 이상 행동할 수 없습니다. 계단으로 나가야 합니다.");

            var neighborIds = _graph.GetNeighborIds(current);
            var rows = new List<NeighborRowData>(neighborIds.Count);
            foreach (var neighborId in neighborIds)
            {
                if (!_graph.TryGetNode(neighborId, out var neighborNode))
                    continue;

                var isLocked = _graph.TryGetLadderLowerRoom(current, neighborId, out var lowerRoomId) &&
                    !_restorationTracker.IsRestored(lowerRoomId);
                var cost = _movementProcessor.PreviewCost(neighborId);

                rows.Add(new NeighborRowData(neighborId, neighborNode.Type, isLocked, cost));
            }

            _view.SetNeighbors(rows);
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
            _view.MoveRequested -= OnMoveRequested;
            _moveSubscription.Dispose();
            _restoredSubscription.Dispose();
            _mentalitySubscription.Dispose();
        }
    }
}
