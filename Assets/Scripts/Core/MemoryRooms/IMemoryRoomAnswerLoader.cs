using System.Collections.Generic;

namespace GameName.Core.MemoryRooms
{
    // 방별 정답 전체를 다른 의뢰의 정답으로 통째로 갈아 끼우는 권한 하나만
    // 표현하는 좁은 경계. IMemoryRoomAnswerRepository/IMemoryRoomPublicInfoRepository
    // (정상 조회 인터페이스)에는 이 능력이 없다 — 오직 GameSession의 의뢰 교체
    // 절차만 이 인터페이스를 받는다.
    public interface IMemoryRoomAnswerLoader
    {
        void Load(IReadOnlyList<MemoryRoomAnswer> answers);
    }
}
