using System;
using GameName.Core.Emotions;
using GameName.Core.Judging;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Journal
{
    // 시향 결과 하나의 불변 기록. AmpouleRecord에 매달려서만 노출된다 —
    // 어느 앰플을 시향한 결과인지는 그 연결로 이미 드러나기 때문에 이 타입
    // 자체는 앰플 식별자를 다시 갖지 않는다.
    public sealed class ScentTestRecord
    {
        public MemoryRoomId RoomId { get; }
        public Scent TestedScent { get; }
        public ScentJudgementResult Result { get; }

        public ScentTestRecord(MemoryRoomId roomId, Scent testedScent, ScentJudgementResult result)
        {
            RoomId = roomId;
            TestedScent = testedScent ?? throw new ArgumentNullException(nameof(testedScent));
            Result = result;
        }
    }
}
