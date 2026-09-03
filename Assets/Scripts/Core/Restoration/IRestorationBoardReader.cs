using System.Collections.Generic;
using GameName.Core.Memories;

namespace GameName.Core.Restoration
{
    // 복원도를 읽기만 하는 표면. 화면이 색별로 노드·간선을 그릴 때, 그리고
    // 편집 처리 전에 지금 상태를 확인할 때 쓴다.
    //
    // 색마다 독립된 보드가 하나씩 있다(R/G/B). 조회는 항상 색을 먼저 받는다 —
    // 서로 다른 색의 노드는 같은 화면에 섞이지 않기 때문이다.
    public interface IRestorationBoardReader
    {
        // 그 색으로 추출이 한 번이라도 있었는가(= 뿌리 노드가 생겼는가).
        bool HasColorRoot(MemoryColor color);

        // 그 색의 뿌리 노드. 아직 없으면 false.
        bool TryGetColorRoot(MemoryColor color, out RestorationNode root);

        // 그 색 보드의 모든 노드(뿌리 포함). 생성 순서.
        IReadOnlyList<RestorationNode> NodesOf(MemoryColor color);

        // 그 색 보드의 모든 간선(잠긴 것 포함). 생성 순서.
        IReadOnlyList<RestorationEdge> EdgesOf(MemoryColor color);

        // 색과 무관하게 식별자로 노드 하나를 찾는다.
        bool TryGetNode(RestorationNodeId nodeId, out RestorationNode node);

        // 색과 무관하게 식별자로 간선 하나를 찾는다.
        bool TryGetEdge(RestorationEdgeId edgeId, out RestorationEdge edge);
    }
}
