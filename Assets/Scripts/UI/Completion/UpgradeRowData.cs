using GameName.Core.Upgrades;

namespace GameName.UI.Completion
{
    // 업그레이드 목록 한 줄을 그리는 데 필요한 정보만 묶은 화면 전용 값.
    // NextOption이 없으면(null) 더 살 수 있는 다음 단계가 없다는 뜻이다.
    public readonly struct UpgradeRowData
    {
        public UpgradeCategory Category { get; }
        public int CurrentLevel { get; }
        public UpgradeOption? NextOption { get; }

        public UpgradeRowData(UpgradeCategory category, int currentLevel, UpgradeOption? nextOption)
        {
            Category = category;
            CurrentLevel = currentLevel;
            NextOption = nextOption;
        }
    }
}
