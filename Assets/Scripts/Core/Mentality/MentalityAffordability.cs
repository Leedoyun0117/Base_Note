namespace GameName.Core.Mentality
{
    // 지금 정신력 잔량으로 어떤 유료 행동을 아직 할 수 있는지 한 번에 알려준다.
    // 기억 방 이동은 담지 않는다 — 정신력이 0이어도 무료로 면제되어 항상
    // 가능하므로(MemoryRoomMovementProcessor), "잔량에 따라 못 하게 될 수
    // 있는 행동" 목록에 들지 않는다.
    public readonly struct MentalityAffordability
    {
        public bool CanBasicAnalyze { get; }
        public bool CanAdvancedAnalyze { get; }
        public bool CanCraftAmpoule { get; }

        public MentalityAffordability(bool canBasicAnalyze, bool canAdvancedAnalyze, bool canCraftAmpoule)
        {
            CanBasicAnalyze = canBasicAnalyze;
            CanAdvancedAnalyze = canAdvancedAnalyze;
            CanCraftAmpoule = canCraftAmpoule;
        }
    }
}
