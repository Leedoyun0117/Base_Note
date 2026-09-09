using System;
using GameName.Core.Events;

namespace GameName.Core.Mind
{
    // IPsychologyMutator 기본 구현. 지금 심리 상태 하나를 든다.
    //
    // 런 전체에 걸쳐 이어진다 — 방이 바뀌어도 리셋되지 않는다(RoomStartedEvent를
    // 구독하지 않는다). 심리적 맥락은 방을 넘어 누적되는 것이라 안정 축·히로민과
    // 같은 스코프다.
    //
    // 상태가 실제로 바뀔 때만 PsychologyChangedEvent를 낸다 — 같은 상태로 다시
    // SetState 해도 화면이 다시 그릴 이유가 없다(TrustGauge가 값이 바뀐 발행만
    // 내는 것과 같다).
    public sealed class PsychologyTracker : IPsychologyMutator
    {
        private readonly IEventBus _eventBus;

        public PsychologyState Current { get; private set; }

        public PsychologyTracker(PsychologyState initial, IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            Current = initial;
        }

        public void SetState(PsychologyState state)
        {
            if (state == Current)
                return;

            var previous = Current;
            Current = state;
            _eventBus.Publish(new PsychologyChangedEvent(previous, Current));
        }
    }
}
