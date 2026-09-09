using System;
using GameName.Core.Events;

namespace GameName.Core.Trust
{
    // ITrustGauge 기본 구현. 유키의 인내심을 나타내는 런 전체 게이지다.
    //
    // 정해진 값에서 시작해 오답으로만 깎이던 것에서 바뀌었다: 이제 깎는 것은
    // 오답이 아니라 나츠의 안정 축 이탈이다. 안정 축이 자유 폭 안에 있으면
    // 인내심은 줄지 않고, 그 밖으로 벗어나 있으면 매 답변마다 조금씩 깎인다.
    // 얼마를 깎을지는 StabilityTrustErosionListener가 정하고, 이 게이지는 0 하한
    // 처리와 변화 이벤트 발행만 책임진다.
    //
    // 방마다 리셋되지 않는다 — 인내심은 런 전체에 걸쳐 이어지는 하나의 값이고,
    // 0에 닿으면 그 자리에서 런이 끝난다(RoomCompletionArbiter → RunProgressor).
    // 예전엔 방마다 시작값으로 되돌아갔지만, 인내심이 방을 넘어 누적되는 지금은
    // RoomStartedEvent를 구독하지 않는다.
    //
    // 값이 실제로 바뀐 발행만 의미가 있으므로(이미 0인데 또 깎였다는 것은 화면이
    // 다시 그릴 이유가 없다) 변화가 없을 때는 이벤트를 내지 않는다.
    public sealed class TrustGauge : ITrustGauge
    {
        private readonly IEventBus _eventBus;

        public int Current { get; private set; }

        public TrustGauge(int startingTrust, IEventBus eventBus)
        {
            if (startingTrust < 0)
                throw new ArgumentOutOfRangeException(nameof(startingTrust), startingTrust, "시작 신뢰도는 음수일 수 없다.");

            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            Current = startingTrust;
        }

        public void Decrease(int amount)
        {
            if (amount <= 0)
                return;

            SetTo(Math.Max(0, Current - amount));
        }

        private void SetTo(int next)
        {
            if (next == Current)
                return;

            var previous = Current;
            Current = next;
            _eventBus.Publish(new TrustChangedEvent(previous, Current));
        }
    }
}
