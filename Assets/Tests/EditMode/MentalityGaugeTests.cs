using System;
using GameName.Core.Events;
using GameName.Core.Mentality;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class MentalityGaugeTests
    {
        private static IMentalityCostSettings MakeSettings(
            int initial = 100,
            int max = 100,
            int move = 1,
            int basic = 20,
            int advanced = 30,
            int ampoule = 8,
            int recovery = 20) =>
            new MentalityCostSettings(initial, max, move, basic, advanced, ampoule, recovery);

        private static EventBus MakeEventBus() => new EventBus(new NoOpEventExceptionHandler());

        [Test]
        public void 잔량이_부족하면_소모가_실패하고_값이_변하지_않는다()
        {
            var gauge = new MentalityGauge(MakeSettings(initial: 15), MakeEventBus());

            var succeeded = gauge.Consume(30);

            Assert.IsFalse(succeeded);
            Assert.AreEqual(15, gauge.CurrentValue);
        }

        [Test]
        public void 회복은_최대값을_넘지_않는다()
        {
            var gauge = new MentalityGauge(MakeSettings(initial: 90, max: 100), MakeEventBus());

            var succeeded = gauge.Restore(50);

            Assert.IsTrue(succeeded);
            Assert.AreEqual(100, gauge.CurrentValue);
        }

        [Test]
        public void 잔량이_0이_되면_CanAct가_false다()
        {
            var gauge = new MentalityGauge(MakeSettings(initial: 1), MakeEventBus());

            gauge.Consume(1);

            Assert.AreEqual(0, gauge.CurrentValue);
            Assert.IsFalse(gauge.CanAct);
        }

        [Test]
        public void 소모량이_0이면_변화_없이_성공하고_이벤트도_발행되지_않는다()
        {
            var eventBus = MakeEventBus();
            var receivedCount = 0;

            using (eventBus.Subscribe<MentalityChangedEvent>(e => receivedCount++))
            {
                var gauge = new MentalityGauge(MakeSettings(initial: 100), eventBus);

                var succeeded = gauge.Consume(0);

                Assert.IsTrue(succeeded);
                Assert.AreEqual(100, gauge.CurrentValue);
                Assert.AreEqual(0, receivedCount);
            }
        }

        [Test]
        public void 이미_최대값이면_회복해도_이벤트가_발행되지_않는다()
        {
            var eventBus = MakeEventBus();
            var receivedCount = 0;

            using (eventBus.Subscribe<MentalityChangedEvent>(e => receivedCount++))
            {
                var gauge = new MentalityGauge(MakeSettings(initial: 100, max: 100), eventBus);

                var succeeded = gauge.Restore(20);

                Assert.IsTrue(succeeded);
                Assert.AreEqual(0, receivedCount);
            }
        }

        [Test]
        public void 소모에_성공하면_이벤트가_한_번_발행되고_전후_값을_담는다()
        {
            var eventBus = MakeEventBus();
            MentalityChangedEvent? received = null;

            using (eventBus.Subscribe<MentalityChangedEvent>(e => received = e))
            {
                var gauge = new MentalityGauge(MakeSettings(initial: 100), eventBus);

                gauge.Consume(20);

                Assert.IsTrue(received.HasValue);
                Assert.AreEqual(100, received.Value.PreviousValue);
                Assert.AreEqual(80, received.Value.CurrentValue);
            }
        }

        [Test]
        public void 소모량이_음수면_예외를_던진다()
        {
            var gauge = new MentalityGauge(MakeSettings(), MakeEventBus());

            Assert.Throws<ArgumentOutOfRangeException>(() => gauge.Consume(-1));
        }

        [Test]
        public void 회복량이_음수면_예외를_던진다()
        {
            var gauge = new MentalityGauge(MakeSettings(), MakeEventBus());

            Assert.Throws<ArgumentOutOfRangeException>(() => gauge.Restore(-1));
        }

        [Test]
        public void 초기값이_최대값보다_크면_생성_시점에_예외를_던진다()
        {
            Assert.Throws<ArgumentException>(() =>
                new MentalityGauge(MakeSettings(initial: 200, max: 100), MakeEventBus()));
        }

        [Test]
        public void Reset은_소모_회복과_무관하게_처음_설정한_초기값으로_되돌린다()
        {
            var gauge = new MentalityGauge(MakeSettings(initial: 70, max: 100), MakeEventBus());
            gauge.Consume(50);
            gauge.Restore(30);

            gauge.Reset();

            Assert.AreEqual(70, gauge.CurrentValue);
        }

        [Test]
        public void 이미_초기값이면_Reset해도_이벤트가_발행되지_않는다()
        {
            var eventBus = MakeEventBus();
            var receivedCount = 0;

            using (eventBus.Subscribe<MentalityChangedEvent>(e => receivedCount++))
            {
                var gauge = new MentalityGauge(MakeSettings(initial: 100), eventBus);

                gauge.Reset();

                Assert.AreEqual(0, receivedCount);
            }
        }
    }
}
