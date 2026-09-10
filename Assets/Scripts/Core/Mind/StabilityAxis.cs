using System;
using GameName.Core.Events;

namespace GameName.Core.Mind
{
    // IStabilityAxis 기본 구현. 침체(하한) ~ 안정(0) ~ 흥분(상한) 사이의 한
    // 위치를 든다.
    //
    // 하한·상한을 상수로 박지 않고 데이터로 받는 이유는 이 값들이 밸런싱
    // 대상이기 때문이다(-100..+100은 지금의 저작값일 뿐이다). 축은 그 범위로
    // 자르는 규칙과, 값이 실제로 바뀌었을 때 StabilityChangedEvent를 내는 것만
    // 안다 — "침체가 얼마나 깊으면 신뢰가 깎이는가" 같은 판단은 축 밖의 몫이다.
    //
    // 런 전체에 걸쳐 이어진다 — 방이 바뀌어도 리셋되지 않는다. 피드백으로 쌓인
    // 심리적 맥락이 다음 방으로 넘어가야 하기 때문이다.
    public sealed class StabilityAxis : IStabilityAxis
    {
        private readonly int _min;
        private readonly int _max;
        private readonly IEventBus _eventBus;

        public int Position { get; private set; }
        public int Min => _min;
        public int Max => _max;

        public StabilityAxis(int initialPosition, int min, int max, IEventBus eventBus)
        {
            if (min > max)
                throw new ArgumentException("안정 축의 하한이 상한보다 클 수 없다.", nameof(min));
            if (initialPosition < min || initialPosition > max)
                throw new ArgumentOutOfRangeException(
                    nameof(initialPosition), initialPosition, "시작 위치가 축 범위를 벗어난다.");

            _min = min;
            _max = max;
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            Position = initialPosition;
        }

        public void Shift(int delta)
        {
            if (delta == 0)
                return;

            var next = Position + delta;
            if (next < _min)
                next = _min;
            else if (next > _max)
                next = _max;

            if (next == Position)
                return;

            var previous = Position;
            Position = next;
            _eventBus.Publish(new StabilityChangedEvent(previous, Position));
        }
    }
}
