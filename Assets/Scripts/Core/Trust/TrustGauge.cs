using System;
using GameName.Core.Events;

namespace GameName.Core.Trust
{
    // ITrustGauge 기본 구현.
    //
    // 방마다 정해진 값에서 시작해 오답으로만 깎이고, 0에 닿으면 더 내려가지
    // 않는다. 값이 실제로 바뀐 발행만 의미가 있으므로(이미 0인데 또 깎였다는
    // 것은 화면이 다시 그릴 이유가 없다) 변화가 없을 때는 이벤트를 내지 않는다.
    //
    // RoomStartedEvent를 구독해 스스로 시작값으로 되돌린다. RunProgressor가
    // 게이지를 붙잡고 리셋해 주는 대신 게이지가 자기 수명을 아는 쪽을 택한 것은,
    // 방마다 초기화되어야 하는 것이 늘 때 RunProgressor로 책임이 모이는 것을
    // 막기 위해서다. 되돌리는 것도 "값이 바뀌면 발행" 규칙을 그대로 따른다 —
    // 0에서 시작값으로 올라가는 것은 화면이 방을 다시 넓게 그려야 하는 변화다.
    public sealed class TrustGauge : ITrustGauge
    {
        private readonly int _startingTrust;
        private readonly IEventBus _eventBus;

        public int Current { get; private set; }

        public TrustGauge(int startingTrust, IEventBus eventBus)
        {
            if (startingTrust < 0)
                throw new ArgumentOutOfRangeException(nameof(startingTrust), startingTrust, "시작 신뢰도는 음수일 수 없다.");

            _startingTrust = startingTrust;
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            Current = startingTrust;

            _eventBus.Subscribe<RoomStartedEvent>(_ => SetTo(_startingTrust));
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
