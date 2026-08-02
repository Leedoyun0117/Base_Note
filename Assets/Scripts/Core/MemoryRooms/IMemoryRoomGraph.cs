using System.Collections.Generic;

namespace GameName.Core.MemoryRooms
{
    // 기억 방 그래프를 읽기만 하는 경계. 이동 처리기/내비게이션 화면 등
    // "지금 구조가 어떻게 생겼는가"만 물어보는 모든 소비자는 이 인터페이스로만
    // 그래프를 받는다 — 구조 자체를 통째로 갈아 끼우는 권한(Load)은
    // IMemoryRoomGraphLoader로 따로 분리되어 있어, 이 인터페이스만 쥔 소비자는
    // 다른 의뢰의 그래프로 바꿔치기할 방법이 없다.
    public interface IMemoryRoomGraph
    {
        bool TryGetNode(MemoryGraphNodeId id, out MemoryGraphNode node);
        bool AreOpenlyConnected(MemoryGraphNodeId a, MemoryGraphNodeId b);
        IReadOnlyList<MemoryGraphNodeId> GetNeighborIds(MemoryGraphNodeId nodeId);
        bool TryGetLadderLowerRoom(MemoryGraphNodeId a, MemoryGraphNodeId b, out MemoryRoomId lowerRoomId);
    }
}
