namespace GameName.Core.Mentality
{
    // IMentalityCostSettings의 생성자 주입 단순 구현체. 값은 생성 이후 바뀌지
    // 않는다 — 나중에 ScriptableObject 등 다른 설정 소스로 갈아끼울 때도 소비
    // 측 로직은 인터페이스에만 의존하도록 유지하기 위함이다. 실제 밸런싱
    // 수치는 이 클래스 밖(합성 루트/에디터 설정)에서 채워 넣는다.
    public sealed class MentalityCostSettings : IMentalityCostSettings
    {
        public int InitialMentality { get; }
        public int MaxMentality { get; }
        public int MemoryRoomMoveCost { get; }
        public int BasicAnalysisCost { get; }
        public int AdvancedAnalysisCost { get; }
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
    }
}
