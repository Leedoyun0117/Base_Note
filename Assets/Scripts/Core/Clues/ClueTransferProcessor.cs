using System;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Clues
{
    // 분석실 보관대와 인벤토리 사이에서 단서를 옮기는 처리기.
    // AmpouleTransferProcessor와 같은 패턴이다 — 두 저장소 다 분석실에서만
    // 조작할 수 있고(IPlayerLocation으로 확인), 목적지에 담다가 실패하면 이미
    // 출발지에서 뺀 것을 되돌려 단서가 두 저장소 어디에도 없는 상태가 생기지
    // 않게 한다.
    public sealed class ClueTransferProcessor
    {
        private readonly IPlayerLocation _playerLocation;
        private readonly MemoryGraphNodeId _analysisRoomNodeId;
        private readonly IClueStorage _storage;
        private readonly IPlayerInventory _inventory;

        public ClueTransferProcessor(
            IPlayerLocation playerLocation,
            MemoryGraphNodeId analysisRoomNodeId,
            IClueStorage storage,
            IPlayerInventory inventory)
        {
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
            _analysisRoomNodeId = analysisRoomNodeId;
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        public ClueTransferResult MoveToInventory(ClueInfo clue)
        {
            if (clue == null) throw new ArgumentNullException(nameof(clue));

            if (!InAnalysisRoom())
                return ClueTransferResult.Failure(ClueTransferFailureReason.NotInAnalysisRoom);

            var removeResult = _storage.TryRemove(clue);
            if (!removeResult.Succeeded)
                return ClueTransferResult.Failure(ClueTransferFailureReason.ClueNotFound);

            var storeResult = _inventory.TryStore(clue);
            if (!storeResult.Succeeded)
            {
                _storage.TryStore(clue); // 롤백: 보관대에 자리가 남아 있었으므로 반드시 성공한다.
                return ClueTransferResult.Failure(ClueTransferFailureReason.DestinationFull);
            }

            return ClueTransferResult.Success();
        }

        public ClueTransferResult MoveToStorage(ClueInfo clue)
        {
            if (clue == null) throw new ArgumentNullException(nameof(clue));

            if (!InAnalysisRoom())
                return ClueTransferResult.Failure(ClueTransferFailureReason.NotInAnalysisRoom);

            var removeResult = _inventory.TryRemove(clue);
            if (!removeResult.Succeeded)
                return ClueTransferResult.Failure(ClueTransferFailureReason.ClueNotFound);

            var storeResult = _storage.TryStore(clue);
            if (!storeResult.Succeeded)
            {
                _inventory.TryStore(clue); // 롤백: 인벤토리에 자리가 남아 있었으므로 반드시 성공한다.
                return ClueTransferResult.Failure(ClueTransferFailureReason.DestinationFull);
            }

            return ClueTransferResult.Success();
        }

        private bool InAnalysisRoom() => _playerLocation.Current.Equals(_analysisRoomNodeId);
    }
}
