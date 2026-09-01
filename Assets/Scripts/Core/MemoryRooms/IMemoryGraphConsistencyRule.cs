using System.Collections.Generic;

namespace GameName.Core.MemoryRooms
{
    // 기억 그래프 검사 규칙 하나. 규칙마다 별도 구현체를 두고
    // MemoryGraphValidator가 그것들을 차례로 돌린다 — 새 규칙을 더해도 검사기
    // 자체는 바뀌지 않는다. 검사 대상은 그래프 전체(노드 배치, 사다리 연결)다.
    public interface IMemoryGraphConsistencyRule
    {
        IReadOnlyList<MemoryGraphIssue> Check(
            IReadOnlyList<MemoryGraphNode> nodes, IReadOnlyList<LadderConnection> ladderConnections);
    }
}
