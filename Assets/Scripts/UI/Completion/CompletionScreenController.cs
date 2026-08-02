using System;
using System.Collections.Generic;
using GameName.Core.Commissions;
using GameName.Core.Rewards;
using GameName.Core.Upgrades;

namespace GameName.UI.Completion
{
    // 완료 화면의 입력 처리와 Core 연동을 담당한다. 점수/보상은 이미
    // CommissionCompletionProcessor가 계산해 둔 결과(lastCompletionResultProvider)를
    // 그대로 읽어 보여줄 뿐이고, 업그레이드 구매 가능 여부는 UpgradeShop에게
    // 그대로 위임한다.
    public sealed class CompletionScreenController : IDisposable
    {
        private static readonly UpgradeCategory[] AllCategories =
        {
            UpgradeCategory.InventoryCapacity, UpgradeCategory.AmpouleStorageCapacity,
            UpgradeCategory.AnalysisEfficiency, UpgradeCategory.RoomMoveEfficiency,
        };

        private readonly CompletionScreenView _view;
        private readonly DisplayCollection _displayCollection;
        private readonly UpgradeShop _upgradeShop;
        private readonly Func<CommissionCompletionResult> _lastCompletionResultProvider;
        private readonly Func<CommissionData> _currentCommissionProvider;
        private readonly IReadOnlyList<CommissionData> _allCommissions;
        private readonly Action<CommissionData> _loadCommission;

        public CompletionScreenController(
            CompletionScreenView view,
            DisplayCollection displayCollection,
            UpgradeShop upgradeShop,
            Func<CommissionCompletionResult> lastCompletionResultProvider,
            Func<CommissionData> currentCommissionProvider,
            IReadOnlyList<CommissionData> allCommissions,
            Action<CommissionData> loadCommission)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _displayCollection = displayCollection ?? throw new ArgumentNullException(nameof(displayCollection));
            _upgradeShop = upgradeShop ?? throw new ArgumentNullException(nameof(upgradeShop));
            _lastCompletionResultProvider =
                lastCompletionResultProvider ?? throw new ArgumentNullException(nameof(lastCompletionResultProvider));
            _currentCommissionProvider =
                currentCommissionProvider ?? throw new ArgumentNullException(nameof(currentCommissionProvider));
            _allCommissions = allCommissions ?? throw new ArgumentNullException(nameof(allCommissions));
            _loadCommission = loadCommission ?? throw new ArgumentNullException(nameof(loadCommission));

            _view.UpgradePurchaseRequested += OnUpgradePurchaseRequested;
            _view.NextCommissionRequested += OnNextCommissionRequested;

            Refresh();
        }

        private void Refresh()
        {
            var result = _lastCompletionResultProvider();
            if (result != null)
            {
                _view.SetResult(result.AverageAccuracy, result.Gift);
                _view.SetRoomResults(result.RoomResults);
            }
            else
            {
                _view.SetNoResultYet();
            }

            _view.SetEmotionalBalance(_displayCollection.AvailableEmotionalValue);

            var rows = new List<UpgradeRowData>(AllCategories.Length);
            foreach (var category in AllCategories)
            {
                var currentLevel = _upgradeShop.GetCurrentLevel(category);
                var nextOption = _upgradeShop.TryGetNextOption(category, out var option)
                    ? option
                    : (UpgradeOption?)null;
                rows.Add(new UpgradeRowData(category, currentLevel, nextOption));
            }
            _view.SetUpgrades(rows, _displayCollection.AvailableEmotionalValue);

            _view.SetNextCommissionButtonEnabled(TryGetNextCommission(out _));
        }

        private void OnUpgradePurchaseRequested(UpgradeCategory category)
        {
            _upgradeShop.Purchase(category);
            Refresh();
        }

        private void OnNextCommissionRequested()
        {
            if (!TryGetNextCommission(out var next))
                return;

            _loadCommission(next);
            _view.SetNextCommissionMessage(null);
        }

        private bool TryGetNextCommission(out CommissionData next)
        {
            var currentId = _currentCommissionProvider().Id;
            for (var i = 0; i < _allCommissions.Count; i++)
            {
                if (!_allCommissions[i].Id.Equals(currentId))
                    continue;

                next = _allCommissions[(i + 1) % _allCommissions.Count];
                return true;
            }

            next = null;
            return false;
        }

        public void Dispose()
        {
            _view.UpgradePurchaseRequested -= OnUpgradePurchaseRequested;
            _view.NextCommissionRequested -= OnNextCommissionRequested;
        }
    }
}
