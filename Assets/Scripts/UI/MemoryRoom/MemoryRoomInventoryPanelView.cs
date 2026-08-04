using System;
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
    //
    // 단서 칸에만 "되돌리기" 버튼을 붙인다 — 앰플은 원래 자리(조향실 보관함)가
    // 이 화면과 무관하고, 되돌려놓기는 단서에만 있는 개념이기 때문이다.
    // 지금 있는 방이 그 단서의 방인지는 이 View가 판단하지 않는다 — 다른 방에서
    // 눌러도 버튼은 항상 보이고, 실패 사유(WrongRoom)를 그대로 보여준다.
    public sealed class MemoryRoomInventoryPanelView
    {
        private readonly Label _countLabel;
        private readonly VisualElement _slotList;
        private readonly Label _fullNoticeLabel;
        private readonly Label _returnFailureLabel;

        public event Action<ClueId> ClueReturnRequested;

        public MemoryRoomInventoryPanelView(VisualElement root)
        {
            _countLabel = root.Q<Label>("inventory-count");
            _slotList = root.Q<VisualElement>("inventory-slot-list");
            _fullNoticeLabel = root.Q<Label>("inventory-full-notice");
            _returnFailureLabel = root.Q<Label>("clue-return-failure-message");
        }

        public void SetSlots(IReadOnlyList<IInventoryItem> items, int capacity)
        {
            _countLabel.text = $"{items.Count} / {capacity}";

            _slotList.Clear();
            for (var i = 0; i < capacity; i++)
                _slotList.Add(i < items.Count ? CreateFilledSlot(items[i]) : CreateEmptySlot());

            _fullNoticeLabel.style.display = items.Count >= capacity ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetReturnFailureMessage(string message)
        {
            _returnFailureLabel.text = message ?? string.Empty;
            _returnFailureLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private VisualElement CreateFilledSlot(IInventoryItem item)
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

                var returnButton = new Button(() => ClueReturnRequested?.Invoke(clue.Id)) { text = "되돌리기" };
                returnButton.AddToClassList("inventory-slot__action-button");
                slot.Add(returnButton);
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
