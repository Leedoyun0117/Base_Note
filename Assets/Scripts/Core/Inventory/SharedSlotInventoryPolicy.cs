using System.Collections.Generic;

namespace GameName.Core.Inventory
{
    // 물건 종류를 구분하지 않고 하나의 용량 풀을 공유하는 가장 단순한 정책.
    // 종류별로 칸을 나누는 규칙이 기획에서 확정되면 이 구현체를 다른
    // IInventorySlotPolicy 구현체로 교체하면 된다 — Inventory 쪽 코드는
    // 바뀌지 않는다.
    public sealed class SharedSlotInventoryPolicy : IInventorySlotPolicy
    {
        public InventoryStoreFailureReason? Evaluate(
            IReadOnlyList<IInventoryItem> currentItems, IInventoryItem newItem, int capacity)
        {
            return currentItems.Count >= capacity
                ? InventoryStoreFailureReason.Full
                : (InventoryStoreFailureReason?)null;
        }
    }
}
