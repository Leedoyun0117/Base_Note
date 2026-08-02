using System;
using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Emotions;
using GameName.UI.Shared;
using UnityEngine.UIElements;

namespace GameName.UI.Perfumery
{
    // 중앙(조향) 패널의 화면 요소 구성과 표시 갱신만 담당한다.
    // 배합이 유효한지, 대기열에 더 담을 수 있는지는 이 클래스가 판단하지
    // 않는다 — 컨트롤러가 Core로 얻은 결과를 그대로 그릴 뿐이다.
    public sealed class PerfumeryCompositionPanelView
    {
        private static readonly EmotionType[] AllEmotions =
        {
            EmotionType.Joy, EmotionType.Love, EmotionType.Anger, EmotionType.Sadness, EmotionType.Fear
        };

        private readonly VisualElement _baseEmotionSlots;
        private readonly VisualElement _supportingRows;
        private readonly Label _totalOverRequiredLabel;
        private readonly VisualElement _violationsList;
        private readonly Button _addToQueueButton;
        private readonly VisualElement _queueList;
        private readonly Label _craftCostNoticeLabel;
        private readonly Button _craftQueueButton;
        private readonly Label _craftResultLabel;

        private readonly Dictionary<EmotionType, VisualElement> _baseSlotsByEmotion =
            new Dictionary<EmotionType, VisualElement>();
        private readonly Dictionary<EmotionType, Label> _supportingValueLabelsByEmotion =
            new Dictionary<EmotionType, Label>();

        public event Action<EmotionType> BaseEmotionSelected;
        public event Action<EmotionType, int> SupportingIntensityStepRequested;
        public event Action AddToQueueRequested;
        public event Action<int> QueueItemRemoveRequested;
        public event Action CraftQueueRequested;

        public PerfumeryCompositionPanelView(VisualElement root)
        {
            _baseEmotionSlots = root.Q<VisualElement>("base-emotion-slots");
            _supportingRows = root.Q<VisualElement>("supporting-emotion-rows");
            _totalOverRequiredLabel = root.Q<Label>("total-over-required");
            _violationsList = root.Q<VisualElement>("violations-list");
            _addToQueueButton = root.Q<Button>("add-to-queue-button");
            _queueList = root.Q<VisualElement>("craft-queue-list");
            _craftCostNoticeLabel = root.Q<Label>("craft-cost-notice");
            _craftQueueButton = root.Q<Button>("craft-queue-button");
            _craftResultLabel = root.Q<Label>("craft-result-message");

            BuildBaseEmotionSlots();
            BuildSupportingEmotionRows();

            _addToQueueButton.clicked += () => AddToQueueRequested?.Invoke();
            _craftQueueButton.clicked += () => CraftQueueRequested?.Invoke();
        }

        public void SetBaseEmotion(EmotionType? selected)
        {
            foreach (var pair in _baseSlotsByEmotion)
            {
                var isSelected = selected.HasValue && pair.Key == selected.Value;
                pair.Value.EnableInClassList("emotion-slot--selected", isSelected);
            }
        }

        public void SetSupportingIntensities(IReadOnlyDictionary<EmotionType, int> intensities)
        {
            foreach (var pair in _supportingValueLabelsByEmotion)
            {
                pair.Value.text = intensities.TryGetValue(pair.Key, out var value)
                    ? value.ToString()
                    : "0";
            }
        }

        public void SetTotalAndRequired(int total, int? requiredTotal)
        {
            _totalOverRequiredLabel.text = requiredTotal.HasValue
                ? $"{total} / {requiredTotal.Value}"
                : $"{total} / -";
        }

        public void SetViolations(IReadOnlyList<string> violations)
        {
            _violationsList.Clear();
            foreach (var violation in violations)
            {
                var label = new Label(violation);
                label.AddToClassList("violation-item");
                _violationsList.Add(label);
            }
        }

        public void SetAddToQueueButtonEnabled(bool enabled) => _addToQueueButton.SetEnabled(enabled);

        public void SetQueue(IReadOnlyList<AmpouleCraftingRequest> queue)
        {
            _queueList.Clear();
            for (var i = 0; i < queue.Count; i++)
                _queueList.Add(CreateQueueRow(i, queue[i]));
        }

        public void SetCraftCostNotice(string text) => _craftCostNoticeLabel.text = text;

        public void SetCraftQueueButtonEnabled(bool enabled) => _craftQueueButton.SetEnabled(enabled);

        public void SetCraftResultMessage(string message)
        {
            _craftResultLabel.text = message ?? string.Empty;
            _craftResultLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private VisualElement CreateQueueRow(int index, AmpouleCraftingRequest request)
        {
            var row = new VisualElement();
            row.AddToClassList("ampoule-row");

            var swatch = new VisualElement();
            swatch.AddToClassList("ampoule-row__swatch");
            swatch.AddToClassList(EmotionDisplay.ColorClass(request.Scent.BaseEmotion));
            row.Add(swatch);

            var summaryLabel = new Label($"{request.TargetRoomId.Value} · {ScentSummaryFormatter.Summarize(request.Scent)}");
            summaryLabel.AddToClassList("ampoule-row__summary");
            row.Add(summaryLabel);

            var removeButton = new Button(() => QueueItemRemoveRequested?.Invoke(index)) { text = "삭제" };
            removeButton.AddToClassList("ampoule-row__action-button");
            row.Add(removeButton);

            return row;
        }

        private void BuildBaseEmotionSlots()
        {
            foreach (var emotion in AllEmotions)
            {
                var slot = new VisualElement();
                slot.AddToClassList("emotion-slot");
                slot.AddToClassList(EmotionDisplay.ColorClass(emotion));

                var label = new Label(EmotionDisplay.Label(emotion));
                label.AddToClassList("emotion-slot__label");
                slot.Add(label);

                slot.RegisterCallback<ClickEvent>(_ => BaseEmotionSelected?.Invoke(emotion));

                _baseSlotsByEmotion.Add(emotion, slot);
                _baseEmotionSlots.Add(slot);
            }
        }

        private void BuildSupportingEmotionRows()
        {
            foreach (var emotion in AllEmotions)
            {
                var row = new VisualElement();
                row.AddToClassList("stepper-row");

                var swatch = new VisualElement();
                swatch.AddToClassList("stepper-row__swatch");
                swatch.AddToClassList(EmotionDisplay.ColorClass(emotion));
                row.Add(swatch);

                var nameLabel = new Label(EmotionDisplay.Label(emotion));
                nameLabel.AddToClassList("stepper-row__label");
                row.Add(nameLabel);

                var capturedEmotion = emotion;

                var minusButton = new Button(() => SupportingIntensityStepRequested?.Invoke(capturedEmotion, -1))
                {
                    text = "-"
                };
                minusButton.AddToClassList("stepper-button");
                row.Add(minusButton);

                var valueLabel = new Label("0");
                valueLabel.AddToClassList("stepper-row__value");
                row.Add(valueLabel);
                _supportingValueLabelsByEmotion.Add(emotion, valueLabel);

                var plusButton = new Button(() => SupportingIntensityStepRequested?.Invoke(capturedEmotion, 1))
                {
                    text = "+"
                };
                plusButton.AddToClassList("stepper-button");
                row.Add(plusButton);

                _supportingRows.Add(row);
            }
        }
    }
}
