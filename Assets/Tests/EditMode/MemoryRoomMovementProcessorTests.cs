using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class MemoryRoomMovementProcessorTests
    {
        private static readonly MemoryGraphNodeId Staircase = new MemoryGraphNodeId("staircase");
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");
        private static readonly MemoryRoomId Room2 = new MemoryRoomId("room-2");
        private static readonly MemoryRoomId Room3 = new MemoryRoomId("room-3");

        // 계단-Room1(문) + Room1-Room2(문) + Room2-Room3(사다리) 구조의 최소
        // 픽스처. 계단과 Room2/Room3 사이에는 연결이 없어 실패 경로도 만든다.
        private static MemoryRoomGraph MakeGraph()
        {
            var nodes = new[]
            {
                new MemoryGraphNode(Staircase, MemoryGraphNodeType.Staircase, new MemoryGraphCoordinate(0, 0)),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room1), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(1, 3)),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room2), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(1, 2)),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room3), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(1, 1)),
            };

            var openConnections = new[]
            {
                new OpenConnection(Staircase, MemoryGraphNodeId.OfRoom(Room1)),
                new OpenConnection(MemoryGraphNodeId.OfRoom(Room1), MemoryGraphNodeId.OfRoom(Room2)),
            };

            var ladderConnections = new[]
            {
                new LadderConnection(upperRoom: Room3, lowerRoom: Room2),
            };

            return new MemoryRoomGraph(nodes, openConnections, ladderConnections);
        }

        private static MemoryRoomMovementProcessor MakeProcessor(
            out IPlayerLocation playerLocation,
            out EventBus eventBus,
            MemoryGraphNodeId initialPosition)
        {
            eventBus = new EventBus(new NoOpEventExceptionHandler());
            var location = new PlayerLocation(initialPosition);
            playerLocation = location;
            return new MemoryRoomMovementProcessor(MakeGraph(), location, eventBus);
        }

        [Test]
        public void 가로_연결은_조건_없이_통행된다()
        {
            var processor = MakeProcessor(out _, out _, MemoryGraphNodeId.OfRoom(Room1));

            var result = processor.Move(MemoryGraphNodeId.OfRoom(Room2));

            Assert.IsTrue(result.Succeeded);
        }

        [Test]
        public void 사다리도_가로_연결과_똑같이_통행된다()
        {
            var processor = MakeProcessor(out _, out _, MemoryGraphNodeId.OfRoom(Room2));

            var result = processor.Move(MemoryGraphNodeId.OfRoom(Room3));

            Assert.IsTrue(result.Succeeded);
        }

        [Test]
        public void 사다리는_양방향으로_통행된다()
        {
            var processor = MakeProcessor(out _, out _, MemoryGraphNodeId.OfRoom(Room3));

            var result = processor.Move(MemoryGraphNodeId.OfRoom(Room2));

            Assert.IsTrue(result.Succeeded);
        }

        [Test]
        public void 허브에서_기억_방으로_이동할_수_있다()
        {
            var processor = MakeProcessor(out var location, out _, Staircase);

            var result = processor.Move(MemoryGraphNodeId.OfRoom(Room1));

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(MemoryGraphNodeId.OfRoom(Room1), location.Current);
        }

        [Test]
        public void 연결이_없으면_이동이_실패하고_사유는_NoConnection이다()
        {
            var processor = MakeProcessor(out _, out _, Staircase);

            var result = processor.Move(MemoryGraphNodeId.OfRoom(Room3));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(MemoryGraphMoveFailureReason.NoConnection, result.FailureReason);
        }

        [Test]
        public void 이동에_성공하면_현재_위치가_목적지로_갱신된다()
        {
            var processor = MakeProcessor(out var location, out _, MemoryGraphNodeId.OfRoom(Room1));

            processor.Move(MemoryGraphNodeId.OfRoom(Room2));

            Assert.AreEqual(MemoryGraphNodeId.OfRoom(Room2), location.Current);
        }

        [Test]
        public void 이동에_실패하면_현재_위치가_그대로_유지된다()
        {
            var initialPosition = Staircase;
            var processor = MakeProcessor(out var location, out _, initialPosition);

            // 계단 -> Room3은 연결이 없어 실패해야 한다.
            var result = processor.Move(MemoryGraphNodeId.OfRoom(Room3));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(initialPosition, location.Current);
        }

        [Test]
        public void 이동에_성공하면_이동_완료_이벤트가_전후_위치와_함께_발행된다()
        {
            var processor = MakeProcessor(out _, out var eventBus, MemoryGraphNodeId.OfRoom(Room1));

            MemoryRoomMoveCompletedEvent? received = null;
            using (eventBus.Subscribe<MemoryRoomMoveCompletedEvent>(e => received = e))
            {
                processor.Move(MemoryGraphNodeId.OfRoom(Room2));
            }

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(MemoryGraphNodeId.OfRoom(Room1), received.Value.PreviousPosition);
            Assert.AreEqual(MemoryGraphNodeId.OfRoom(Room2), received.Value.NewPosition);
        }

        [Test]
        public void 이동에_실패하면_이동_완료_이벤트가_발행되지_않는다()
        {
            var processor = MakeProcessor(out _, out var eventBus, Staircase);

            var receivedCount = 0;
            using (eventBus.Subscribe<MemoryRoomMoveCompletedEvent>(e => receivedCount++))
            {
                processor.Move(MemoryGraphNodeId.OfRoom(Room3)); // 연결이 없어 실패해야 한다.
            }

            Assert.AreEqual(0, receivedCount);
        }
    }
}
