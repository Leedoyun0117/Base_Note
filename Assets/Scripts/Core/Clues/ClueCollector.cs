using System;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Clues
{
    // 단서 습득 경계.
    // "플레이어가 지금 있는 방의 단서만 집을 수 있다"는 규칙, "이미 집은 단서는
    // 다시 집을 수 없다"는 규칙, 그리고 집은 단서를 인벤토리에 담는 절차를
    // 이어준다. ClueId만 받고 ClueDefinition을 직접 받지 않는다 — 호출부(UI 등)가
    // 애초에 진실 데이터를 들고 있을 필요가 없어야 하기 때문이다. 진실
    // 데이터는 IMemoryRoomClueTracker 안에만 있고, 인벤토리에는 ToInfo()로 변환한
    // 안전한 정보만 들어간다.
    //
    // "그 단서가 어느 방에 있는가"도 더 이상 정의에서 읽지 않고 추적기에
    // 물어본다 — 버리기로 소속 방이 바뀔 수 있게 된 뒤로 그 사실의 유일한
    // 진실 원천이 추적기이기 때문이다.
    public sealed class ClueCollector
    {
        private readonly IPlayerLocation _playerLocation;
        private readonly IPlayerInventory _inventory;
        private readonly IMemoryRoomClueTracker _clueTracker;

        public ClueCollector(
            IPlayerLocation playerLocation, IPlayerInventory inventory, IMemoryRoomClueTracker clueTracker)
        {
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _clueTracker = clueTracker ?? throw new ArgumentNullException(nameof(clueTracker));
        }

        public ClueCollectionResult Collect(ClueId clueId)
        {
            if (!_clueTracker.TryGetPlacedClue(clueId, out var definition, out var roomId))
                return ClueCollectionResult.Failure(ClueCollectionFailureReason.NotAvailable);

            if (!_playerLocation.Current.Equals(MemoryGraphNodeId.OfRoom(roomId)))
                return ClueCollectionResult.Failure(ClueCollectionFailureReason.WrongRoom);

            var storeResult = _inventory.TryStore(definition.ToInfo());
            if (!storeResult.Succeeded)
            {
                ClueCollectionFailureReason reason;
                if (storeResult.FailureReason == InventoryStoreFailureReason.Full)
                    reason = ClueCollectionFailureReason.InventoryFull;
                else if (storeResult.FailureReason == InventoryStoreFailureReason.Duplicate)
                    reason = ClueCollectionFailureReason.Duplicate;
                else
                    reason = ClueCollectionFailureReason.ItemTypeNotAccepted;

                return ClueCollectionResult.Failure(reason);
            }

            _clueTracker.MarkCollected(clueId);
            return ClueCollectionResult.Success();
        }
    }
}
