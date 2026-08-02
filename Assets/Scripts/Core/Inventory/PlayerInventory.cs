using System;
using System.Collections.Generic;

namespace GameName.Core.Inventory
{
    // IPlayerInventory 기본 구현.
    // 실제로 담을 수 있는지 여부는 IInventorySlotPolicy에 위임한다 — 단서/앰플이
    // 슬롯을 공유하는지 나누는지는 이 타입이 알지 못하고, 정책 구현체를
    // 갈아끼우는 것만으로 규칙을 바꿀 수 있다.
    //
    // 중복 검사는 정책에 위임하지 않고 이 타입이 직접 한다 — "같은 물건을 두 번
    // 담을 수 없다"는 규칙은 슬롯을 어떻게 나누느냐와 무관하게 항상 성립해야
    // 하는 인벤토리 자체의 불변식이기 때문이다. "같음"의 판단은 각 아이템
    // 타입의 Equals 구현(예: ClueInfo는 ClueId로 비교)에 맡긴다.
    public sealed class PlayerInventory : IPlayerInventory
    {
        private readonly List<IInventoryItem> _items = new List<IInventoryItem>();
        private readonly IInventorySlotPolicy _slotPolicy;

        public int Capacity { get; private set; }
        public IReadOnlyList<IInventoryItem> Items => _items;

        public PlayerInventory(IInventorySettings settings, IInventorySlotPolicy slotPolicy)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _slotPolicy = slotPolicy ?? throw new ArgumentNullException(nameof(slotPolicy));

            Capacity = settings.InitialCapacity;
        }

        public void IncreaseCapacity(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "용량 증가분은 양수여야 한다.");

            Capacity += amount;
        }

        public InventoryStoreResult TryStore(IInventoryItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (_items.Contains(item))
                return InventoryStoreResult.Failure(InventoryStoreFailureReason.Duplicate);

            var failureReason = _slotPolicy.Evaluate(_items, item, Capacity);
            if (failureReason.HasValue)
                return InventoryStoreResult.Failure(failureReason.Value);

            _items.Add(item);
            return InventoryStoreResult.Success();
        }

        public InventoryRemoveResult TryRemove(IInventoryItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            return _items.Remove(item)
                ? InventoryRemoveResult.Success()
                : InventoryRemoveResult.Failure(InventoryRemoveFailureReason.NotFound);
        }
    }
}
