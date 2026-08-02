using GameName.Core.Commissions;

namespace GameName.Core.Events
{
    // 의뢰 단계가 바뀔 때마다 발행된다. 화면 전환기가 이 이벤트(와 이동 완료
    // 이벤트)를 구독해 지금 보여줄 화면을 고른다.
    public readonly struct CommissionStageChangedEvent
    {
        public CommissionStage PreviousStage { get; }
        public CommissionStage NewStage { get; }

        public CommissionStageChangedEvent(CommissionStage previousStage, CommissionStage newStage)
        {
            PreviousStage = previousStage;
            NewStage = newStage;
        }
    }
}
