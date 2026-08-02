using System;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Ampoules
{
    // 조향실 보관함과 인벤토리 사이에서 앰플을 옮기는 처리기.
    // 두 저장소 다 조향실에서만 조작할 수 있다 — IPlayerLocation으로 확인한다.
    //
    // 목적지에 담다가 실패하면(용량 등) 이미 출발지에서 뺀 것을 되돌려, 앰플이
    // 두 저장소 어디에도 없거나 양쪽에 동시에 있는 상태가 생기지 않게 한다.
    public sealed class AmpouleTransferProcessor
    {
        private readonly IPlayerLocation _playerLocation;
        private readonly MemoryGraphNodeId _perfumeryRoomNodeId;
        private readonly IAmpouleStorage _storage;
        private readonly IPlayerInventory _inventory;

        public AmpouleTransferProcessor(
            IPlayerLocation playerLocation,
            MemoryGraphNodeId perfumeryRoomNodeId,
            IAmpouleStorage storage,
            IPlayerInventory inventory)
        {
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
            _perfumeryRoomNodeId = perfumeryRoomNodeId;
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        public AmpouleTransferResult MoveToInventory(Ampoule ampoule)
        {
            if (ampoule == null) throw new ArgumentNullException(nameof(ampoule));

            if (!InPerfumeryRoom())
                return AmpouleTransferResult.Failure(AmpouleTransferFailureReason.NotInPerfumeryRoom);

            var removeResult = _storage.TryRemove(ampoule);
            if (!removeResult.Succeeded)
                return AmpouleTransferResult.Failure(AmpouleTransferFailureReason.AmpouleNotFound);

            var storeResult = _inventory.TryStore(ampoule);
            if (!storeResult.Succeeded)
            {
                _storage.TryStore(ampoule); // 롤백: 보관함에 자리가 남아 있었으므로 반드시 성공한다.
                return AmpouleTransferResult.Failure(AmpouleTransferFailureReason.DestinationFull);
            }

            return AmpouleTransferResult.Success();
        }

        public AmpouleTransferResult MoveToStorage(Ampoule ampoule)
        {
            if (ampoule == null) throw new ArgumentNullException(nameof(ampoule));

            if (!InPerfumeryRoom())
                return AmpouleTransferResult.Failure(AmpouleTransferFailureReason.NotInPerfumeryRoom);

            var removeResult = _inventory.TryRemove(ampoule);
            if (!removeResult.Succeeded)
                return AmpouleTransferResult.Failure(AmpouleTransferFailureReason.AmpouleNotFound);

            var storeResult = _storage.TryStore(ampoule);
            if (!storeResult.Succeeded)
            {
                _inventory.TryStore(ampoule); // 롤백: 인벤토리에 자리가 남아 있었으므로 반드시 성공한다.
                return AmpouleTransferResult.Failure(AmpouleTransferFailureReason.DestinationFull);
            }

            return AmpouleTransferResult.Success();
        }

        private bool InPerfumeryRoom() => _playerLocation.Current.Equals(_perfumeryRoomNodeId);
    }
}
