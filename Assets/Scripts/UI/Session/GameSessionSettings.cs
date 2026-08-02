using System;
using GameName.Core.Ampoules;
using GameName.Core.Inventory;
using GameName.Core.Judging;
using GameName.Core.Mentality;
using GameName.Core.Upgrades;
using GameName.Core.Validation;

namespace GameName.UI.Session
{
    // GameSession을 조립하는 데 필요한 밸런싱 수치 묶음. 각 설정은 이미 그
    // 자체로 인터페이스가 있는 Core 개념이라, 이 타입은 그것들을 한데 모아
    // 생성자 하나로 주입할 수 있게 하는 역할만 한다 — 새 규칙을 추가하지 않는다.
    public sealed class GameSessionSettings
    {
        public IMentalityCostSettings MentalityCostSettings { get; }

        // MentalityCostSettings와 같은 객체를 가리킨다 — 업그레이드 상점만
        // 이 좁은 인터페이스로 비용을 낮출 권한을 받고, 다른 모든 소비자는
        // 위 IMentalityCostSettings(읽기 전용)로만 받는다는 경계를 GameSession
        // 조립 시점에도 캐스팅 없이 그대로 지키기 위해 별도 필드로 둔다.
        public IMentalityCostAdjuster MentalityCostAdjuster { get; }

        public IScentJudgementSettings JudgementSettings { get; }
        public IEmotionCompositionPolicy CompositionPolicy { get; }
        public IInventorySettings InventorySettings { get; }
        public IAmpouleStorageSettings StorageSettings { get; }
        public IAmpouleIdGenerator AmpouleIdGenerator { get; }
        public IUpgradeCatalog UpgradeCatalog { get; }

        public GameSessionSettings(
            IMentalityCostSettings mentalityCostSettings,
            IMentalityCostAdjuster mentalityCostAdjuster,
            IScentJudgementSettings judgementSettings,
            IEmotionCompositionPolicy compositionPolicy,
            IInventorySettings inventorySettings,
            IAmpouleStorageSettings storageSettings,
            IAmpouleIdGenerator ampouleIdGenerator,
            IUpgradeCatalog upgradeCatalog)
        {
            MentalityCostSettings = mentalityCostSettings ?? throw new ArgumentNullException(nameof(mentalityCostSettings));
            MentalityCostAdjuster = mentalityCostAdjuster ?? throw new ArgumentNullException(nameof(mentalityCostAdjuster));
            JudgementSettings = judgementSettings ?? throw new ArgumentNullException(nameof(judgementSettings));
            CompositionPolicy = compositionPolicy ?? throw new ArgumentNullException(nameof(compositionPolicy));
            InventorySettings = inventorySettings ?? throw new ArgumentNullException(nameof(inventorySettings));
            StorageSettings = storageSettings ?? throw new ArgumentNullException(nameof(storageSettings));
            AmpouleIdGenerator = ampouleIdGenerator ?? throw new ArgumentNullException(nameof(ampouleIdGenerator));
            UpgradeCatalog = upgradeCatalog ?? throw new ArgumentNullException(nameof(upgradeCatalog));
        }
    }
}
