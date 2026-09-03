using GameName.Core.Memories;

namespace GameName.Core.Restoration
{
    // 복원도 위의 간선 한 개. 같은 색 보드 안의 두 노드를 잇는다.
    //
    // IsLocked가 true면 추출과 함께 자동으로 생긴 간선(뿌리 ↔ 단서 노드)이라
    // 플레이어가 지울 수 없다. false면 플레이어가 직접 그은 연결이다.
    //
    // 방향은 담되(From/To) 의미는 두지 않는다 — 추리 보드의 연결은 대칭이다.
    // From/To는 "이 두 노드가 이어져 있다"를 적어 두는 순서일 뿐이다.
    public sealed class RestorationEdge
    {
        public RestorationEdgeId Id { get; }
        public RestorationNodeId From { get; }
        public RestorationNodeId To { get; }
        public MemoryColor Color { get; }
        public bool IsLocked { get; }

        public RestorationEdge(
            RestorationEdgeId id,
            RestorationNodeId from,
            RestorationNodeId to,
            MemoryColor color,
            bool isLocked)
        {
            Id = id;
            From = from;
            To = to;
            Color = color;
            IsLocked = isLocked;
        }

        // 이 간선이 그 노드에 닿아 있는가. 플레이어 노드를 지울 때 함께 지울
        // 간선을 고르는 데 쓴다.
        public bool Touches(RestorationNodeId nodeId) => From == nodeId || To == nodeId;
    }
}
