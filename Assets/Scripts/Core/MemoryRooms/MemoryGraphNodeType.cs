namespace GameName.Core.MemoryRooms
{
    // 기억 방 그래프에 등장하는 노드의 역할.
    // 기억 방 하나이거나, 세 허브(계단/분석실/조향실) 중 하나다.
    public enum MemoryGraphNodeType
    {
        MemoryRoom,
        Staircase,
        AnalysisRoom,
        PerfumeryRoom
    }
}
