namespace GameName.Core.MemoryRooms
{
    // 기억 방 그래프의 노드 하나. 식별자, 역할(기억 방/허브 종류), 지도 배치
    // 좌표를 담는다. 좌표는 지도를 그리기 위한 값일 뿐 이동 가능 여부(연결
    // 목록의 몫)에는 관여하지 않는다 — 다만 사다리 방향만은 이 좌표(Row)로
    // 검증한다(LadderRespectsDepthOrderRule).
    public sealed class MemoryGraphNode
    {
        public MemoryGraphNodeId Id { get; }
        public MemoryGraphNodeType Type { get; }
        public MemoryGraphCoordinate Coordinate { get; }

        public MemoryGraphNode(MemoryGraphNodeId id, MemoryGraphNodeType type, MemoryGraphCoordinate coordinate)
        {
            Id = id;
            Type = type;
            Coordinate = coordinate;
        }
    }
}
