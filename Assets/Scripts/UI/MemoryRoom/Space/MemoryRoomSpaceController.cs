using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.UI.Shared;

namespace GameName.UI.MemoryRoom.Space
{
    // 씬에 그려진 기억 방과 Core 사이를 잇는 단 하나의 지점.
    //
    // 씬 오브젝트는 Core 처리기를 전혀 참조하지 않는다 — 단서 오브젝트는 자기
    // 식별자만, 출입구 오브젝트는 목적지 노드 식별자만 들고 있고, 눌렸다는
    // 사실만 알린다. 그 뒤의 판단(집을 수 있는가, 갈 수 있는가, 얼마가 드는가)은
    // 전부 여기서 Core에 물어 얻은 결론이다.
    //
    // 이 컨트롤러도 규칙을 계산하지 않는다: 방에 무엇이 남아 있는지는
    // IMemoryRoomClueTracker에게, 어디로 이어지는지는 IMemoryRoomGraph에게,
    // 이동 가능 여부는 MemoryRoomMovementProcessor에게 묻는다. 여기서 직접
    // 정하는 것은 "그 결과를 화면 어디에 놓을 것인가"(좌표)뿐이다.
    //
    // 방이 다시 그려지는 계기는 전부 Refresh 하나로 모인다(이동 완료, 버리기,
    // 습득). 계기마다 화면을 조금씩 다르게 손대기 시작하면 어느 경로에서는
    // 이전 방의 오브젝트가 남는 상태가 생기고, 그건 화면을 보고는 알아채기
    // 어려운 종류의 어긋남이다.
    public sealed class MemoryRoomSpaceController : IDisposable
    {
        private readonly MemoryRoomSpaceView _view;
        private readonly IPlayerLocation _playerLocation;
        private readonly IMemoryRoomGraph _graph;
        private readonly IMemoryRoomClueTracker _clueTracker;
        private readonly IReadOnlyList<MemoryRoomId> _roomIds;
        private readonly MemoryRoomMovementProcessor _movementProcessor;
        private readonly MemoryRoomLayout _layout;

        private readonly IDisposable _moveSubscription;
        private readonly IDisposable _clueDroppedSubscription;

        // 버린 자리는 이 컨트롤러가 들고 있지 않는다 — 방을 옮길 때마다
        // 사라져 단서가 제자리로 튀어 돌아가기 때문이다. 의뢰 하나만큼 살아
        // 있는 표시용 기록으로 따로 뺐다(DroppedCluePositions 주석 참고).
        private readonly DroppedCluePositions _droppedPositions;

        // 지금 방에 보이는 단서들의 공개 정보. 씬 오브젝트는 식별자만 알리므로,
        // 확대 화면에 넘길 정보를 여기서 되찾는다 — 씬 오브젝트가 ClueInfo를
        // 들고 있지 않아야 한다는 원칙을 지키면서도 화면이 필요한 것을 얻는
        // 방법이다.
        private readonly Dictionary<ClueId, ClueInfo> _visibleCluesById = new Dictionary<ClueId, ClueInfo>();

        // 단서를 눌렀다 — 확대 화면을 열 차례다. 확대 화면 자체는 이 컨트롤러가
        // 소유하지 않는다(씬과 UI Toolkit은 서로 다른 표시 수단이라, 둘을 잇는
        // 것은 한 단계 위인 화면 컨트롤러의 몫이다).
        public event Action<ClueInfo> ClueActivated;

        // 이동 실패 사유나 출입구 안내처럼 플레이어에게 보여줄 한 줄.
        public event Action<string> MessageChanged;

        public MemoryRoomSpaceController(
            MemoryRoomSpaceView view,
            MemoryRoomLayout layout,
            IPlayerLocation playerLocation,
            IMemoryRoomGraph graph,
            IMemoryRoomClueTracker clueTracker,
            IReadOnlyList<MemoryRoomId> roomIds,
            MemoryRoomMovementProcessor movementProcessor,
            DroppedCluePositions droppedPositions,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _clueTracker = clueTracker ?? throw new ArgumentNullException(nameof(clueTracker));
            _roomIds = roomIds ?? throw new ArgumentNullException(nameof(roomIds));
            _movementProcessor = movementProcessor ?? throw new ArgumentNullException(nameof(movementProcessor));
            _droppedPositions = droppedPositions ?? throw new ArgumentNullException(nameof(droppedPositions));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.Build(_layout);
            _view.ClueActivated += OnClueActivated;
            _view.ExitActivated += OnExitActivated;
            _view.ExitHoverChanged += OnExitHoverChanged;

            _moveSubscription = eventBus.Subscribe<MemoryRoomMoveCompletedEvent>(_ => Refresh());
            _clueDroppedSubscription = eventBus.Subscribe<ClueDroppedEvent>(OnClueDropped);

            Refresh();
        }

        // 확대 화면이 열려 있는 동안 방을 조작하지 못하게 막는다.
        public void SetInteractionEnabled(bool enabled) => _view.SetInteractionEnabled(enabled);

        public void Refresh()
        {
            // 계단처럼 공간이 만들어지지 않은 허브에 서 있으면 방을 통째로
            // 치운다. 감추는 것으로 그치지 않고 실제로 비우는 이유는
            // MemoryRoomSpaceView.HideRoom 주석에 적었다.
            if (!CurrentRoomResolver.TryResolve(_playerLocation.Current, _roomIds, out var roomId))
            {
                _visibleCluesById.Clear();
                _view.HideRoom();
                return;
            }

            _view.SetContents(BuildClueItems(roomId), BuildExitItems());
        }

        private IReadOnlyList<ClueSceneItem> BuildClueItems(MemoryRoomId roomId)
        {
            // 이미 집은 단서는 이 목록에 아예 오지 않는다 — 방에서 사라지는
            // 규칙은 추적기가 이미 지키고 있으므로 화면이 따로 거를 필요가 없다.
            var clueInfos = _clueTracker.GetAvailableClueInfos(roomId);

            _visibleCluesById.Clear();
            var items = new List<ClueSceneItem>(clueInfos.Count);
            foreach (var info in clueInfos)
            {
                _visibleCluesById[info.Id] = info;
                items.Add(new ClueSceneItem(
                    info.Id, info.Kind, CluePlacementLayout.PositionAt(_layout, info.Kind, DisplayPositionOf(info))));
            }

            return items;
        }

        // 자리는 저작 데이터가 정한다. 플레이어가 직접 버려 놓은 적이 있다면 그
        // 자리가 우선한다 — 자동 계산 값이 아니라 의도한 행동이기 때문이다.
        // 어느 쪽이든 방에 남은 단서 개수와는 무관해서, 옆 단서를 집거나 버려도
        // 이 단서는 움직이지 않는다.
        private CluePositionRatio DisplayPositionOf(ClueInfo info) =>
            _droppedPositions.TryGet(info.Id, out var droppedPosition) ? droppedPosition : info.AuthoredPosition;

        private IReadOnlyList<RoomExitSceneItem> BuildExitItems()
        {
            var current = _playerLocation.Current;
            var neighborIds = _graph.GetNeighborIds(current);

            // 종류를 먼저 전부 가려낸 다음에 자리를 정한다. 같은 종류가 몇 개인지
            // 알아야 가운데를 기준으로 대칭이 되게 나눠 세울 수 있는데, 한 번에
            // 훑으면서 자리까지 정하면 마지막 것을 만날 때까지 그 개수를 알 수
            // 없다 — 방 2처럼 사다리가 둘인 방이 실제로 있다.
            var kinds = new List<RoomExitKind>(neighborIds.Count);
            var doorCount = 0;
            var ladderCount = 0;

            foreach (var neighborId in neighborIds)
            {
                // 사다리인지 문인지는 화면이 짐작하지 않고 그래프에 물어본다.
                var isLadder = _graph.TryGetLadderLowerRoom(current, neighborId, out _);
                kinds.Add(isLadder ? RoomExitKind.Ladder : RoomExitKind.Door);

                if (isLadder)
                    ladderCount++;
                else
                    doorCount++;
            }

            var items = new List<RoomExitSceneItem>(neighborIds.Count);
            var doorIndex = 0;
            var ladderIndex = 0;

            for (var i = 0; i < neighborIds.Count; i++)
            {
                var kind = kinds[i];
                var isLadder = kind == RoomExitKind.Ladder;
                var index = isLadder ? ladderIndex++ : doorIndex++;
                var count = isLadder ? ladderCount : doorCount;

                items.Add(new RoomExitSceneItem(
                    neighborIds[i], kind, RoomExitLayout.PositionOf(_layout, kind, index, count),
                    DescribeNode(neighborIds[i])));
            }

            return items;
        }

        private void OnClueActivated(ClueId clueId)
        {
            if (_visibleCluesById.TryGetValue(clueId, out var info))
            {
                ClueActivated?.Invoke(info);
                return;
            }

            // 여기 오면 씬에 남아 있는 오브젝트와 이 컨트롤러가 아는 목록이
            // 어긋난 것이다. 조용히 넘기면 "눌러도 아무 일이 없다"로만 보여서
            // 원인을 찾을 길이 없어진다 — 실제로 이번 버그가 그렇게 숨어 있었다.
            MessageChanged?.Invoke("이 단서는 더 이상 이 방에 없습니다.");
        }

        private void OnExitActivated(MemoryGraphNodeId nodeId)
        {
            // 갈 수 없는 곳도 누를 수 있게 열어 두고 실패 사유를 그대로 보여준다
            // — 화면이 이동 규칙을 흉내 내 미리 막으면 규칙이 두 곳에 생긴다.
            var result = _movementProcessor.Move(nodeId);
            if (result.Succeeded)
            {
                // 성공하면 MemoryRoomMoveCompletedEvent 구독으로 Refresh가 이미
                // 불린다. 여기서 다시 부르면 같은 렌더링이 중복된다.
                MessageChanged?.Invoke(null);
                return;
            }

            MessageChanged?.Invoke(DescribeFailure(result.FailureReason.Value));
        }

        private void OnExitHoverChanged(MemoryGraphNodeId nodeId, bool hovered)
        {
            if (!hovered)
            {
                MessageChanged?.Invoke(null);
                return;
            }

            MessageChanged?.Invoke($"{DescribeNode(nodeId)}(으)로 이동");
        }

        private void OnClueDropped(ClueDroppedEvent dropped)
        {
            // 어느 자리에 내려놓을지는 DroppedCluePlacement가 정한다 — 플레이어
            // 몸에 가려지지 않고 이미 놓인 단서와도 겹치지 않는 자리다.
            //
            // 월드 좌표가 아니라 비율로 되돌려 기억한다 — 저작 위치와 같은
            // 단위로 두어야 나중에 방 크기가 바뀌어도 같은 자리를 가리킨다.
            // 자리를 정하지 못하더라도 방은 반드시 다시 그린다 — 버렸다는 사실
            // 자체는 이미 Core가 확정했으므로, 화면만 그 사실을 모르는 상태가
            // 남으면 안 된다.
            if (TryGetDroppedClue(dropped, out var droppedClue))
            {
                var position = DroppedCluePlacement.Resolve(
                    _layout, droppedClue.Kind, _view.PlayerX, OccupiedPositions(dropped, droppedClue.Kind));

                _droppedPositions.Record(dropped.ClueId, position);
            }

            Refresh();
        }

        // 버린 단서의 공개 정보. 종류(벽인가 바닥인가)를 알아야 놓을 자리를
        // 정할 수 있는데, 그 사실은 이벤트가 아니라 추적기가 들고 있다.
        private bool TryGetDroppedClue(ClueDroppedEvent dropped, out ClueInfo clue)
        {
            foreach (var info in _clueTracker.GetAvailableClueInfos(dropped.RoomId))
            {
                if (!info.Id.Equals(dropped.ClueId))
                    continue;

                clue = info;
                return true;
            }

            clue = null;
            return false;
        }

        // 그 방에 이미 놓여 있는 같은 종류 단서들의 자리. 방금 버린 단서 자신은
        // 뺀다 — 아직 자리가 정해지지 않았으므로 저작 위치가 자기 자리를 막는
        // 결과가 된다.
        private IReadOnlyList<CluePositionRatio> OccupiedPositions(ClueDroppedEvent dropped, ClueKind kind)
        {
            var positions = new List<CluePositionRatio>();
            foreach (var info in _clueTracker.GetAvailableClueInfos(dropped.RoomId))
            {
                if (info.Id.Equals(dropped.ClueId) || info.Kind != kind)
                    continue;

                positions.Add(DisplayPositionOf(info));
            }

            return positions;
        }

        private string DescribeNode(MemoryGraphNodeId nodeId) =>
            _graph.TryGetNode(nodeId, out var node)
                ? MemoryGraphNodeTypeDisplay.Label(node.Type)
                : nodeId.Value;

        private static string DescribeFailure(MemoryGraphMoveFailureReason reason)
        {
            switch (reason)
            {
                case MemoryGraphMoveFailureReason.NoConnection: return "그 곳으로 이어지는 길이 없습니다.";
                default: return "이동에 실패했습니다.";
            }
        }

        public void Dispose()
        {
            _view.ClueActivated -= OnClueActivated;
            _view.ExitActivated -= OnExitActivated;
            _view.ExitHoverChanged -= OnExitHoverChanged;

            _moveSubscription.Dispose();
            _clueDroppedSubscription.Dispose();
        }
    }
}
