using System;
using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;

namespace GameName.UI.Perfumery
{
    // 좌측 패널의 입력 처리와 Core 연동을 담당한다. 방 목록/선택 상태를 들고
    // 있다가 View에 그리라고 요청할 뿐, "어떤 방이 복원되었는가"는 절대
    // 스스로 계산하지 않는다 — IMemoryRoomRestorationTracker가 이미 판단해
    // 둔 값을 그대로 읽는다. 정답(MemoryRoomAnswer)에는 접근하지 않는다 —
    // 공개 정보(IMemoryRoomPublicInfoRepository)만 받는다.
    public sealed class RoomSelectionPanelController : IDisposable
    {
        private readonly RoomSelectionPanelView _view;
        private readonly IReadOnlyList<MemoryRoomId> _roomIds;
        private readonly IMemoryRoomPublicInfoRepository _publicInfoRepository;
        private readonly IMemoryRoomRestorationTracker _restorationTracker;
        private readonly IDisposable _restoredSubscription;

        private MemoryRoomId? _selectedRoomId;

        // 다른 패널(조향)에 "선택된 방이 바뀌었다"만 알린다. 방의 요구 총량이라는
        // 공개 정보를 함께 넘겨, 받는 쪽이 정답 저장소를 따로 조회할 필요가 없게 한다.
        public event Action<MemoryRoomId, MemoryRoomPublicInfo> SelectedRoomChanged;

        public RoomSelectionPanelController(
            RoomSelectionPanelView view,
            IReadOnlyList<MemoryRoomId> roomIds,
            IMemoryRoomPublicInfoRepository publicInfoRepository,
            IMemoryRoomRestorationTracker restorationTracker,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _roomIds = roomIds ?? throw new ArgumentNullException(nameof(roomIds));
            _publicInfoRepository =
                publicInfoRepository ?? throw new ArgumentNullException(nameof(publicInfoRepository));
            _restorationTracker = restorationTracker ?? throw new ArgumentNullException(nameof(restorationTracker));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.RoomSelected += OnRoomSelected;
            _restoredSubscription = eventBus.Subscribe<MemoryRoomRestoredEvent>(_ => RefreshRooms());

            RefreshRooms();
        }

        private void OnRoomSelected(MemoryRoomId roomId)
        {
            if (!_publicInfoRepository.TryGetPublicInfo(roomId, out var info))
                return;

            _selectedRoomId = roomId;
            _view.SetSelectedRoom(roomId);
            _view.SetRequiredTotal(info.RequiredSupportingIntensityTotal);

            SelectedRoomChanged?.Invoke(roomId, info);
        }

        private void RefreshRooms()
        {
            var rooms = new List<RoomListItemData>(_roomIds.Count);
            foreach (var roomId in _roomIds)
            {
                if (!_publicInfoRepository.TryGetPublicInfo(roomId, out var info))
                    continue;

                rooms.Add(new RoomListItemData(info, _restorationTracker.IsRestored(roomId)));
            }

            _view.SetRooms(rooms, _selectedRoomId);
        }

        public void Dispose()
        {
            _view.RoomSelected -= OnRoomSelected;
            _restoredSubscription.Dispose();
        }
    }
}
