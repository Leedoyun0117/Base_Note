using System;
using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.Mind;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 안정 축은 양방향으로 움직이고, 하한·상한에서 멈추며, 값이 실제로 바뀔
    // 때만 사건을 낸다.
    public class StabilityAxisTests
    {
        private static EventBus Bus() => new EventBus(new NoOpEventExceptionHandler());

        private static List<StabilityChangedEvent> Record(EventBus bus)
        {
            var seen = new List<StabilityChangedEvent>();
            bus.Subscribe<StabilityChangedEvent>(seen.Add);
            return seen;
        }

        [Test]
        public void 흥분_쪽으로_밀면_그만큼_올라가고_사건이_난다()
        {
            var bus = Bus();
            var seen = Record(bus);
            var axis = new StabilityAxis(0, -100, 100, bus);

            axis.Shift(15);

            Assert.AreEqual(15, axis.Position);
            Assert.AreEqual(1, seen.Count);
            Assert.AreEqual(0, seen[0].Previous);
            Assert.AreEqual(15, seen[0].Current);
        }

        [Test]
        public void 침체_쪽으로_밀면_음수로_내려간다()
        {
            var bus = Bus();
            var axis = new StabilityAxis(0, -100, 100, bus);

            axis.Shift(-30);

            Assert.AreEqual(-30, axis.Position);
        }

        [Test]
        public void 상한을_넘겨_밀면_상한에서_멈춘다()
        {
            var bus = Bus();
            var seen = Record(bus);
            var axis = new StabilityAxis(90, -100, 100, bus);

            axis.Shift(50);

            Assert.AreEqual(100, axis.Position);
            Assert.AreEqual(100, seen[0].Current);
        }

        [Test]
        public void 하한을_넘겨_밀면_하한에서_멈춘다()
        {
            var bus = Bus();
            var axis = new StabilityAxis(-90, -100, 100, bus);

            axis.Shift(-50);

            Assert.AreEqual(-100, axis.Position);
        }

        [Test]
        public void 이미_끝에_있으면_같은_방향으로_더_밀어도_사건이_없다()
        {
            var bus = Bus();
            var axis = new StabilityAxis(100, -100, 100, bus);
            var seen = Record(bus);

            axis.Shift(10);

            Assert.AreEqual(100, axis.Position);
            CollectionAssert.IsEmpty(seen);
        }

        [Test]
        public void 델타가_0이면_아무_일도_없다()
        {
            var bus = Bus();
            var seen = Record(bus);
            var axis = new StabilityAxis(20, -100, 100, bus);

            axis.Shift(0);

            Assert.AreEqual(20, axis.Position);
            CollectionAssert.IsEmpty(seen);
        }

        [Test]
        public void 시작값이_범위를_벗어나면_생성에서_막는다()
        {
            var bus = Bus();

            Assert.Throws<ArgumentOutOfRangeException>(() => new StabilityAxis(150, -100, 100, bus));
        }

        [Test]
        public void 하한이_상한보다_크면_생성에서_막는다()
        {
            var bus = Bus();

            Assert.Throws<ArgumentException>(() => new StabilityAxis(0, 100, -100, bus));
        }
    }
}
