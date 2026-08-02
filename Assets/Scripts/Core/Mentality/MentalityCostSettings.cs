using System;

namespace GameName.Core.Mentality
{
    // IMentalityCostSettings의 생성자 주입 기본 구현체.
    //
    // IMentalityCostAdjuster도 함께 구현한다 — 처음 설계했을 때는 값이 생성
    // 이후 절대 바뀌지 않는 불변 설정이었지만, 업그레이드 상점이 "분석/이동
    // 비용을 영구히 낮추는" 효과를 내려면 이 객체 자체를 갈아 끼우는 대신
    // (참조를 들고 있는 모든 처리기를 다시 만들어야 한다) 내부 값만 낮추는
    // 편이 훨씬 단순하다. 다만 그 권한은 IMentalityCostAdjuster로만 노출되고
    // UpgradeShop만 그 권한을 받으므로, 일반 소비자 입장에서는 여전히 "읽기
    // 전용 설정"이다.
    public sealed class MentalityCostSettings : IMentalityCostSettings, IMentalityCostAdjuster
    {
        public int InitialMentality { get; }
        public int MaxMentality { get; }
        public int MemoryRoomMoveCost { get; private set; }
        public int BasicAnalysisCost { get; private set; }
        public int AdvancedAnalysisCost { get; private set; }
        public int AmpouleCraftingCost { get; }
        public int MemoryRoomFullRestorationRecovery { get; }

        public MentalityCostSettings(
            int initialMentality,
            int maxMentality,
            int memoryRoomMoveCost,
            int basicAnalysisCost,
            int advancedAnalysisCost,
            int ampouleCraftingCost,
            int memoryRoomFullRestorationRecovery)
        {
            InitialMentality = initialMentality;
            MaxMentality = maxMentality;
            MemoryRoomMoveCost = memoryRoomMoveCost;
            BasicAnalysisCost = basicAnalysisCost;
            AdvancedAnalysisCost = advancedAnalysisCost;
            AmpouleCraftingCost = ampouleCraftingCost;
            MemoryRoomFullRestorationRecovery = memoryRoomFullRestorationRecovery;
        }

        public void ReduceMemoryRoomMoveCost(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "감소량은 양수여야 한다.");

            MemoryRoomMoveCost = Math.Max(0, MemoryRoomMoveCost - amount);
        }

        public void ReduceAnalysisCost(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "감소량은 양수여야 한다.");

            BasicAnalysisCost = Math.Max(0, BasicAnalysisCost - amount);
            AdvancedAnalysisCost = Math.Max(0, AdvancedAnalysisCost - amount);
        }
    }
}
