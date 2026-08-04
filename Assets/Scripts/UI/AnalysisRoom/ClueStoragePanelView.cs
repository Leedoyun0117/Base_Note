using System;
using System.Collections.Generic;
using GameName.Core.Analysis;
using GameName.Core.Clues;
using GameName.UI.Shared;
using UnityEngine.UIElements;

namespace GameName.UI.AnalysisRoom
{
    // 보관대 패널의 화면 요소 구성과 표시 갱신만 담당한다. 옮길 수 있는지,
    // 자리가 있는지는 이 클래스가 판단하지 않는다 — 컨트롤러가 Core로 이미
    // 얻은 결과를 그대로 그릴 뿐이다.
    public sealed class ClueStoragePanelView
    {
        private readonly Label _storageCountLabel;
        private readonly VisualElement _storageList;
        private readonly Label _inventoryCountLabel;
        private readonly VisualElement _inventoryList;
        private readonly Label _inventoryFullNoticeLabel;
        private readonly Label _transferFailureLabel;

        public event Action<ClueId> MoveToInventoryRequested;
        public event Action<ClueId> MoveToStorageRequested;

        public ClueStoragePanelView(VisualElement root)
        {
            _storageCountLabel = root.Q<Label>("clue-storage-count");
            _storageList = root.Q<VisualElement>("clue-storage-list");
            _inventoryCountLabel = root.Q<Label>("clue-storage-inventory-count");
            _inventoryList = root.Q<VisualElement>("clue-storage-inventory-list");
            _inventoryFullNoticeLabel = root.Q<Label>("clue-storage-inventory-full-notice");
            _transferFailureLabel = root.Q<Label>("clue-storage-transfer-failure-message");
        }

        public void SetStorage(IReadOnlyList<ClueStorageRowData> rows, int capacity)
        {
            _storageCountLabel.text = $"{rows.Count} / {capacity}";

            _storageList.Clear();
            foreach (var row in rows)
                _storageList.Add(CreateStorageRow(row));
        }

        public void SetInventoryClues(IReadOnlyList<ClueInfo> clues, int capacity, bool isInventoryFull)
        {
            _inventoryCountLabel.text = $"{clues.Count} / {capacity}";

            _inventoryList.Clear();
            foreach (var clue in clues)
                _inventoryList.Add(CreateInventoryRow(clue));

            _inventoryFullNoticeLabel.style.display = isInventoryFull ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetTransferFailureMessage(string message)
        {
            _transferFailureLabel.text = message ?? string.Empty;
            _transferFailureLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private VisualElement CreateStorageRow(ClueStorageRowData row)
        {
            var container = new VisualElement();
            container.AddToClassList("clue-row");

            container.Add(CreateSwatchRow(row.Clue));

            var summaryLabel = new Label(ScentSummaryFormatter.Summarize(row.Clue.ApparentComposition));
            summaryLabel.AddToClassList("clue-row__summary");
            container.Add(summaryLabel);

            var depthLabel = new Label(DescribeDepth(row.AnalyzedDepth));
            depthLabel.AddToClassList("caption");
            container.Add(depthLabel);

            var moveButton = new Button(() => MoveToInventoryRequested?.Invoke(row.Clue.Id)) { text = "인벤토리로" };
            moveButton.AddToClassList("clue-row__action-button");
            container.Add(moveButton);

            return container;
        }

        private VisualElement CreateInventoryRow(ClueInfo clue)
        {
            var container = new VisualElement();
            container.AddToClassList("clue-row");

            container.Add(CreateSwatchRow(clue));

            var summaryLabel = new Label(ScentSummaryFormatter.Summarize(clue.ApparentComposition));
            summaryLabel.AddToClassList("clue-row__summary");
            container.Add(summaryLabel);

            var moveButton = new Button(() => MoveToStorageRequested?.Invoke(clue.Id)) { text = "보관하기" };
            moveButton.AddToClassList("clue-row__action-button");
            container.Add(moveButton);

            return container;
        }

        private static VisualElement CreateSwatchRow(ClueInfo clue)
        {
            var swatchRow = new VisualElement();
            swatchRow.AddToClassList("clue-row__swatches");
            foreach (var emotion in clue.ApparentComposition.Emotions)
            {
                var swatch = new VisualElement();
                swatch.AddToClassList("clue-row__swatch");
                swatch.AddToClassList(EmotionDisplay.ColorClass(emotion));
                swatchRow.Add(swatch);
            }
            return swatchRow;
        }

        private static string DescribeDepth(AnalysisDepth? depth)
        {
            if (depth == null) return "미분석";
            return depth.Value == AnalysisDepth.Advanced ? "고급 분석 완료" : "일반 분석 완료";
        }
    }
}
