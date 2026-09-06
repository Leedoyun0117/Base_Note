using System;
using GameName.Core.Events;

namespace GameName.Core.Hiromi
{
    // IHiromiMutator 기본 구현. 런 전체에 걸쳐 쌓이고 줄어드는 단일 자원이다.
    //
    // 방이 바뀌어도 리셋되지 않는다 — 옛 IExtractionBudget과 같은 스코프이자
    // 그 후신이다. 대화 한 번에 +3, 추출 한 번에 -9, 다음 기억으로 이동에
    // -15가 이 자원 하나를 오간다 — 세 행동이 서로 다른 자원을 쓰던 것을
    // 하나로 합친 것이 이 개편의 핵심이다(같은 자원이 공존하면 안 된다는
    // 요구가 여기서 나온다).
    public sealed class HiromiWallet : IHiromiMutator
    {
        private readonly IEventBus _eventBus;

        public int Remaining { get; private set; }

        public HiromiWallet(int initial, IEventBus eventBus)
        {
            if (initial < 0)
                throw new ArgumentOutOfRangeException(nameof(initial), initial, "히로민은 음수로 시작할 수 없다.");

            Remaining = initial;
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public void Earn(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "벌어들일 히로민은 음수일 수 없다.");
            if (amount == 0)
                return;

            SetTo(Remaining + amount);
        }

        public void Spend(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "쓸 히로민은 음수일 수 없다.");
            if (amount > Remaining)
                throw new InvalidOperationException("가진 히로민보다 많이 쓸 수 없다 — 호출자가 먼저 확인했어야 한다.");
            if (amount == 0)
                return;

            SetTo(Remaining - amount);
        }

        private void SetTo(int next)
        {
            var previous = Remaining;
            Remaining = next;
            _eventBus.Publish(new HiromiChangedEvent(previous, Remaining));
        }
    }
}
