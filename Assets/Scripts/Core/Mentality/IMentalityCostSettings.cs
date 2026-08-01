namespace GameName.Core.Mentality
{
    // 정신력 관련 수치는 밸런싱 중 계속 바뀌므로 전부 외부 설정으로 분리한다.
    // 시향 자체, 그리고 분석실/조향실/계단 사이의 이동은 소모가 없으므로
    // 별도 필드를 두지 않는다(둘 것이 없다는 사실 자체가 계약이다).
    public interface IMentalityCostSettings
    {
        int InitialMentality { get; }
        int MaxMentality { get; }

        int MemoryRoomMoveCost { get; }
        int BasicAnalysisCost { get; }
        int AdvancedAnalysisCost { get; }
        int AmpouleCraftingCost { get; }

        int MemoryRoomFullRestorationRecovery { get; }
    }
}
