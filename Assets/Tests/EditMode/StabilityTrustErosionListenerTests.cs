using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.Mind;
using GameName.Core.Trust;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 안정 축이 자유 폭 안이면 답변해도 신뢰가 안 깎이고, 벗어나면 벗어난
    // 만큼 round((|위치| - 자유폭) / 제수)씩 깎인다. 답이 정답인지 오답인지는
    // 보지 않는다.
    public class StabilityTrustErosionListenerTests
    {
        private const int FreeBand = 20;
        private const int Divisor = 10;

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly StabilityAxis Stability;
            public readonly TrustGauge Trust;
            public readonly List<TrustChangedEvent> TrustChanges = new List<TrustChangedEvent>();

            // ReSharper disable once NotAccessedField.Local — 구독을 살려 두기 위한 보관.
            private readonly StabilityTrustErosionListener _listener;

            public Fixture(int startStability, int startTrust = 20)
            {
                Stability = new StabilityAxis(startStability, -100, 100, Bus);
                Trust = new TrustGauge(startTrust, Bus);
                _listener = new StabilityTrustErosionListener(Stability, Trust, FreeBand, Divisor, Bus);
                Bus.Subscribe<TrustChangedEvent>(TrustChanges.Add);
            }

            public void Answer(bool correct) => Bus.Publish(new ClueAnsweredEvent(correct));
        }

        [Test]
        public void 안정_축이_자유_폭_이내면_답변해도_신뢰가_안_깎인다()
        {
            var fx = new Fixture(startStability: 20);

            fx.Answer(correct: false);
            fx.Answer(correct: true);

            Assert.AreEqual(20, fx.Trust.Current);
            CollectionAssert.IsEmpty(fx.TrustChanges);
        }

        [Test]
        public void 침체_쪽으로_벗어나면_벗어난_만큼_깎인다()
        {
            // |−45| = 45, (45 − 20) = 25, round(25 / 10) = 3.
            var fx = new Fixture(startStability: -45);

            fx.Answer(correct: true);

            Assert.AreEqual(17, fx.Trust.Current);
        }

        [Test]
        public void 흥분_쪽으로_벗어나도_똑같이_깎인다()
        {
            // |+45| = 45 → 위치의 부호와 무관하게 3 깎인다.
            var fx = new Fixture(startStability: 45);

            fx.Answer(correct: true);

            Assert.AreEqual(17, fx.Trust.Current);
        }

        [Test]
        public void 반올림_반은_올림이다()
        {
            // (35 − 20) = 15, round(15 / 10) = 2 (1.5 → 올림).
            var fx = new Fixture(startStability: -35);

            fx.Answer(correct: false);

            Assert.AreEqual(18, fx.Trust.Current);
        }

        [Test]
        public void 자유_폭을_갓_넘긴_정도로는_아직_안_깎인다()
        {
            // (24 − 20) = 4, round(4 / 10) = 0.
            var fx = new Fixture(startStability: -24);

            fx.Answer(correct: false);

            Assert.AreEqual(20, fx.Trust.Current);
            CollectionAssert.IsEmpty(fx.TrustChanges);
        }

        [Test]
        public void 매_답변마다_깎여_결국_0에_닿는다()
        {
            // |−70| = 70, (70 − 20) = 50, round(50 / 10) = 5. 20 → 15 → 10 → 5 → 0.
            var fx = new Fixture(startStability: -70, startTrust: 20);

            for (var i = 0; i < 4; i++)
                fx.Answer(correct: true);

            Assert.AreEqual(0, fx.Trust.Current);
        }

        [Test]
        public void 답변_전_안정_축이_움직이면_그_새_위치로_깎는다()
        {
            var fx = new Fixture(startStability: 0, startTrust: 20);

            fx.Answer(correct: false); // 자유 폭 안 → 변화 없음
            Assert.AreEqual(20, fx.Trust.Current);

            fx.Stability.Shift(-60); // −60로 이동
            fx.Answer(correct: false); // (60 − 20) = 40, round(40/10) = 4

            Assert.AreEqual(16, fx.Trust.Current);
        }
    }
}
