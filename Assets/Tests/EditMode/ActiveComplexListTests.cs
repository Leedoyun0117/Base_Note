using System.Collections.Generic;
using GameName.Core.Complexes;
using GameName.Core.Events;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 활성 컴플렉스 목록은 우선순위 순서로 담고, 상한을 지키며, 매 턴 지속
    // 턴을 줄이다 0이 되면 스스로 걷어낸다.
    public class ActiveComplexListTests
    {
        private static EventBus Bus() => new EventBus(new NoOpEventExceptionHandler());

        private static ComplexDefinition Complex(string id, int priority, int duration) =>
            new ComplexDefinition(
                new ComplexId(id), priority, duration, ComplexKind.Transform,
                new List<TagTransformRule>());

        [Test]
        public void 우선순위_오름차순으로_담는다()
        {
            var list = new ActiveComplexList(maxConcurrent: 4, Bus());

            list.TryActivate(Complex("late", 10, 3));
            list.TryActivate(Complex("early", 1, 3));
            list.TryActivate(Complex("mid", 5, 3));

            var order = list.DefinitionsInPriorityOrder;
            Assert.AreEqual(new[] { "early", "mid", "late" },
                new[] { order[0].Id.Value, order[1].Id.Value, order[2].Id.Value });
        }

        [Test]
        public void 상한이_차면_새_활성화는_거절된다()
        {
            var list = new ActiveComplexList(maxConcurrent: 2, Bus());

            Assert.IsTrue(list.TryActivate(Complex("a", 0, 3)));
            Assert.IsTrue(list.TryActivate(Complex("b", 1, 3)));
            Assert.IsFalse(list.TryActivate(Complex("c", 2, 3)));
            Assert.AreEqual(2, list.Count);
        }

        [Test]
        public void 같은_id가_이미_활성이면_거절한다()
        {
            var list = new ActiveComplexList(4, Bus());

            Assert.IsTrue(list.TryActivate(Complex("a", 0, 3)));
            Assert.IsFalse(list.TryActivate(Complex("a", 0, 5)));
        }

        [Test]
        public void 활성화하면_지속_턴이_실린_사건이_난다()
        {
            var bus = Bus();
            var seen = new List<ComplexActivatedEvent>();
            bus.Subscribe<ComplexActivatedEvent>(seen.Add);
            var list = new ActiveComplexList(4, bus);

            list.TryActivate(Complex("a", 0, 4));

            Assert.AreEqual(1, seen.Count);
            Assert.AreEqual("a", seen[0].ComplexId.Value);
            Assert.AreEqual(4, seen[0].RemainingTurns);
        }

        [Test]
        public void 매_턴_지속_턴이_줄고_0이_되면_소멸_사건과_함께_빠진다()
        {
            var bus = Bus();
            var expired = new List<ComplexExpiredEvent>();
            bus.Subscribe<ComplexExpiredEvent>(expired.Add);
            var list = new ActiveComplexList(4, bus);
            var coordinator = new TurnCoordinator(turnsToSurvive: 10, bus);

            list.TryActivate(Complex("a", 0, 2));

            coordinator.AdvanceTurn();
            Assert.AreEqual(0, expired.Count);
            Assert.AreEqual(1, list.InPriorityOrder[0].RemainingTurns);

            coordinator.AdvanceTurn();
            Assert.AreEqual(1, expired.Count);
            Assert.AreEqual("a", expired[0].ComplexId.Value);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void 리셋은_담긴_것을_전부_소멸시킨다()
        {
            var bus = Bus();
            var expired = new List<ComplexExpiredEvent>();
            bus.Subscribe<ComplexExpiredEvent>(expired.Add);
            var list = new ActiveComplexList(4, bus);

            list.TryActivate(Complex("a", 0, 3));
            list.TryActivate(Complex("b", 1, 3));
            list.Reset();

            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(2, expired.Count);
        }
    }
}
