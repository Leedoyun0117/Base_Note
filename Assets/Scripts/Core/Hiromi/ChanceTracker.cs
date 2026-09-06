using System;
using GameName.Core.Events;

namespace GameName.Core.Hiromi
{
    // IChanceTracker 기본 구현. 런당 정해진 수에서 시작해 한 방향으로만 줄어들고
    // 0 밑으로는 내려가지 않는다.
    //
    // 0에 닿았을 때 런을 끝내는 판정은 여기 없다 — ChanceChangedEvent만 내고,
    // "0이면 런 종료"는 별도 리스너(ChanceExhaustionListener)의 몫이다.
    // TrustGauge가 신뢰 0을 스스로 판정하지 않고 RoomCompletionArbiter에
    // 맡기는 것과 같은 이유다: 셈과 그 셈이 부르는 결과를 한 타입에 묶지 않는다.
    //
    // 방이 바뀌어도 리셋되지 않는다 — 런 전체에 걸쳐 단 한 번만 소진된다.
    public sealed class ChanceTracker : IChanceTracker
    {
        private readonly IEventBus _eventBus;

        public int Remaining { get; private set; }

        public ChanceTracker(int initial, IEventBus eventBus)
        {
            if (initial < 0)
                throw new ArgumentOutOfRangeException(nameof(initial), initial, "기회는 음수로 시작할 수 없다.");

            Remaining = initial;
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public void Decrease()
        {
            if (Remaining <= 0)
                return;

            Remaining--;
            _eventBus.Publish(new ChanceChangedEvent(Remaining));
        }
    }
}
