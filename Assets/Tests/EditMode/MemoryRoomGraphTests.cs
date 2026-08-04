using GameName.Core.Events;
using GameName.Core.Judging;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class MemoryRoomGraphTests
    {
        private static readonly MemoryGraphNodeId Staircase = new MemoryGraphNodeId("staircase");
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");
        private static readonly MemoryRoomId Room2 = new MemoryRoomId("room-2");

        // 이동 패널이 실제로 쓸 최소 픽스처: 계단-방1(가로 연결) + 방1-방2(사다리,
        // 방1 복원 필요).
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

        // 이동 패널이 "사다리 잠김" 배지를 그리는 데 실제로 쓰는 것과 정확히
        // 같은 조합(TryGetLadderLowerRoom + IsRestored)으로 잠김 -> 복원 후
        // 열림을 증명한다.
        [Test]
        public void 잠긴_사다리는_아래_방이_복원되면_잠김_판정이_열림으로_바뀐다()
        {
            var graph = MakeGraph();
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var tracker = new MemoryRoomRestorationTracker(eventBus);

            var from = MemoryGraphNodeId.OfRoom(Room1);
            var to = MemoryGraphNodeId.OfRoom(Room2);

            var hasLadder = graph.TryGetLadderLowerRoom(from, to, out var lowerRoomId);
            var isLockedBefore = hasLadder && !tracker.IsRestored(lowerRoomId);

            tracker.ReportJudgement(
                Room1,
                new ScentJudgementResult(
                    isBaseEmotionCorrect: true, stage: FeedbackStage.PianoAndViolinAndDrum, accuracy: 1.0));

            var isLockedAfter = hasLadder && !tracker.IsRestored(lowerRoomId);

            Assert.IsTrue(hasLadder);
            Assert.IsTrue(isLockedBefore);
            Assert.IsFalse(isLockedAfter);
        }
    }
}
