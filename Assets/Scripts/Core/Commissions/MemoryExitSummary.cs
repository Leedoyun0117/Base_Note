namespace GameName.Core.Commissions
{
    // 이탈(현실로 복귀) 확인 화면이 "지금 나가면 무엇을 잃는지" 보여주는 데
    // 쓰는 값. 개수만 담는다 — 어떤 단서인지, 어떤 방의 정답이 무엇인지 같은
    // 진실 데이터는 이 타입에 절대 들어오지 않는다.
    public readonly struct MemoryExitSummary
    {
        public int UnanalyzedClueCount { get; }
        public int UnrestoredRoomCount { get; }

        public MemoryExitSummary(int unanalyzedClueCount, int unrestoredRoomCount)
        {
            UnanalyzedClueCount = unanalyzedClueCount;
            UnrestoredRoomCount = unrestoredRoomCount;
        }
    }
}
