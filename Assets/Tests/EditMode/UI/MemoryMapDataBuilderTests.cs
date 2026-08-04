using System.Linq;
using GameName.Core.Events;
using GameName.Core.Judging;
using GameName.Core.MemoryRooms;
using GameName.UI.Shared;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // 지도가 잠긴/열린 사다리를 구분해 그리는 데 실제로 쓰는 계산
    // (MemoryMapDataBuilder.BuildConnections)이 옳은지 확인한다.
    public class MemoryMapDataBuilderTests
    {
        private static readonly MemoryRoomId UpperRoom = new MemoryRoomId("room-upper");
        private static readonly MemoryRoomId LowerRoom = new MemoryRoomId("room-lower");
        private static readonly MemoryGraphNodeId UpperNodeId = MemoryGraphNodeId.OfRoom(UpperRoom);
        private static readonly MemoryGraphNodeId LowerNodeId = MemoryGraphNodeId.OfRoom(LowerRoom);

        private static MemoryRoomGraph MakeGraph()
        {
            var nodes = new[]
            {
                new MemoryGraphNode(UpperNodeId, MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(0, 1)),
                new MemoryGraphNode(LowerNodeId, MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(0, 2)),
            };
            var ladderConnections = new[] { new LadderConnection(upperRoom: UpperRoom, lowerRoom: LowerRoom) };

            return new MemoryRoomGraph(nodes, new OpenConnection[0], ladderConnections);
        }

        [Test]
        public void 아래_방이_복원되기_전에는_사다리_연결이_잠김으로_표시된다()
        {
            var graph = MakeGraph();
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var tracker = new MemoryRoomRestorationTracker(eventBus);

            var connections = MemoryMapDataBuilder.BuildConnections(graph, tracker);

            var ladder = connections.Single(c => c.IsLadder);
            Assert.IsTrue(ladder.IsLocked);
        }

        [Test]
        public void 아래_방이_복원되면_사다리_연결이_열림으로_바뀐다()
        {
            var graph = MakeGraph();
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var tracker = new MemoryRoomRestorationTracker(eventBus);

            tracker.ReportJudgement(
                LowerRoom,
                new ScentJudgementResult(isBaseEmotionCorrect: true, stage: FeedbackStage.PianoAndViolinAndDrum, accuracy: 1.0));

            var connections = MemoryMapDataBuilder.BuildConnections(graph, tracker);

            var ladder = connections.Single(c => c.IsLadder);
            Assert.IsFalse(ladder.IsLocked);
        }
    }
}
