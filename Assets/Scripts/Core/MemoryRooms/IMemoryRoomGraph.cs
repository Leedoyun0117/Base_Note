using System.Collections.Generic;

namespace GameName.Core.MemoryRooms
{
    // 기억 방 그래프를 읽기만 하는 경계. 이동 처리기/내비게이션 화면 등
    // "지금 구조가 어떻게 생겼는가"만 물어보는 모든 소비자는 이 인터페이스로만
    // 그래프를 받는다 — 구조 자체를 통째로 갈아 끼우는 권한(Load)은
    // IMemoryRoomGraphLoader로 따로 분리되어 있어, 이 인터페이스만 쥔 소비자는
    // 다른 그래프로 바꿔치기할 방법이 없다.
    public interface IMemoryRoomGraph
    {
        bool TryGetNode(MemoryGraphNodeId id, out MemoryGraphNode node);
        bool AreOpenlyConnected(MemoryGraphNodeId a, MemoryGraphNodeId b);
        IReadOnlyList<MemoryGraphNodeId> GetNeighborIds(MemoryGraphNodeId nodeId);
        bool TryGetLadderLowerRoom(MemoryGraphNodeId a, MemoryGraphNodeId b, out MemoryRoomId lowerRoomId);

        // 지도처럼 그래프 전체를 한 번에 그려야 하는 소비자를 위한 전수 열거.
        // 위 조회 전용 메서드들과 달리 "구조 전체"가 필요할 때만 쓴다.
        IReadOnlyList<MemoryGraphNode> Nodes { get; }
        IReadOnlyList<OpenConnection> OpenConnections { get; }
        IReadOnlyList<LadderConnection> LadderConnections { get; }
    }
}
