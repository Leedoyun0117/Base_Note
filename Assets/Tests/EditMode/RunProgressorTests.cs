using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.Core.Progression;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 라운드를 버텨 내면(RoundSurvivedEvent) 다음 라운드로 잇고, 마지막 라운드를
    // 버텨 내면 런이 끝난다.
    public class RunProgressorTests
    {
        private static RoomDefinition Round(string id) =>
            new RoomDefinition(new MemoryRoomId(id), Array.Empty<ClueDefinition>(), turnsToSurvive: 3);

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly RunProgressor Progressor;
            public readonly List<RoomStartedEvent> Started = new List<RoomStartedEvent>();
            public readonly List<RunCompletedEvent> Completed = new List<RunCompletedEvent>();

            public Fixture(int roundCount)
            {
                var rounds = new List<RoomDefinition>();
                for (var i = 1; i <= roundCount; i++)
                    rounds.Add(Round($"round-{i}"));

                Progressor = new RunProgressor(rounds, Bus);
                Bus.Subscribe<RoomStartedEvent>(Started.Add);
                Bus.Subscribe<RunCompletedEvent>(Completed.Add);
            }
        }

        [Test]
        public void 시작하면_첫_라운드로_들어간다()
        {
            var fx = new Fixture(3);

            fx.Progressor.Start();

            Assert.AreEqual(1, fx.Started.Count);
            Assert.AreEqual(new MemoryRoomId("round-1"), fx.Started[0].RoomId);
            Assert.AreEqual(0, fx.Started[0].RoomIndex);
        }

        [Test]
        public void 라운드를_버텨_내면_다음_라운드로_넘어간다()
        {
            var fx = new Fixture(3);
            fx.Progressor.Start();

            fx.Bus.Publish(new RoundSurvivedEvent());

            Assert.AreEqual(2, fx.Started.Count);
            Assert.AreEqual(new MemoryRoomId("round-2"), fx.Started[1].RoomId);
            Assert.AreEqual(1, fx.Started[1].RoomIndex);
        }

        [Test]
        public void 마지막_라운드를_버텨_내면_런이_끝나고_더_이상_라운드가_시작되지_않는다()
        {
            var fx = new Fixture(3);
            fx.Progressor.Start();

            fx.Bus.Publish(new RoundSurvivedEvent());
            fx.Bus.Publish(new RoundSurvivedEvent());
            fx.Bus.Publish(new RoundSurvivedEvent());

            Assert.AreEqual(3, fx.Started.Count);
            Assert.AreEqual(1, fx.Completed.Count);
        }

        [Test]
        public void EndRun하면_남은_라운드가_있어도_런이_끝난다()
        {
            var fx = new Fixture(3);
            fx.Progressor.Start();

            fx.Progressor.EndRun();

            Assert.AreEqual(1, fx.Started.Count);
            Assert.AreEqual(1, fx.Completed.Count);
        }

        [Test]
        public void 런은_한_번만_시작할_수_있다()
        {
            var fx = new Fixture(2);
            fx.Progressor.Start();

            Assert.Throws<InvalidOperationException>(() => fx.Progressor.Start());
        }
    }
}
