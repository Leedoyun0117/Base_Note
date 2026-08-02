using System.Collections.Generic;

namespace GameName.Core.Clues
{
    // 방 데이터 검사 규칙 하나. 규칙마다 별도 구현체로 나눠 새 규칙을 추가하거나
    // 기존 규칙을 교체하기 쉽게 한다 — 단서/정답 사이에 어떤 관계가 성립해야
    // 하는지는 기획에서 아직 확정되지 않았으므로, 이 인터페이스 뒤에서 계속
    // 바뀔 수 있어야 한다.
    public interface IRoomDataConsistencyRule
    {
        IReadOnlyList<RoomDataIssue> Check(MemoryRoomData data);
    }
}
