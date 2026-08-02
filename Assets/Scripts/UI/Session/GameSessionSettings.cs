using System;
using GameName.Core.Ampoules;
using GameName.Core.Inventory;
using GameName.Core.Judging;
using GameName.Core.Mentality;
using GameName.Core.Validation;

namespace GameName.UI.Session
{
    // GameSession을 조립하는 데 필요한 밸런싱 수치 묶음. 각 설정은 이미 그
    // 자체로 인터페이스가 있는 Core 개념이라, 이 타입은 그것들을 한데 모아
    // 생성자 하나로 주입할 수 있게 하는 역할만 한다 — 새 규칙을 추가하지 않는다.
    public sealed class GameSessionSettings
    {
        public IMentalityCostSettings MentalityCostSettings { get; }
        public IScentJudgementSettings JudgementSettings { get; }
        public IEmotionCompositionPolicy CompositionPolicy { get; }
        public IInventorySettings InventorySettings { get; }
        public IAmpouleStorageSettings StorageSettings { get; }
        public IAmpouleIdGenerator AmpouleIdGenerator { get; }

        public GameSessionSettings(
            IMentalityCostSettings mentalityCostSettings,
            IScentJudgementSettings judgementSettings,
            IEmotionCompositionPolicy compositionPolicy,
            IInventorySettings inventorySettings,
            IAmpouleStorageSettings storageSettings,
            IAmpouleIdGenerator ampouleIdGenerator)
        {
            MentalityCostSettings = mentalityCostSettings ?? throw new ArgumentNullException(nameof(mentalityCostSettings));
            JudgementSettings = judgementSettings ?? throw new ArgumentNullException(nameof(judgementSettings));
            CompositionPolicy = compositionPolicy ?? throw new ArgumentNullException(nameof(compositionPolicy));
            InventorySettings = inventorySettings ?? throw new ArgumentNullException(nameof(inventorySettings));
            StorageSettings = storageSettings ?? throw new ArgumentNullException(nameof(storageSettings));
            AmpouleIdGenerator = ampouleIdGenerator ?? throw new ArgumentNullException(nameof(ampouleIdGenerator));
        }
    }
}
