using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.UI.Shared;

namespace GameName.UI.MemoryRoom
{
    // 단서 목록 패널의 입력 처리와 Core 연동을 담당한다. ClueId만 다루고
    // ClueDefinition(진실 포함)에는 접근하지 않는다 — IMemoryRoomClueTracker의
    // 공개 조회(GetAvailableClueInfos)와 ClueCollector.Collect(ClueId)만 쓴다.
    //
    // 이동 완료 이벤트를 구독해서만 목록을 다시 그린다 — 방을 옮기면 보여줄
    // 단서 목록 자체가 바뀌기 때문이다. 습득은 그 자리에서 결과가 바로
    // 나오므로 별도 이벤트 없이 직접 새로고침한다. 되돌려놓기는 인벤토리
    // 패널(다른 컨트롤러)에서 일어나므로, ClueReturnedEvent를 구독해서만
    // 그 결과를 반영한다 — 되돌린 단서가 이 방의 것이 아니어도 그냥 다시
    // 그리기만 하면 되므로 방 일치 여부를 따로 걸러낼 필요가 없다.
    public sealed class ClueCollectionPanelController : IDisposable
    {
        private readonly ClueCollectionPanelView _view;
        private readonly IPlayerLocation _playerLocation;
        private readonly IMemoryRoomClueTracker _clueTracker;
        private readonly ClueCollector _clueCollector;
        private readonly IReadOnlyList<MemoryRoomId> _roomIds;
        private readonly IDisposable _moveSubscription;
        private readonly IDisposable _clueReturnedSubscription;

        // 습득에 성공하면 인벤토리 내용이 바뀐다 — 인벤토리 패널은 이 화면의
        // 다른 컨트롤러가 소유하므로, 화면 컨트롤러가 이 이벤트를 듣고
        // 인벤토리 패널에 새로고침을 지시한다.
        public event Action ClueCollected;

        public ClueCollectionPanelController(
            ClueCollectionPanelView view,
            IPlayerLocation playerLocation,
            IMemoryRoomClueTracker clueTracker,
            ClueCollector clueCollector,
            IReadOnlyList<MemoryRoomId> roomIds,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
            _clueTracker = clueTracker ?? throw new ArgumentNullException(nameof(clueTracker));
            _clueCollector = clueCollector ?? throw new ArgumentNullException(nameof(clueCollector));
            _roomIds = roomIds ?? throw new ArgumentNullException(nameof(roomIds));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.CollectRequested += OnCollectRequested;
            _moveSubscription = eventBus.Subscribe<MemoryRoomMoveCompletedEvent>(_ => Refresh());
            _clueReturnedSubscription = eventBus.Subscribe<ClueReturnedEvent>(_ => Refresh());

            Refresh();
        }

        private void OnCollectRequested(ClueId clueId)
        {
            var result = _clueCollector.Collect(clueId);
            if (!result.Succeeded)
            {
                _view.SetCollectFailureMessage(DescribeFailure(result.FailureReason.Value));
                return;
            }

            _view.SetCollectFailureMessage(null);
            Refresh();
            ClueCollected?.Invoke();
        }

        private void Refresh()
        {
            if (!CurrentRoomResolver.TryResolve(_playerLocation.Current, _roomIds, out var roomId))
            {
                _view.SetClues(Array.Empty<ClueInfo>());
                return;
            }

            _view.SetClues(_clueTracker.GetAvailableClueInfos(roomId));
        }

        private static string DescribeFailure(ClueCollectionFailureReason reason)
        {
            switch (reason)
            {
                case ClueCollectionFailureReason.NotAvailable: return "이미 습득했거나 존재하지 않는 단서입니다.";
                case ClueCollectionFailureReason.WrongRoom: return "지금 있는 방의 단서가 아닙니다.";
                case ClueCollectionFailureReason.InventoryFull: return "인벤토리에 자리가 없습니다.";
                case ClueCollectionFailureReason.ItemTypeNotAccepted: return "이 물건은 담을 수 없습니다.";
                case ClueCollectionFailureReason.Duplicate: return "이미 가지고 있는 단서입니다.";
                default: return "습득에 실패했습니다.";
            }
        }

        public void Dispose()
        {
            _view.CollectRequested -= OnCollectRequested;
            _moveSubscription.Dispose();
            _clueReturnedSubscription.Dispose();
        }
    }
}
