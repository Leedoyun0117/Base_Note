using GameName.Core.Memories;
using GameName.Core.Restoration;

namespace GameName.Core.Events
{
    // 플레이어 간선 하나가 복원도에서 지워졌다는 사실. 직접 지운 경우와, 끝점
    // 플레이어 노드가 지워지며 함께 사라진 경우 모두 이 사건으로 알린다.
    //
    // 색을 함께 싣는 이유는 RestorationNodeRemovedEvent와 같다 — 지워진 뒤에는
    // 간선을 조회할 수 없다.
    public readonly struct RestorationEdgeRemovedEvent
    {
        public RestorationEdgeId EdgeId { get; }
        public MemoryColor Color { get; }

        public RestorationEdgeRemovedEvent(RestorationEdgeId edgeId, MemoryColor color)
        {
            EdgeId = edgeId;
            Color = color;
        }
    }
}
