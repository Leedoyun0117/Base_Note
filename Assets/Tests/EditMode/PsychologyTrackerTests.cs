using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.Mind;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 심리 상태는 시작값을 그대로 들고, 실제로 바뀔 때만 사건을 낸다.
    public class PsychologyTrackerTests
    {
        private static EventBus Bus() => new EventBus(new NoOpEventExceptionHandler());

        private static List<PsychologyChangedEvent> Record(EventBus bus)
        {
            var seen = new List<PsychologyChangedEvent>();
            bus.Subscribe<PsychologyChangedEvent>(seen.Add);
            return seen;
        }

        [Test]
        public void 시작_상태를_그대로_읽는다()
        {
            var tracker = new PsychologyTracker(PsychologyState.Melancholy, Bus());

            Assert.AreEqual(PsychologyState.Melancholy, tracker.Current);
        }

        [Test]
        public void 상태를_바꾸면_이전과_현재를_실어_사건이_난다()
        {
            var bus = Bus();
            var seen = Record(bus);
            var tracker = new PsychologyTracker(PsychologyState.Optimism, bus);

            tracker.SetState(PsychologyState.Mania);

            Assert.AreEqual(PsychologyState.Mania, tracker.Current);
            Assert.AreEqual(1, seen.Count);
            Assert.AreEqual(PsychologyState.Optimism, seen[0].Previous);
            Assert.AreEqual(PsychologyState.Mania, seen[0].Current);
        }

        [Test]
        public void 같은_상태로_다시_설정하면_사건이_없다()
        {
            var bus = Bus();
            var tracker = new PsychologyTracker(PsychologyState.Optimism, bus);
            var seen = Record(bus);

            tracker.SetState(PsychologyState.Optimism);

            Assert.AreEqual(PsychologyState.Optimism, tracker.Current);
            CollectionAssert.IsEmpty(seen);
        }
    }
}
