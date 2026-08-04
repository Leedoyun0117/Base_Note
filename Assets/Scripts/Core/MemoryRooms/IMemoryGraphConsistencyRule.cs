using System.Collections.Generic;

namespace GameName.Core.MemoryRooms
{
    // 기억 그래프 검사 규칙 하나. RoomDataValidator/IRoomDataConsistencyRule과
    // 같은 조립 방식(규칙마다 별도 구현체)이지만, 검사 대상이 방 하나(단서/정답)가
    // 아니라 그래프 전체(노드 배치, 사다리 연결)라 별도 인터페이스로 둔다 —
    // MemoryRoomData에는 애초에 좌표나 연결 정보가 없어 억지로 끼워 넣을 수
    // 없다.
    public interface IMemoryGraphConsistencyRule
    {
        IReadOnlyList<MemoryGraphIssue> Check(
            IReadOnlyList<MemoryGraphNode> nodes, IReadOnlyList<LadderConnection> ladderConnections);
    }
}
