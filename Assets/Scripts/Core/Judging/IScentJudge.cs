using GameName.Core.Emotions;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Judging
{
    // 조향 판정 경계. 일치도 계산 공식은 구현체 책임이며, 이 계약은
    // "후보 향 + 방 정답 -> 판정 결과"라는 입출력만 고정한다.
    public interface IScentJudge
    {
        ScentJudgementResult Judge(Scent candidate, MemoryRoomAnswer answer);
    }
}
