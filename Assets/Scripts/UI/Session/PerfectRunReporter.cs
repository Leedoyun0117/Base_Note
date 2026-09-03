using System;
using GameName.Core.Events;

namespace GameName.UI.Session
{
    // 한 판을 "모든 최적 선택"으로 완주했는지 콘솔에 알리는 테스트용 관찰자.
    //
    // 최적이 아닌 것: 텍스트 선택지 오답(TrustChangedEvent의 감소), ClueSelection
    // 오답 또는 넘어가기(ClueAnsweredEvent.WasCorrect == false). 둘 중 하나라도
    // 있었으면 흠집으로 본다.
    //
    // 게임 규칙이 아니라 진단이라 Core에 두지 않는다 — RunCompletedEvent 시점에
    // 흠집이 없었으면 "클리어"를 찍기만 한다.
    internal sealed class PerfectRunReporter : IDisposable
    {
        private readonly IDisposable[] _subscriptions;
        private bool _flawless = true;

        public PerfectRunReporter(IEventBus eventBus)
        {
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _subscriptions = new[]
            {
                eventBus.Subscribe<TrustChangedEvent>(e =>
                {
                    if (e.Current < e.Previous)
                        _flawless = false;
                }),
                eventBus.Subscribe<ClueAnsweredEvent>(e =>
                {
                    if (!e.WasCorrect)
                        _flawless = false;
                }),
                eventBus.Subscribe<RunCompletedEvent>(_ => Report()),
            };
        }

        private void Report()
        {
            UnityEngine.Debug.Log(_flawless
                ? "[TEST] 클리어 — 모든 최적 선택으로 완주했습니다."
                : "[TEST] 완주 — 최적 선택은 아니었습니다.");
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }
    }
}
