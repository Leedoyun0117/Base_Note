namespace GameName.Core.MemoryRooms
{
    // 기억 방 그래프에 등장하는 노드의 역할.
    // 기억 방 하나이거나, 네 허브(계단/분석실/조향실/이탈) 중 하나다. 이탈
    // 노드는 계단과 별개의 공간이다 — 계단은 그래프 위를 오가는 평범한 지점
    // 중 하나로 계속 쓰이고, 이 노드만 눌렀을 때 현실로 돌아가는 절차가
    // 시작된다.
    public enum MemoryGraphNodeType
    {
        MemoryRoom,
        Staircase,
        AnalysisRoom,
        PerfumeryRoom,
        Exit
    }
}
