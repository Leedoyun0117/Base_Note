using System.Collections.Generic;

namespace GameName.Core.Inventory
{
    // 플레이어가 물건을 들고 다니는 경계.
    // 방 위치나 이동에 대해서는 전혀 알지 못한다 — "지금 무엇을 들고 있는가"만
    // 관리한다.
    public interface IPlayerInventory
    {
        int Capacity { get; }
        IReadOnlyList<IInventoryItem> Items { get; }

        InventoryStoreResult TryStore(IInventoryItem item);
        InventoryRemoveResult TryRemove(IInventoryItem item);

        // 용량을 늘리는 것은 안전한 가산 동작이라(줄이거나 비우는 것과 달리
        // 아무 것도 파괴하지 않는다) 정상 경계인 여기 그대로 둔다.
        void IncreaseCapacity(int amount);
    }
}
