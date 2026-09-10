using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Complexes;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.Core.Mind;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 발생 리스너는 매 턴 확률을 굴려, 걸리면 뽑기 소스에서 하나 꺼내 활성
    // 목록에 넣는다. 확률 0이면 절대, 1이면 매 턴(목록에 자리가 있는 한) 넣는다.
    public class ComplexSpawnListenerTests
    {
        private static EventBus Bus() => new EventBus(new NoOpEventExceptionHandler());

        private static TurnCoordinator Turns(EventBus bus)
        {
            var rounds = new List<RoomDefinition>
            {
                new RoomDefinition(new MemoryRoomId("round-1"), Array.Empty<ClueDefinition>(), turnsToSurvive: 99),
            };
            var coordinator = new TurnCoordinator(rounds, bus);
            bus.Publish(new RoomStartedEvent(new MemoryRoomId("round-1"), 0));
            return coordinator;
        }

        private sealed class FixedPolicy : IComplexSpawnPolicy
        {
            private readonly float _chance;
            public FixedPolicy(float chance) => _chance = chance;
            public float SpawnChance(int stabilityPosition) => _chance;
        }

        private sealed class QueueDrawSource : IComplexDrawSource
        {
            private readonly Queue<ComplexDefinition> _queue;
            public QueueDrawSource(params ComplexDefinition[] items) =>
                _queue = new Queue<ComplexDefinition>(items);

            public bool TryDraw(int turn, out ComplexDefinition definition)
            {
                if (_queue.Count == 0)
                {
                    definition = null;
                    return false;
                }

                definition = _queue.Dequeue();
                return true;
            }
        }

        private static ComplexDefinition Complex(string id) =>
            new ComplexDefinition(
                new ComplexId(id), priority: 0, durationTurns: 3, ComplexKind.Transform,
                new List<TagTransformRule>());

        [Test]
        public void 확률이_0이면_아무것도_넣지_않는다()
        {
            var bus = Bus();
            var stability = new StabilityAxis(0, -100, 100, bus);
            var active = new ActiveComplexList(4, bus);
            _ = new ComplexSpawnListener(
                new FixedPolicy(0f), stability, active, new QueueDrawSource(Complex("a")), seed: 1, bus);
            var coordinator = Turns(bus);

            for (var i = 0; i < 10; i++)
                coordinator.AdvanceTurn();

            Assert.AreEqual(0, active.Count);
        }

        [Test]
        public void 확률이_1이면_매_턴_하나씩_뽑아_넣는다()
        {
            var bus = Bus();
            var stability = new StabilityAxis(0, -100, 100, bus);
            var active = new ActiveComplexList(4, bus);
            _ = new ComplexSpawnListener(
                new FixedPolicy(1f), stability, active,
                new QueueDrawSource(Complex("a"), Complex("b")), seed: 1, bus);
            var coordinator = Turns(bus);

            coordinator.AdvanceTurn();
            Assert.AreEqual(1, active.Count);

            coordinator.AdvanceTurn();
            Assert.AreEqual(2, active.Count);
        }

        [Test]
        public void 뽑을_것이_없으면_조용히_넘어간다()
        {
            var bus = Bus();
            var stability = new StabilityAxis(0, -100, 100, bus);
            var active = new ActiveComplexList(4, bus);
            _ = new ComplexSpawnListener(
                new FixedPolicy(1f), stability, active, new QueueDrawSource(), seed: 1, bus);
            var coordinator = Turns(bus);

            Assert.DoesNotThrow(() => coordinator.AdvanceTurn());
            Assert.AreEqual(0, active.Count);
        }
    }
}
