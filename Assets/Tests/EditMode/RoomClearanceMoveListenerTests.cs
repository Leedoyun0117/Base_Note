using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Hiromi;
using GameName.Core.MemoryRooms;
using GameName.Core.Progression;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // "다음으로" 버튼(RoomClearedEvent)으로 넘어가는 것은 "다음 기억으로" 레버와
    // 같은 히로민 이동이다 — 넉넉하면 비용만 빠지고, 모자라면 기회로 대신
    // 치르며, 기회가 바닥나면 그 자리에서 런이 끝난다.
    public class RoomClearanceMoveListenerTests
    {
        private const int MoveCost = 15;

        private static RoomDefinition Room(string id) =>
            new RoomDefinition(
                new MemoryRoomId(id), Array.Empty<ClueDefinition>(), null,
                Array.Empty<DialogueLineDefinition>());

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly HiromiWallet Hiromi;
            public readonly ChanceTracker Chance;
            public readonly List<RoomStartedEvent> Started = new List<RoomStartedEvent>();
            public readonly List<RunCompletedEvent> Completed = new List<RunCompletedEvent>();

            public Fixture(int startingHiromi, int startingChance)
            {
                var rooms = new List<RoomDefinition> { Room("room-1"), Room("room-2"), Room("room-3") };
                Hiromi = new HiromiWallet(startingHiromi, Bus);
                Chance = new ChanceTracker(startingChance, Bus);

                var progressor = new RunProgressor(rooms, Bus);
                var move = new MemoryMoveProcessor(MoveCost, Hiromi, Chance, progressor);
                _ = new RoomClearanceMoveListener(move, Bus);
                _ = new ChanceExhaustionListener(Bus);

                Bus.Subscribe<RoomStartedEvent>(Started.Add);
                Bus.Subscribe<RunCompletedEvent>(Completed.Add);
                progressor.Start(); // room-1 진입 (Started[0])
            }
        }

        [Test]
        public void 히로민이_넉넉하면_이동_비용만_빠지고_다음_방으로_넘어간다()
        {
            var fx = new Fixture(startingHiromi: 30, startingChance: 2);

            fx.Bus.Publish(new RoomClearedEvent(new MemoryRoomId("room-1")));

            Assert.AreEqual(2, fx.Started.Count);
            Assert.AreEqual(new MemoryRoomId("room-2"), fx.Started[1].RoomId);
            Assert.AreEqual(15, fx.Hiromi.Remaining);
            Assert.AreEqual(2, fx.Chance.Remaining);
        }

        [Test]
        public void 히로민이_모자라면_가진_만큼_쓰고_기회로_대신_치른다()
        {
            var fx = new Fixture(startingHiromi: 5, startingChance: 2);

            fx.Bus.Publish(new RoomClearedEvent(new MemoryRoomId("room-1")));

            Assert.AreEqual(2, fx.Started.Count, "모자라도 이동 자체는 일어난다.");
            Assert.AreEqual(0, fx.Hiromi.Remaining);
            Assert.AreEqual(1, fx.Chance.Remaining);
            Assert.IsEmpty(fx.Completed);
        }

        [Test]
        public void 마지막_기회까지_쓰면_이동하지_않고_런이_끝난다()
        {
            var fx = new Fixture(startingHiromi: 0, startingChance: 1);

            fx.Bus.Publish(new RoomClearedEvent(new MemoryRoomId("room-1")));

            Assert.AreEqual(1, fx.Started.Count, "기회가 바닥나면 다음 방은 없다.");
            Assert.AreEqual(1, fx.Completed.Count);
        }
    }
}
