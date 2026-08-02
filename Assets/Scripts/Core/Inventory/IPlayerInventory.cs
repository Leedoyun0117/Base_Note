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

        // 용량을 늘리는 것은 안전한 가산 동작이라(줄이거나 비우는 것과 달리
        // 아무 것도 파괴하지 않는다) 여기 그대로 둔다 — 업그레이드 상점이
        // 이 메서드로 인벤토리 칸을 늘린다. 내용물을 통째로 비우는 권한은
        // IResettable로만 노출된다(별도로 분리한 이유는 그 타입 주석 참고).
        void IncreaseCapacity(int amount);
    }
}
