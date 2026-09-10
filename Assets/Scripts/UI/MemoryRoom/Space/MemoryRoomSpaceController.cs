using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;

namespace GameName.UI.MemoryRoom.Space
{
    // 씬에 그려진 기억 방과 Core 사이를 잇는 단 하나의 지점.
    //
    // 씬 오브젝트는 Core 처리기를 전혀 참조하지 않는다 — 단서 오브젝트는 자기
    // 식별자만 들고 있고, 눌렸다는 사실만 알린다. 그 단서로 무엇을 할지(서사
    // 표시 + 태그 해석 + 턴 소모)는 화면 컨트롤러가 ClueUseProcessor에 맡긴다.
    //
    // 지금 어느 라운드를 그릴지는 RunProgressor가 정한다(RoomStartedEvent).
    // 이미 읽은 단서(ClueState.Used)는 방에서 사라진다 — 그 계기가 ClueUsedEvent다.
    //
    // 방이 다시 그려지는 계기는 전부 Refresh 하나로 모인다.
    public sealed class MemoryRoomSpaceController : IDisposable
    {
        private readonly MemoryRoomSpaceView _view;
        private readonly IMemoryRoomClueTracker _clueTracker;
        private readonly IClueStateReader _clueState;
        private readonly MemoryRoomLayout _layout;

        private readonly IDisposable _clueUsedSubscription;
        private readonly IDisposable _roomStartedSubscription;

        private readonly Dictionary<ClueId, ClueInfo> _visibleCluesById = new Dictionary<ClueId, ClueInfo>();

        private MemoryRoomId _activeRoomId;
        private bool _hasActiveRoom;

        // 단서를 눌렀다 — 화면 컨트롤러가 이 단서를 읽는다.
        public event Action<ClueInfo> ClueActivated;

        // 플레이어에게 보여줄 한 줄.
        public event Action<string> MessageChanged;

        public MemoryRoomSpaceController(
            MemoryRoomSpaceView view,
            MemoryRoomLayout layout,
            IMemoryRoomClueTracker clueTracker,
            IClueStateReader clueState,
            MemoryRoomId initialRoomId,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _clueTracker = clueTracker ?? throw new ArgumentNullException(nameof(clueTracker));
            _clueState = clueState ?? throw new ArgumentNullException(nameof(clueState));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.Build(_layout);
            _view.ClueActivated += OnClueActivated;

            _clueUsedSubscription = eventBus.Subscribe<ClueUsedEvent>(_ => Refresh());
            _roomStartedSubscription = eventBus.Subscribe<RoomStartedEvent>(OnRoomStarted);

            _activeRoomId = initialRoomId;
            _hasActiveRoom = true;
            Refresh();
        }

        // 스토리 패널이 열려 있는 동안 방을 조작하지 못하게 막는다.
        public void SetInteractionEnabled(bool enabled) => _view.SetInteractionEnabled(enabled);

        public void Refresh()
        {
            if (!_hasActiveRoom)
            {
                _visibleCluesById.Clear();
                _view.HideRoom();
                return;
            }

            _view.SetContents(BuildClueItems(_activeRoomId));
        }

        private void OnRoomStarted(RoomStartedEvent e)
        {
            _activeRoomId = e.RoomId;
            _hasActiveRoom = true;
            Refresh();
        }

        private IReadOnlyList<ClueSceneItem> BuildClueItems(MemoryRoomId roomId)
        {
            _visibleCluesById.Clear();
            var items = new List<ClueSceneItem>();
            foreach (var info in _clueTracker.GetCluesInRoom(roomId))
            {
                if (_clueState.TryGetState(info.Id, out var state) && state != ClueState.Available)
                    continue;

                _visibleCluesById[info.Id] = info;
                items.Add(new ClueSceneItem(
                    info.Id, info.Kind,
                    CluePlacementLayout.PositionAt(_layout, info.Kind, info.AuthoredPosition),
                    accessible: true));
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

            MessageChanged?.Invoke("이 단서는 더 이상 이 방에 없습니다.");
        }

        public void Dispose()
        {
            _view.ClueActivated -= OnClueActivated;
            _clueUsedSubscription.Dispose();
            _roomStartedSubscription.Dispose();
        }
    }
}
