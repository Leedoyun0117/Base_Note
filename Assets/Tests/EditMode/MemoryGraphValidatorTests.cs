using System.Linq;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class MemoryGraphValidatorTests
    {
        private static MemoryGraphValidator MakeValidator() =>
            new MemoryGraphValidator(new IMemoryGraphConsistencyRule[] { new LadderRespectsDepthOrderRule() });

        [Test]
        public void 사다리의_위쪽_방이_아래쪽_방보다_Row가_크면_오류를_잡아낸다()
        {
            var upperRoom = new MemoryRoomId("room-upper");
            var lowerRoom = new MemoryRoomId("room-lower");

            // 좌표를 일부러 뒤집는다 — 위쪽 방(Upper)의 Row가 아래쪽 방(Lower)의
            // Row보다 커서(더 과거라서), "위로 갈수록 현재"라는 규칙을 어긴다.
            var nodes = new[]
            {
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(upperRoom), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(0, 5)),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(lowerRoom), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(0, 1)),
            };
            var ladderConnections = new[] { new LadderConnection(upperRoom: upperRoom, lowerRoom: lowerRoom) };

            var issues = MakeValidator().Validate(nodes, ladderConnections);

            Assert.IsTrue(issues.Any(i => i.Severity == MemoryGraphIssueSeverity.Error));
        }

        [Test]
        public void 사다리의_위쪽_방이_아래쪽_방보다_Row가_작으면_문제없다()
        {
            var upperRoom = new MemoryRoomId("room-upper");
            var lowerRoom = new MemoryRoomId("room-lower");

            var nodes = new[]
            {
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(upperRoom), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(0, 1)),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(lowerRoom), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(0, 5)),
            };
            var ladderConnections = new[] { new LadderConnection(upperRoom: upperRoom, lowerRoom: lowerRoom) };

            var issues = MakeValidator().Validate(nodes, ladderConnections);

            CollectionAssert.IsEmpty(issues);
        }
    }
}
