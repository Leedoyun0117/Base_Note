using System.Collections.Generic;

namespace GameName.Core.Inventory
{
    // 단서와 앰플이 같은 칸을 공유하는지, 종류별로 별도 칸을 쓰는지는 아직
    // 미정이라 정책으로 분리한다. 인벤토리는 "지금 상태에서 이 물건을 더 담을
    // 수 있는가"만 이 정책에 묻고, 그 판단 근거(용량 배분 등)는 알지 못한다.
    public interface IInventorySlotPolicy
    {
        // 담을 수 있으면 null, 없으면 그 사유를 반환한다.
        InventoryStoreFailureReason? Evaluate(
            IReadOnlyList<IInventoryItem> currentItems, IInventoryItem newItem, int capacity);
    }
}
