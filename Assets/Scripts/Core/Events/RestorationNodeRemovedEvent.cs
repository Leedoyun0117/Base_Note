using GameName.Core.Memories;
using GameName.Core.Restoration;

namespace GameName.Core.Events
{
    // 플레이어 노드 하나가 복원도에서 지워졌다는 사실. 색을 함께 싣는 이유는
    // 받는 쪽이 어느 색 보드를 다시 그려야 하는지 알아야 하기 때문이다(지워진
    // 뒤에는 노드를 조회해 색을 알아낼 수 없다).
    //
    // 이 노드에 닿아 있던 플레이어 간선은 각각 RestorationEdgeRemovedEvent로
    // 먼저 알린 뒤 이 사건이 온다.
    public readonly struct RestorationNodeRemovedEvent
    {
        public RestorationNodeId NodeId { get; }
        public MemoryColor Color { get; }

        public RestorationNodeRemovedEvent(RestorationNodeId nodeId, MemoryColor color)
        {
            NodeId = nodeId;
            Color = color;
        }
    }
}
