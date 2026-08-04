using GameName.Core.Commissions;

namespace GameName.Core.Events
{
    // 의뢰가 성공적으로 완료(제공)될 때 결과와 함께 발행된다.
    //
    // CommissionCompletionProcessor.Complete()는 이 이벤트를 반드시
    // CommissionSession.TryComplete()보다 먼저 발행한다 — TryComplete()가
    // 발행하는 CommissionStageChangedEvent를 SceneScreenSwitcher가 구독해
    // 완료 화면으로 즉시 전환하므로, 그 전환이 일어나기 전에 결과를 먼저
    // 저장해 둬야 완료 화면이 첫 렌더링부터 올바른 결과를 보여줄 수 있다.
    // (화면 전환 자체가 같은 스레드에서 동기적으로 일어나므로, 이 이벤트를
    // 나중에 발행하면 화면이 이미 빈 결과로 한 번 그려진 뒤라 소용없다.)
    public readonly struct CommissionCompletedEvent
    {
        public CommissionCompletionResult Result { get; }

        public CommissionCompletedEvent(CommissionCompletionResult result)
        {
            Result = result;
        }
    }
}
