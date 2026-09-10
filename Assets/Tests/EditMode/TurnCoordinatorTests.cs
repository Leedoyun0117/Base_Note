using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Complexes;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 턴 코디네이터는 턴을 세고, 매 턴 TurnAdvancedEvent를 내며, 그 라운드의
    // 목표 턴 수를 채우면 RoundSurvivedEvent를 딱 한 번 낸다. 라운드가 바뀌면
    // 카운터가 0으로 돌아가고 목표 턴 수를 새로 읽는다.
    public class TurnCoordinatorTests
    {
        private static EventBus Bus() => new EventBus(new NoOpEventExceptionHandler());

        private static RoomDefinition Round(string id, int turnsToSurvive) =>
            new RoomDefinition(new MemoryRoomId(id), Array.Empty<ClueDefinition>(), turnsToSurvive);

        private static IReadOnlyList<RoomDefinition> Rounds(params int[] turns)
        {
            var list = new List<RoomDefinition>();
            for (var i = 0; i < turns.Length; i++)
                list.Add(Round($"round-{i + 1}", turns[i]));
            return list;
        }

        [Test]
        public void 턴을_넘길_때마다_턴_번호가_실린_사건이_난다()
        {
            var bus = Bus();
            var seen = new List<TurnAdvancedEvent>();
            bus.Subscribe<TurnAdvancedEvent>(seen.Add);
            var coordinator = new TurnCoordinator(Rounds(3), bus);
            bus.Publish(new RoomStartedEvent(new MemoryRoomId("round-1"), 0));

            coordinator.AdvanceTurn();
            coordinator.AdvanceTurn();

            Assert.AreEqual(2, coordinator.CurrentTurn);
            Assert.AreEqual(new[] { 1, 2 }, new[] { seen[0].Turn, seen[1].Turn });
            Assert.AreEqual(3, seen[0].TurnsToSurvive);
        }

        [Test]
        public void 목표_턴_수를_채우면_클리어_사건이_난다()
        {
            var bus = Bus();
            var survived = 0;
            bus.Subscribe<RoundSurvivedEvent>(_ => survived++);
            var coordinator = new TurnCoordinator(Rounds(2), bus);
            bus.Publish(new RoomStartedEvent(new MemoryRoomId("round-1"), 0));

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
            var coordinator = new TurnCoordinator(Rounds(1), bus);
            bus.Publish(new RoomStartedEvent(new MemoryRoomId("round-1"), 0));

            coordinator.AdvanceTurn();
            coordinator.AdvanceTurn();
            coordinator.AdvanceTurn();

            Assert.AreEqual(1, survived);
        }

        [Test]
        public void 라운드가_바뀌면_카운터가_0으로_돌아가고_목표_턴_수를_새로_읽는다()
        {
            var bus = Bus();
            var coordinator = new TurnCoordinator(Rounds(2, 5), bus);
            bus.Publish(new RoomStartedEvent(new MemoryRoomId("round-1"), 0));
            coordinator.AdvanceTurn();
            coordinator.AdvanceTurn();
            Assert.IsTrue(coordinator.Survived);

            bus.Publish(new RoomStartedEvent(new MemoryRoomId("round-2"), 1));

            Assert.AreEqual(0, coordinator.CurrentTurn);
            Assert.IsFalse(coordinator.Survived);
            Assert.AreEqual(5, coordinator.TurnsToSurvive);
        }
    }
}
