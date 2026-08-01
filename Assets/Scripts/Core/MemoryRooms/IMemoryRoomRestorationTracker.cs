using GameName.Core.Judging;

namespace GameName.Core.MemoryRooms
{
    // 기억 방 복원 상태 추적 경계.
    // 그래프/이동 처리는 "복원됐는지"만 이 인터페이스로 물어보고, 무엇이 복원을
    // 판단하는지는 몰라도 된다.
    public interface IMemoryRoomRestorationTracker
    {
        bool IsRestored(MemoryRoomId roomId);
        void ReportJudgement(MemoryRoomId roomId, ScentJudgementResult result);
    }
}
