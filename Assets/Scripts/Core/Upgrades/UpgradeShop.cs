using System;
using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Inventory;
using GameName.Core.Mentality;
using GameName.Core.Rewards;

namespace GameName.Core.Upgrades
{
    // 진열로 쌓은 감정량을 소비해 영구 업그레이드를 구매하는 처리기.
    //
    // 일부러 IResettable을 구현하지 않고, CommissionSession의 초기화 목록에도
    // 들어가지 않는다 — 여기서 구매한 효과(인벤토리/보관함 용량, 정신력 비용
    // 감소)는 전부 다음 의뢰부터 유지되어야 하는 영구 자산이기 때문이다.
    // "다음 의뢰부터만 적용된다"는 규칙은 별도의 지연 적용 장치 없이도 이미
    // 성립한다 — 이 상점은 의뢰 완료 화면(CommissionStage.Completed)에서만
    // 열리므로, 구매 시점에는 이미 "지금 진행 중인 의뢰"가 없다. 따라서
    // 즉시 적용해도 자동으로 "다음 의뢰부터 적용"이 된다.
    public sealed class UpgradeShop
    {
        private readonly IUpgradeCatalog _catalog;
        private readonly DisplayCollection _displayCollection;
        private readonly IPlayerInventory _inventory;
        private readonly IAmpouleStorage _storage;
        private readonly IMentalityCostAdjuster _costAdjuster;
        private readonly Dictionary<UpgradeCategory, int> _currentLevelByCategory = new Dictionary<UpgradeCategory, int>();

        public UpgradeShop(
            IUpgradeCatalog catalog,
            DisplayCollection displayCollection,
            IPlayerInventory inventory,
            IAmpouleStorage storage,
            IMentalityCostAdjuster costAdjuster)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _displayCollection = displayCollection ?? throw new ArgumentNullException(nameof(displayCollection));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _costAdjuster = costAdjuster ?? throw new ArgumentNullException(nameof(costAdjuster));

            foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory)))
                _currentLevelByCategory[category] = 0;
        }

        public int GetCurrentLevel(UpgradeCategory category) => _currentLevelByCategory[category];

        public bool TryGetNextOption(UpgradeCategory category, out UpgradeOption option) =>
            _catalog.TryGetOption(category, _currentLevelByCategory[category] + 1, out option);

        public UpgradeResult Purchase(UpgradeCategory category)
        {
            if (!TryGetNextOption(category, out var option))
                return UpgradeResult.Failure(UpgradeFailureReason.NoMoreUpgrades);

            if (!_displayCollection.TrySpend(option.Price))
                return UpgradeResult.Failure(UpgradeFailureReason.InsufficientEmotionalValue);

            Apply(option);
            _currentLevelByCategory[category] = option.Level;

            return UpgradeResult.Success(option);
        }

        private void Apply(UpgradeOption option)
        {
            switch (option.Category)
            {
                case UpgradeCategory.InventoryCapacity:
                    _inventory.IncreaseCapacity(option.Amount);
                    break;
                case UpgradeCategory.AmpouleStorageCapacity:
                    _storage.IncreaseCapacity(option.Amount);
                    break;
                case UpgradeCategory.AnalysisEfficiency:
                    _costAdjuster.ReduceAnalysisCost(option.Amount);
                    break;
                case UpgradeCategory.RoomMoveEfficiency:
                    _costAdjuster.ReduceMemoryRoomMoveCost(option.Amount);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(option), "알 수 없는 업그레이드 종류다.");
            }
        }
    }
}
