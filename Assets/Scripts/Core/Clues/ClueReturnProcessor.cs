using System;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Clues
{
    // 인벤토리의 단서를 원래 방에 되돌려놓는 처리기.
    //
    // "구조만 열어두고 기능은 만들지 마라"고 미뤄뒀던 되돌려놓기 기능이다.
    // 그 단서가 있던 방에서만 되돌려놓을 수 있다 — ClueCollector가 "지금 있는
    // 방의 단서만 집을 수 있다"를 검사하는 것과 대칭인 검사를, 반대 방향으로
    // 한다. 분석 진행도(IClueAnalysisProgress)와 기록지는 전혀 건드리지
    // 않는다 — 물건의 위치와 그 물건에서 알아낸 정보는 서로 다른 층위이기
    // 때문이다.
    public sealed class ClueReturnProcessor
    {
        private readonly IPlayerLocation _playerLocation;
        private readonly IPlayerInventory _inventory;
        private readonly IMemoryRoomClueTracker _clueTracker;
        private readonly IEventBus _eventBus;

        public ClueReturnProcessor(
            IPlayerLocation playerLocation,
            IPlayerInventory inventory,
            IMemoryRoomClueTracker clueTracker,
            IEventBus eventBus)
        {
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _clueTracker = clueTracker ?? throw new ArgumentNullException(nameof(clueTracker));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public ClueReturnResult Return(ClueInfo clue)
        {
            if (clue == null) throw new ArgumentNullException(nameof(clue));

            if (!ContainsClue(clue))
                return ClueReturnResult.Failure(ClueReturnFailureReason.ClueNotInInventory);

            if (!_playerLocation.Current.Equals(MemoryGraphNodeId.OfRoom(clue.RoomId)))
                return ClueReturnResult.Failure(ClueReturnFailureReason.WrongRoom);

            _inventory.TryRemove(clue);
            _clueTracker.MarkReturned(clue.Id);

            _eventBus.Publish(new ClueReturnedEvent(clue.Id, clue.RoomId));

            return ClueReturnResult.Success();
        }

        private bool ContainsClue(ClueInfo clue)
        {
            foreach (var item in _inventory.Items)
            {
                if (item.Equals(clue))
                    return true;
            }

            return false;
        }
    }
}
