using System;
using System.Collections.Generic;
using System.Globalization;
using GameName.Core.Judging;
using GameName.Core.MemoryRooms;
using GameName.Core.Rewards;
using GameName.Core.Upgrades;
using UnityEngine.UIElements;

namespace GameName.UI.Completion
{
    // 완료 화면의 화면 요소 구성과 표시 갱신만 담당한다. 점수 계산, 업그레이드
    // 구매 가능 여부 판단은 전혀 하지 않는다 — 컨트롤러가 Core로 얻은 결론을
    // 그대로 그릴 뿐이다.
    public sealed class CompletionScreenView
    {
        private readonly Label _averageAccuracyLabel;
        private readonly Label _giftLabel;
        private readonly VisualElement _roomResultsList;
        private readonly Label _emotionalBalanceLabel;
        private readonly VisualElement _upgradeList;
        private readonly Button _nextCommissionButton;
        private readonly Label _nextCommissionMessage;

        public event Action<UpgradeCategory> UpgradePurchaseRequested;
        public event Action NextCommissionRequested;

        public CompletionScreenView(VisualElement root)
        {
            _averageAccuracyLabel = root.Q<Label>("average-accuracy-label");
            _giftLabel = root.Q<Label>("gift-label");
            _roomResultsList = root.Q<VisualElement>("room-results-list");
            _emotionalBalanceLabel = root.Q<Label>("emotional-balance-label");
            _upgradeList = root.Q<VisualElement>("upgrade-list");
            _nextCommissionButton = root.Q<Button>("next-commission-button");
            _nextCommissionMessage = root.Q<Label>("next-commission-message");

            _nextCommissionButton.clicked += () => NextCommissionRequested?.Invoke();
        }

        public void SetNoResultYet()
        {
            _averageAccuracyLabel.text = "-";
            _giftLabel.text = "-";
            _roomResultsList.Clear();
        }

        public void SetResult(double averageAccuracy, Gift gift)
        {
            _averageAccuracyLabel.text = (averageAccuracy * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";
            _giftLabel.text = $"감정량 +{gift.EmotionalValue} · \"{gift.ReactionDialogue}\"";
        }

        public void SetRoomResults(IReadOnlyDictionary<MemoryRoomId, ScentJudgementResult> results)
        {
            _roomResultsList.Clear();
            foreach (var pair in results)
            {
                var row = new VisualElement();
                row.AddToClassList("room-result-row");

                var nameLabel = new Label(pair.Key.Value);
                nameLabel.AddToClassList("room-result-row__name");
                row.Add(nameLabel);

                var baseText = pair.Value.IsBaseEmotionCorrect ? "바탕 감정 일치" : "바탕 감정 불일치";
                var accuracyText = (pair.Value.Accuracy * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";
                var detailLabel = new Label($"{baseText} · 정확도 {accuracyText}");
                detailLabel.AddToClassList("room-result-row__detail");
                row.Add(detailLabel);

                _roomResultsList.Add(row);
            }
        }

        public void SetEmotionalBalance(int value) => _emotionalBalanceLabel.text = $"보유 감정량: {value}";

        public void SetUpgrades(IReadOnlyList<UpgradeRowData> rows, int availableEmotionalValue)
        {
            _upgradeList.Clear();
            foreach (var data in rows)
                _upgradeList.Add(CreateUpgradeRow(data, availableEmotionalValue));
        }

        public void SetNextCommissionButtonEnabled(bool enabled) => _nextCommissionButton.SetEnabled(enabled);

        public void SetNextCommissionMessage(string message)
        {
            _nextCommissionMessage.text = message ?? string.Empty;
            _nextCommissionMessage.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private VisualElement CreateUpgradeRow(UpgradeRowData data, int availableEmotionalValue)
        {
            var row = new VisualElement();
            row.AddToClassList("upgrade-row");

            var nameLabel = new Label($"{DescribeCategory(data.Category)} (Lv.{data.CurrentLevel})");
            nameLabel.AddToClassList("upgrade-row__name");
            row.Add(nameLabel);

            if (data.NextOption.HasValue)
            {
                var option = data.NextOption.Value;
                var descriptionLabel = new Label($"{option.Description} · 감정량 {option.Price}");
                descriptionLabel.AddToClassList("upgrade-row__description");
                row.Add(descriptionLabel);

                var buyButton = new Button(() => UpgradePurchaseRequested?.Invoke(data.Category)) { text = "구매" };
                buyButton.AddToClassList("upgrade-row__buy-button");
                buyButton.SetEnabled(availableEmotionalValue >= option.Price);
                row.Add(buyButton);
            }
            else
            {
                var maxedLabel = new Label("최고 레벨");
                maxedLabel.AddToClassList("upgrade-row__description");
                row.Add(maxedLabel);
            }

            return row;
        }

        private static string DescribeCategory(UpgradeCategory category)
        {
            switch (category)
            {
                case UpgradeCategory.InventoryCapacity: return "인벤토리 칸";
                case UpgradeCategory.AmpouleStorageCapacity: return "앰플 보관 칸";
                case UpgradeCategory.AnalysisEfficiency: return "분석 효율";
                case UpgradeCategory.RoomMoveEfficiency: return "방 이동 효율";
                default: return category.ToString();
            }
        }
    }
}
