using GameName.Core.Ampoules;
using GameName.Core.Emotions;
using GameName.Core.Judging;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Events
{
    // 플레이어가 기억 방에서 향을 시험(시향)할 때마다 발행된다.
    // AmpouleId를 담는 이유: 소비한 앰플이 어느 것이었는지 알아야, 기록지가
    // 이 결과를 해당 앰플의 제작 기록(AmpouleRecipe.Id)과 정확히 연결할 수
    // 있다 — 같은 방·같은 배합으로 여러 개를 만들었을 수 있어 방/배합만으로는
    // 어느 개체를 시향했는지 구분할 수 없다.
    public readonly struct ScentJudgedEvent
    {
        public AmpouleId AmpouleId { get; }
        public MemoryRoomId RoomId { get; }
        public Scent TestedScent { get; }
        public ScentJudgementResult Result { get; }

        public ScentJudgedEvent(AmpouleId ampouleId, MemoryRoomId roomId, Scent testedScent, ScentJudgementResult result)
        {
            AmpouleId = ampouleId;
            RoomId = roomId;
            TestedScent = testedScent;
            Result = result;
        }
    }
}
