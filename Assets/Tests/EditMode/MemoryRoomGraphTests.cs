using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class MemoryRoomGraphTests
    {
        private static readonly MemoryGraphNodeId Staircase = new MemoryGraphNodeId("staircase");
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");
        private static readonly MemoryRoomId Room2 = new MemoryRoomId("room-2");

        // 최소 픽스처: 계단-방1(가로 연결) + 방1-방2(사다리).
        private static MemoryRoomGraph MakeGraph()
        {
            var nodes = new[]
            {
                new MemoryGraphNode(Staircase, MemoryGraphNodeType.Staircase, new MemoryGraphCoordinate(0, 0)),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room1), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(0, 2)),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room2), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(0, 1)),
            };

            var openConnections = new[] { new OpenConnection(Staircase, MemoryGraphNodeId.OfRoom(Room1)) };
            var ladderConnections = new[] { new LadderConnection(upperRoom: Room2, lowerRoom: Room1) };

            return new MemoryRoomGraph(nodes, openConnections, ladderConnections);
        }

        [Test]
        public void 인접_노드_목록에는_가로_연결과_사다리_연결이_모두_포함된다()
        {
            var graph = MakeGraph();

            var neighbors = graph.GetNeighborIds(MemoryGraphNodeId.OfRoom(Room1));

            CollectionAssert.Contains(neighbors, Staircase);
            CollectionAssert.Contains(neighbors, MemoryGraphNodeId.OfRoom(Room2));
        }

        [Test]
        public void 연결이_없는_노드는_인접_목록에_없다()
        {
            var graph = MakeGraph();

            var neighbors = graph.GetNeighborIds(Staircase);

            CollectionAssert.DoesNotContain(neighbors, MemoryGraphNodeId.OfRoom(Room2));
        }

        [Test]
        public void 사다리는_어느_방향으로_물어도_아래쪽_방을_돌려준다()
        {
            var graph = MakeGraph();

            var upward = graph.TryGetLadderLowerRoom(
                MemoryGraphNodeId.OfRoom(Room1), MemoryGraphNodeId.OfRoom(Room2), out var lowerFromBelow);
            var downward = graph.TryGetLadderLowerRoom(
                MemoryGraphNodeId.OfRoom(Room2), MemoryGraphNodeId.OfRoom(Room1), out var lowerFromAbove);

            Assert.IsTrue(upward);
            Assert.IsTrue(downward);
            Assert.AreEqual(Room1, lowerFromBelow);
            Assert.AreEqual(Room1, lowerFromAbove);
        }

        [Test]
        public void 사다리가_아닌_연결은_사다리로_조회되지_않는다()
        {
            var graph = MakeGraph();

            var isLadder = graph.TryGetLadderLowerRoom(Staircase, MemoryGraphNodeId.OfRoom(Room1), out _);

            Assert.IsFalse(isLadder);
        }
    }
}