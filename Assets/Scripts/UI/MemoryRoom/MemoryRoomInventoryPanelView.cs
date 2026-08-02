using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Clues;
using GameName.Core.Inventory;
using GameName.UI.Shared;
using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom
{
    // 인벤토리 패널의 화면 요소 구성과 표시 갱신만 담당한다. 칸이 비었는지도
    // 그대로 보여준다 — 빈 칸이 있어야 용량이 체감되기 때문이다.
    public sealed class MemoryRoomInventoryPanelView
    {
        private readonly Label _countLabel;
        private readonly VisualElement _slotList;

        public MemoryRoomInventoryPanelView(VisualElement root)
        {
            _countLabel = root.Q<Label>("inventory-count");
            _slotList = root.Q<VisualElement>("inventory-slot-list");
        }

        public void SetSlots(IReadOnlyList<IInventoryItem> items, int capacity)
        {
            _countLabel.text = $"{items.Count} / {capacity}";

            _slotList.Clear();
            for (var i = 0; i < capacity; i++)
                _slotList.Add(i < items.Count ? CreateFilledSlot(items[i]) : CreateEmptySlot());
        }

        private static VisualElement CreateFilledSlot(IInventoryItem item)
        {
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot");
            slot.AddToClassList("inventory-slot--filled");

            if (item is ClueInfo clue)
            {
                var swatchRow = new VisualElement();
                swatchRow.AddToClassList("inventory-slot__swatch-row");
                foreach (var emotion in clue.ApparentComposition.Emotions)
                {
                    var swatch = new VisualElement();
                    swatch.AddToClassList("inventory-slot__swatch");
                    swatch.AddToClassList(EmotionDisplay.ColorClass(emotion));
                    swatchRow.Add(swatch);
                }
                slot.Add(swatchRow);

                var categoryLabel = new Label("단서");
                categoryLabel.AddToClassList("inventory-slot__category");
                slot.Add(categoryLabel);
            }
            else if (item is Ampoule ampoule)
            {
                var swatch = new VisualElement();
                swatch.AddToClassList("inventory-slot__swatch");
                swatch.AddToClassList(EmotionDisplay.ColorClass(ampoule.Scent.BaseEmotion));
                slot.Add(swatch);

                var categoryLabel = new Label("앰플");
                categoryLabel.AddToClassList("inventory-slot__category");
                slot.Add(categoryLabel);

                var summaryLabel = new Label(
                    $"{ampoule.TargetRoomId.Value} · {ScentSummaryFormatter.Summarize(ampoule.Scent)}");
                summaryLabel.AddToClassList("inventory-slot__summary");
                slot.Add(summaryLabel);
            }

            return slot;
        }

        private static VisualElement CreateEmptySlot()
        {
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot");
            slot.AddToClassList("inventory-slot--empty");
            return slot;
        }
    }
}
