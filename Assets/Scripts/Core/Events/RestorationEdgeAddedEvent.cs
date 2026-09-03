using GameName.Core.Restoration;

namespace GameName.Core.Events
{
    // 복원도에 간선이 하나 생겼다는 사실. 자동 생성된 잠긴 간선(뿌리 ↔ 단서)과
    // 플레이어가 그은 간선을 똑같이 이 사건으로 알린다(IsLocked로 구분).
    public readonly struct RestorationEdgeAddedEvent
    {
        public RestorationEdge Edge { get; }

        public RestorationEdgeAddedEvent(RestorationEdge edge)
        {
            Edge = edge;
        }
    }
}
