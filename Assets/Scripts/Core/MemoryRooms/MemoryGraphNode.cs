namespace GameName.Core.MemoryRooms
{
    // 기억 방 그래프의 노드 하나. 식별자와 역할(기억 방/허브 종류)만 담는다.
    public sealed class MemoryGraphNode
    {
        public MemoryGraphNodeId Id { get; }
        public MemoryGraphNodeType Type { get; }

        public MemoryGraphNode(MemoryGraphNodeId id, MemoryGraphNodeType type)
        {
            Id = id;
            Type = type;
        }
    }
}
