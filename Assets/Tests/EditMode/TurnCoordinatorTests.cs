using System;
using System.Collections.Generic;
using GameName.Core.Complexes;
using GameName.Core.Events;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 턴 코디네이터는 턴을 세고, 매 턴 TurnAdvancedEvent를 내며, 버텨야 하는
    // 턴 수를 채우면 RoundSurvivedEvent를 딱 한 번 낸다.
    public class TurnCoordinatorTests
    {
        private static EventBus Bus() => new EventBus(new NoOpEventExceptionHandler());

        [Test]
        public void 턴을_넘길_때마다_턴_번호가_실린_사건이_난다()
        {
            var bus = Bus();
            var seen = new List<TurnAdvancedEvent>();
            bus.Subscribe<TurnAdvancedEvent>(seen.Add);
            var coordinator = new TurnCoordinator(turnsToSurvive: 3, bus);

            coordinator.AdvanceTurn();
            coordinator.AdvanceTurn();

            Assert.AreEqual(2, coordinator.CurrentTurn);
            Assert.AreEqual(new[] { 1, 2 }, new[] { seen[0].Turn, seen[1].Turn });
            Assert.AreEqual(3, seen[0].TurnsToSurvive);
        }

        [Test]
        public void 버텨야_하는_턴_수를_채우면_클리어_사건이_난다()
        {
            var bus = Bus();
            var survived = 0;
            bus.Subscribe<RoundSurvivedEvent>(_ => survived++);
            var coordinator = new TurnCoordinator(turnsToSurvive: 2, bus);

            coordinator.AdvanceTurn();
            Assert.AreEqual(0, survived);
            Assert.IsFalse(coordinator.Survived);

            coordinator.AdvanceTurn();
            Assert.AreEqual(1, survived);
            Assert.IsTrue(coordinator.Survived);
        }

        [Test]
        public void 문턱을_넘은_뒤_더_진행해도_클리어_사건은_한_번뿐이다()
        {
            var bus = Bus();
            var survived = 0;
            bus.Subscribe<RoundSurvivedEvent>(_ => survived++);
            var coordinator = new TurnCoordinator(turnsToSurvive: 1, bus);

            coordinator.AdvanceTurn();
            coordinator.AdvanceTurn();
            coordinator.AdvanceTurn();

            Assert.AreEqual(1, survived);
        }

        [Test]
        public void 버텨야_하는_턴_수는_1_이상이어야_한다()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TurnCoordinator(0, Bus()));
        }
    }
}
