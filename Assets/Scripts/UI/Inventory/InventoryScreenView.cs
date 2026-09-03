using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Inventory;
using UnityEngine.UIElements;

namespace GameName.UI.Inventory
{
    // 인벤토리(가방) 오버레이의 요소 구성과 표시 갱신.
    //
    // 칸 개수는 넘겨받은 용량이 정한다 — Core의 IPlayerInventory.Capacity가
    // 그대로 온다. 빈 칸도 그려서 용량이 눈에 보이게 한다.
    //
    // 표시 전용이다. 단서를 담고 빼는 것은 ClueState의 투영(InventoryProjection)이
    // 수집·추출 사건을 듣고 처리한다. 이 화면은 그 결과를 그리고, 채워진 칸을
    // 눌렀다는 사실만(SlotClicked) 알린다 — 그 뒤 "대화에 사용/기억 추출"을
    // 고르는 것은 별도 컨트롤러의 몫이다.
    public sealed class InventoryScreenView
    {
        private readonly Label _countLabel;
        private readonly VisualElement _slotGrid;
        private readonly Label _messageLabel;

        // 채워진 칸을 눌렀다 — 몇 번째 칸인지 알린다(컨트롤러가 ClueInfo로 옮긴다).
        public event Action<int> SlotClicked;

        public InventoryScreenView(VisualElement root)
        {
            _countLabel = root.Q<Label>("inventory-count");
            _slotGrid = root.Q<VisualElement>("inventory-slot-grid");
            _messageLabel = root.Q<Label>("inventory-message");
        }

        // 지금 그려진 칸 개수. 칸 수가 Core 설정값을 따르는지 화면 없이 확인할
        // 수 있게 노출한다.
        public int SlotCount => _slotGrid.childCount;

        public void SetSlots(IReadOnlyList<IInventoryItem> items, int capacity)
        {
            _countLabel.text = $"{items.Count} / {capacity}";

            _slotGrid.Clear();
            for (var i = 0; i < capacity; i++)
                _slotGrid.Add(i < items.Count ? CreateFilledSlot(items[i], i) : CreateEmptySlot());
        }

        public void SetMessage(string message)
        {
            _messageLabel.text = message ?? string.Empty;
            _messageLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private VisualElement CreateFilledSlot(IInventoryItem item, int index)
        {
            var slot = CreateEmptySlot();
            slot.AddToClassList("inventory-slot--filled");

            if (item is ClueInfo clue)
            {
                var name = string.IsNullOrEmpty(clue.DisplayName)
                    ? (clue.Kind == ClueKind.Poster ? "단서 · 포스터" : "단서 · 물건")
                    : clue.DisplayName;
                var categoryLabel = new Label(name);
                categoryLabel.AddToClassList("inventory-slot__category");
                slot[0].Add(categoryLabel);

                // 채워진 칸만 누를 수 있다 — 빈 칸은 고를 것이 없다.
                slot.RegisterCallback<ClickEvent>(_ => SlotClicked?.Invoke(index));
            }

            return slot;
        }

        private static VisualElement CreateEmptySlot()
        {
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot");

            var inner = new VisualElement();
            inner.AddToClassList("inventory-slot__inner");
            slot.Add(inner);

            return slot;
        }
    }
}
