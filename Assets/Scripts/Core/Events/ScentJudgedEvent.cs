using GameName.Core.Emotions;
using GameName.Core.Judging;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Events
{
    // 플레이어가 기억 방에서 향을 시험(시향)할 때마다 발행된다.
    public readonly struct ScentJudgedEvent
    {
        public MemoryRoomId RoomId { get; }
        public Scent TestedScent { get; }
        public ScentJudgementResult Result { get; }

        public ScentJudgedEvent(MemoryRoomId roomId, Scent testedScent, ScentJudgementResult result)
        {
            RoomId = roomId;
            TestedScent = testedScent;
            Result = result;
        }
    }
}
