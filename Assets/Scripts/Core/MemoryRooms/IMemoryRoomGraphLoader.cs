using System.Collections.Generic;

namespace GameName.Core.MemoryRooms
{
    // 그래프 구조 전체를 다른 노드/연결 집합으로 통째로 갈아 끼우는 권한 하나만
    // 표현하는 좁은 경계. MemoryRoomGraph를 정상적으로 참조하는 처리기들은 이
    // 인터페이스를 주입받지 않는다 — 오직 구성 루트(GameSession)만 받는다.
    public interface IMemoryRoomGraphLoader
    {
        void Load(
            IReadOnlyList<MemoryGraphNode> nodes,
            IReadOnlyList<OpenConnection> openConnections,
            IReadOnlyList<LadderConnection> ladderConnections);
    }
}
