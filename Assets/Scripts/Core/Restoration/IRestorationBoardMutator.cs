using GameName.Core.Clues;
using GameName.Core.Memories;

namespace GameName.Core.Restoration
{
    // 복원도를 바꾸는 표면. 읽기(IRestorationBoardReader)와 갈라 둔다.
    //
    // 자동 생성(뿌리·단서 노드와 그 잠긴 간선)은 추출을 듣는 리스너만
    // 부른다(EnsureColorRoot / AddClueNode). 나머지는 전부 화면이 직접 부르는
    // 단순 편집이라 별도 처리기를 두지 않았다 — 판정이 거의 없는 CRUD다.
    public interface IRestorationBoardMutator
    {
        // 그 색의 뿌리 노드를 보장한다. 없으면 만들고, 있으면 있던 것을 그대로
        // 돌려준다(멱등). 같은 색으로 두 번째 추출이 와도 뿌리는 하나뿐이다.
        RestorationNode EnsureColorRoot(MemoryColor color);

        // 추출된 단서 하나에 대응하는 노드를 만들고 그 색 뿌리에 잠긴 간선으로
        // 잇는다. 뿌리가 아직 없으면 함께 만든다. 라벨(단서 이름)은 잠긴다.
        RestorationNode AddClueNode(MemoryColor color, ClueId clueId, string displayName);

        // 플레이어가 자유 노드를 놓는다. 그 색으로 추출이 한 번도 없었으면
        // (뿌리 없음) 실패한다 — 아직 존재하지 않는 보드다.
        RestorationNodeResult AddPlayerNode(MemoryColor color, string label, BoardPosition position);

        // 플레이어 노드의 이름을 바꾼다. PlayerAuthored가 아니면 실패.
        RestorationEditResult RenamePlayerNode(RestorationNodeId nodeId, string newLabel);

        // 노드를 옮긴다. 종류와 무관하게 모두 허용된다 — 배치는 "내용 편집"이
        // 아니다.
        RestorationEditResult MoveNode(RestorationNodeId nodeId, BoardPosition position);

        // 플레이어가 두 노드를 잇는다. 두 노드가 같은 색 보드에 함께 있어야 하고,
        // 자기 자신과는 이을 수 없다. 생기는 간선은 잠기지 않는다.
        RestorationEdgeResult AddPlayerEdge(RestorationNodeId fromNodeId, RestorationNodeId toNodeId);

        // 플레이어가 그은 간선을 지운다. 잠긴 간선이면 실패.
        RestorationEditResult RemovePlayerEdge(RestorationEdgeId edgeId);

        // 플레이어 노드를 지운다. PlayerAuthored가 아니면 실패. 그 노드에 닿은
        // 플레이어 간선도 함께 지운다(잠긴 간선은 애초에 플레이어 노드에 붙지
        // 않는다).
        RestorationEditResult RemovePlayerNode(RestorationNodeId nodeId);
    }
}
