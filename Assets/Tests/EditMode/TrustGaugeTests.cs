using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.Core.Trust;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 신뢰도는 감소만 하고, 0에서 멈추며, 값이 실제로 바뀔 때만 사건을 낸다.
    public class TrustGaugeTests
    {
        private static EventBus Bus() => new EventBus(new NoOpEventExceptionHandler());

        private static List<TrustChangedEvent> Record(EventBus bus)
        {
            var seen = new List<TrustChangedEvent>();
            bus.Subscribe<TrustChangedEvent>(seen.Add);
            return seen;
        }

        [Test]
        public void 오답으로_깎이면_그만큼_내려가고_사건이_난다()
        {
            var bus = Bus();
            var seen = Record(bus);
            var gauge = new TrustGauge(3, bus);

            gauge.Decrease(1);

            Assert.AreEqual(2, gauge.Current);
            Assert.AreEqual(1, seen.Count);
            Assert.AreEqual(3, seen[0].Previous);
            Assert.AreEqual(2, seen[0].Current);
        }

        [Test]
        public void 신뢰도는_0_아래로는_내려가지_않는다()
        {
            var bus = Bus();
            var gauge = new TrustGauge(1, bus);

            gauge.Decrease(5);

            Assert.AreEqual(0, gauge.Current);
        }

        [Test]
        public void 이미_0이면_또_깎여도_사건이_없다()
        {
            var bus = Bus();
            var gauge = new TrustGauge(1, bus);
            gauge.Decrease(1);
            var seen = Record(bus);

            gauge.Decrease(1);

            Assert.AreEqual(0, gauge.Current);
            CollectionAssert.IsEmpty(seen);
        }

        [Test]
        public void 깎을_양이_0이하면_아무_일도_없다()
        {
            var bus = Bus();
            var seen = Record(bus);
            var gauge = new TrustGauge(3, bus);

            gauge.Decrease(0);
            gauge.Decrease(-2);

            Assert.AreEqual(3, gauge.Current);
            CollectionAssert.IsEmpty(seen);
        }

        [Test]
        public void 방이_시작되면_시작값으로_되돌아간다()
        {
            var bus = Bus();
            var gauge = new TrustGauge(3, bus);
            gauge.Decrease(3);
            var seen = Record(bus);

            bus.Publish(new RoomStartedEvent(new MemoryRoomId("room-2"), 1));

            Assert.AreEqual(3, gauge.Current);
            Assert.AreEqual(1, seen.Count);
            Assert.AreEqual(0, seen[0].Previous);
            Assert.AreEqual(3, seen[0].Current);
        }

        [Test]
        public void 신뢰도가_그대로인_방_시작은_사건을_내지_않는다()
        {
            var bus = Bus();
            var gauge = new TrustGauge(3, bus);
            var seen = Record(bus);

            bus.Publish(new RoomStartedEvent(new MemoryRoomId("room-1"), 0));

            CollectionAssert.IsEmpty(seen);
        }
    }
}
