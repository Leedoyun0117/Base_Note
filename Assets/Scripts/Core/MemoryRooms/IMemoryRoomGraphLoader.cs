using System.Collections.Generic;

namespace GameName.Core.MemoryRooms
{
    // 그래프 구조 전체를 다른 의뢰의 구조로 통째로 갈아 끼우는 권한 하나만
    // 표현하는 좁은 경계. IResettable과 다르다 — 비우는 것이 아니라 완전히
    // 다른 노드/연결 집합으로 대체하는 것이라 "무엇으로 바꿀지"를 인자로
    // 받아야 하기 때문에 별도 인터페이스로 뺐다. MemoryRoomGraph를 정상적으로
    // 참조하는 처리기들은 이 인터페이스를 주입받지 않는다 — 오직 GameSession의
    // 의뢰 교체 절차만 받는다.
    public interface IMemoryRoomGraphLoader
    {
        void Load(
            IReadOnlyList<MemoryGraphNode> nodes,
            IReadOnlyList<OpenConnection> openConnections,
            IReadOnlyList<LadderConnection> ladderConnections);
    }
}
