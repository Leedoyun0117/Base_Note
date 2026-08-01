using System;
using GameName.Core.Events;

namespace GameName.Core.Mentality
{
    // IMentalityGauge 기본 구현.
    //
    // Consume은 부족하면 아무것도 바꾸지 않고 실패한다(원자적 — 확인 후에만
    // 상태를 바꾸므로 부분 소모가 생길 수 없다). Restore는 최대값을 넘지 않도록
    // 클램프하고, 클램프로 인해 일부만 회복되어도 성공으로 취급한다.
    //
    // 소모/회복량이 음수인 경우는 호출자 버그로 보고 예외를 던진다 — Consume과
    // Restore는 이미 방향(빼기/더하기)이 메서드 이름에 고정되어 있으므로, 음수를
    // 받아들여 반대 방향으로 동작하게 두면 두 메서드의 책임 구분이 흐려진다.
    // 반대로 0은 "밸런싱으로 비용이 0으로 조정된 상태"를 나타낼 수 있는 유효한
    // 값이라 에러로 취급하지 않고 그대로 성공(무변화)으로 처리한다.
    //
    // 0이 되었을 때 무엇이 막히는지는 이 타입이 판단하지 않는다. CanAct 값만
    // 제공하고, 그 값을 어떻게 쓸지는 각 행동 시스템의 몫이다. 기억 방 복원에
    // 따른 자동 회복도 여기서 다루지 않는다 — 그건 MentalityChangedEvent가 아니라
    // MemoryRoomRestoredEvent를 구독하는 별도 어댑터의 책임이다.
    public sealed class MentalityGauge : IMentalityGauge
    {
        private readonly IEventBus _eventBus;
        private int _currentValue;

        public int MaxValue { get; }
        public int CurrentValue => _currentValue;
        public bool CanAct => _currentValue > 0;

        public MentalityGauge(IMentalityCostSettings settings, IEventBus eventBus)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            if (settings.InitialMentality > settings.MaxMentality)
                throw new ArgumentException("초기값은 최대값을 넘을 수 없다.", nameof(settings));

            MaxValue = settings.MaxMentality;
            _currentValue = settings.InitialMentality;
        }

        public bool Consume(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "소모량은 음수일 수 없다.");

            if (amount > _currentValue)
                return false;

            ChangeBy(-amount);
            return true;
        }

        public bool Restore(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "회복량은 음수일 수 없다.");

            var clampedAmount = Math.Min(amount, MaxValue - _currentValue);
            ChangeBy(clampedAmount);
            return true;
        }

        private void ChangeBy(int delta)
        {
            if (delta == 0)
                return;

            var previous = _currentValue;
            _currentValue += delta;
            _eventBus.Publish(new MentalityChangedEvent(previous, _currentValue));
        }
    }
}
