using GameName.Core.MemoryRooms;

namespace GameName.UI.MemoryRoom
{
    // 이동 패널이 인접 노드 한 칸을 그리는 데 필요한 정보만 모은 UI 전용 DTO.
    // 잠김 여부와 비용은 컨트롤러가 그래프/복원 트래커/이동 처리기에 미리
    // 물어본 결과이며, 이 타입 자체는 아무것도 판단하지 않는다.
    public readonly struct NeighborRowData
    {
        public MemoryGraphNodeId NodeId { get; }
        public MemoryGraphNodeType NodeType { get; }
        public bool IsLocked { get; }
        public int Cost { get; }

        public NeighborRowData(MemoryGraphNodeId nodeId, MemoryGraphNodeType nodeType, bool isLocked, int cost)
        {
            NodeId = nodeId;
            NodeType = nodeType;
            IsLocked = isLocked;
            Cost = cost;
        }
    }
}
