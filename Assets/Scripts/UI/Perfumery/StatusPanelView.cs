using System;
using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.Perfumery
{
    // 우측(상태) 패널의 화면 요소 구성과 표시 갱신만 담당한다.
    // 정신력이 얼마나 남았는지, 보관함/인벤토리에 자리가 있는지는 이 클래스가
    // 판단하지 않는다 — 컨트롤러가 넘겨준 값을 그대로 그릴 뿐이다.
    public sealed class StatusPanelView
    {
        private readonly VisualElement _mentalityBarFill;
        private readonly Label _mentalityValueLabel;
        private readonly Label _storageCountLabel;
        private readonly VisualElement _storageList;
        private readonly Label _inventoryCountLabel;
        private readonly VisualElement _inventoryList;
        private readonly Label _transferFailureLabel;

        // 보관함에 있는 앰플을 인벤토리로 옮기라는 요청. 반대 방향(인벤토리 ->
        // 보관함)은 이 화면에서 노출하지 않는다 — 기획상 플레이어의 기본 동선은
        // "만들고 -> 옮기고 -> 목표 방에서 시험"이라 이 화면에서는 그 한 방향만
        // 필요하다. Core(AmpouleTransferProcessor)는 반대 방향도 이미 지원한다.
        public event Action<Ampoule> MoveToInventoryRequested;

        public StatusPanelView(VisualElement root)
        {
            _mentalityBarFill = root.Q<VisualElement>("mentality-bar-fill");
            _mentalityValueLabel = root.Q<Label>("mentality-value");
            _storageCountLabel = root.Q<Label>("storage-count");
            _storageList = root.Q<VisualElement>("storage-list");
            _inventoryCountLabel = root.Q<Label>("inventory-count");
            _inventoryList = root.Q<VisualElement>("inventory-list");
            _transferFailureLabel = root.Q<Label>("transfer-failure-message");
        }

        public void SetMentality(int current, int max)
        {
            _mentalityValueLabel.text = $"{current} / {max}";

            var ratio = max <= 0 ? 0f : (float)current / max;
            _mentalityBarFill.style.width = Length.Percent(Mathf.Clamp01(ratio) * 100f);
        }

        public void SetStorage(IReadOnlyList<Ampoule> ampoules, int capacity)
        {
            _storageCountLabel.text = $"{ampoules.Count} / {capacity}";

            _storageList.Clear();
            foreach (var ampoule in ampoules)
                _storageList.Add(CreateStorageRow(ampoule));
        }

        public void SetInventoryAmpoules(IReadOnlyList<Ampoule> ampoules, int capacity)
        {
            _inventoryCountLabel.text = $"{ampoules.Count} / {capacity}";

            _inventoryList.Clear();
            foreach (var ampoule in ampoules)
                _inventoryList.Add(CreateInventoryRow(ampoule));
        }

        public void SetTransferFailureMessage(string message)
        {
            _transferFailureLabel.text = message ?? string.Empty;
            _transferFailureLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private VisualElement CreateStorageRow(Ampoule ampoule)
        {
            var row = new VisualElement();
            row.AddToClassList("ampoule-row");

            var swatch = new VisualElement();
            swatch.AddToClassList("ampoule-row__swatch");
            swatch.AddToClassList(EmotionDisplay.ColorClass(ampoule.Scent.BaseEmotion));
            row.Add(swatch);

            var summaryLabel = new Label(
                $"{ampoule.TargetRoomId.Value} · {ScentSummaryFormatter.Summarize(ampoule.Scent)}");
            summaryLabel.AddToClassList("ampoule-row__summary");
            row.Add(summaryLabel);

            var moveButton = new Button(() => MoveToInventoryRequested?.Invoke(ampoule)) { text = "인벤토리로" };
            moveButton.AddToClassList("ampoule-row__action-button");
            row.Add(moveButton);

            return row;
        }

        private static VisualElement CreateInventoryRow(Ampoule ampoule)
        {
            var row = new VisualElement();
            row.AddToClassList("ampoule-row");

            var swatch = new VisualElement();
            swatch.AddToClassList("ampoule-row__swatch");
            swatch.AddToClassList(EmotionDisplay.ColorClass(ampoule.Scent.BaseEmotion));
            row.Add(swatch);

            var summaryLabel = new Label(
                $"{ampoule.TargetRoomId.Value} · {ScentSummaryFormatter.Summarize(ampoule.Scent)}");
            summaryLabel.AddToClassList("ampoule-row__summary");
            row.Add(summaryLabel);

            return row;
        }
    }
}
