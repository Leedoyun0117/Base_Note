using System.Collections.Generic;

namespace GameName.Core.Inventory
{
    // 기억 방에서 분석실까지 물건(단서, 앰플 등)을 운반하는 경계.
    // 방 위치나 이동에 대해서는 전혀 알지 못한다 — "지금 무엇을 들고 있는가"만
    // 관리한다.
    public interface IPlayerInventory
    {
        int Capacity { get; }
        IReadOnlyList<IInventoryItem> Items { get; }

        InventoryStoreResult TryStore(IInventoryItem item);
        InventoryRemoveResult TryRemove(IInventoryItem item);
        void IncreaseCapacity(int amount);
    }
}
