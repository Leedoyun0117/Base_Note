using System;

namespace GameName.Core.Upgrades
{
    // 업그레이드 등급 하나 — "어떤 종류를, 몇 레벨째, 얼마에, 얼마나 좋아지게"를
    // 표현하는 불변 값. 가격/효과량을 코드에 상수로 박아두지 않고
    // UpgradeCatalog 생성자로만 주입하기 위한 자료 단위다.
    public readonly struct UpgradeOption
    {
        public UpgradeCategory Category { get; }

        // 1부터 시작하는 레벨. 같은 Category에서 레벨이 낮은 옵션부터 순서대로
        // 구매해야 한다 — UpgradeShop이 "지금 레벨+1"만 구매 대상으로 찾는다.
        public int Level { get; }

        public int Price { get; }
        public int Amount { get; }
        public string Description { get; }

        public UpgradeOption(UpgradeCategory category, int level, int price, int amount, string description)
        {
            if (level <= 0)
                throw new ArgumentOutOfRangeException(nameof(level), "레벨은 1 이상이어야 한다.");
            if (price < 0)
                throw new ArgumentOutOfRangeException(nameof(price), "가격은 음수일 수 없다.");
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "효과량은 양수여야 한다.");
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("설명은 비어 있을 수 없다.", nameof(description));

            Category = category;
            Level = level;
            Price = price;
            Amount = amount;
            Description = description;
        }
    }
}
