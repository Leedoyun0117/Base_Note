namespace GameName.Core.MemoryRooms
{
    // 두 노드 사이의 조건 없는 양방향 연결.
    // 가로 연결(문), 허브-허브 연결, 허브-방 연결이 전부 이 형태다 — 통행 가능
    // 여부에 조건이 없다는 점에서 셋을 동일하게 다룰 수 있다. 정신력 비용은
    // 연결 종류가 아니라 노드 종류(기억 방인지 허브인지)로 계산되므로, 이 타입은
    // 비용을 전혀 알지 못한다.
    public readonly struct OpenConnection
    {
        public MemoryGraphNodeId NodeA { get; }
        public MemoryGraphNodeId NodeB { get; }

        public OpenConnection(MemoryGraphNodeId nodeA, MemoryGraphNodeId nodeB)
        {
            NodeA = nodeA;
            NodeB = nodeB;
        }
    }
}
