using System;
using GameName.Core.Ampoules;
using GameName.Core.Emotions;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Journal
{
    // 제작된 앰플 하나의 불변 기록. 살아있는 Ampoule 객체를 참조하지 않는다 —
    // 시향하면 그 객체는 인벤토리에서 사라지므로, 참조를 들고 있으면 "사라진
    // 물건을 계속 가리키는" 이상한 상태가 된다. 대신 식별자·목표 방·배합만
    // 복사해 독립적으로 들고, State/TestResult는 Journal이 조회 시점에
    // 계산해 채워 넣는다.
    public sealed class AmpouleRecord
    {
        public AmpouleId AmpouleId { get; }
        public MemoryRoomId TargetRoomId { get; }
        public Scent Scent { get; }
        public AmpouleRecordState State { get; }
        public ScentTestRecord TestResult { get; }

        public AmpouleRecord(
            AmpouleId ampouleId,
            MemoryRoomId targetRoomId,
            Scent scent,
            AmpouleRecordState state,
            ScentTestRecord testResult)
        {
            AmpouleId = ampouleId;
            TargetRoomId = targetRoomId;
            Scent = scent ?? throw new ArgumentNullException(nameof(scent));
            State = state;
            TestResult = testResult;
        }
    }
}
