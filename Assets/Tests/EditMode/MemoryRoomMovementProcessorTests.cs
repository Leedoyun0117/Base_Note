using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.Judging;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class MemoryRoomMovementProcessorTests
    {
        // 이동 처리기 단위 테스트를 복원 판정 로직과 분리하기 위한 테스트 전용 트래커.
        // 실제 복원 조건(시향 판정)은 MemoryRoomRestorationTrackerTests에서 따로 검증한다.
        private sealed class FakeRestorationTracker : IMemoryRoomRestorationTracker
        {
            private readonly HashSet<MemoryRoomId> _restored = new HashSet<MemoryRoomId>();

            public bool IsRestored(MemoryRoomId roomId) => _restored.Contains(roomId);
            public void ReportJudgement(MemoryRoomId roomId, ScentJudgementResult result) { }
            public void MarkRestored(MemoryRoomId roomId) => _restored.Add(roomId);
        }

        private static readonly MemoryGraphNodeId Staircase = new MemoryGraphNodeId("staircase");
        private static readonly MemoryGraphNodeId AnalysisRoom = new MemoryGraphNodeId("analysis-room");
        private static readonly MemoryGraphNodeId PerfumeryRoom = new MemoryGraphNodeId("perfumery-room");
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");
        private static readonly MemoryRoomId Room2 = new MemoryRoomId("room-2");
        private static readonly MemoryRoomId Room3 = new MemoryRoomId("room-3");

        // 허브 삼각형(계단-분석실-조향실) + 분석실-Room1(문) + Room1-Room2(문) +
        // Room2-Room3(사다리, Room2 복원 필요) 구조의 최소 픽스처.
        private static MemoryRoomGraph MakeGraph()
        {
            var nodes = new[]
            {
                new MemoryGraphNode(Staircase, MemoryGraphNodeType.Staircase),
                new MemoryGraphNode(AnalysisRoom, MemoryGraphNodeType.AnalysisRoom),
                new MemoryGraphNode(PerfumeryRoom, MemoryGraphNodeType.PerfumeryRoom),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room1), MemoryGraphNodeType.MemoryRoom),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room2), MemoryGraphNodeType.MemoryRoom),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room3), MemoryGraphNodeType.MemoryRoom),
            };

            var openConnections = new[]
            {
                new OpenConnection(Staircase, AnalysisRoom),
                new OpenConnection(Staircase, PerfumeryRoom),
                new OpenConnection(AnalysisRoom, PerfumeryRoom),
                new OpenConnection(AnalysisRoom, MemoryGraphNodeId.OfRoom(Room1)),
                new OpenConnection(MemoryGraphNodeId.OfRoom(Room1), MemoryGraphNodeId.OfRoom(Room2)),
            };

            var ladderConnections = new[]
            {
                new LadderConnection(upperRoom: Room3, lowerRoom: Room2),
            };

            return new MemoryRoomGraph(nodes, openConnections, ladderConnections);
        }

        private static MemoryRoomMovementProcessor MakeProcessor(
            out FakeRestorationTracker tracker,
            out IMentalityGauge gauge,
            out IPlayerLocation playerLocation,
            MemoryGraphNodeId initialPosition,
            int initialMentality = 100,
            int moveCost = 1)
        {
            tracker = new FakeRestorationTracker();
            var settings = new MentalityCostSettings(
                initialMentality: initialMentality, maxMentality: 100,
                memoryRoomMoveCost: moveCost, basicAnalysisCost: 20, advancedAnalysisCost: 30,
                ampouleCraftingCost: 8, memoryRoomFullRestorationRecovery: 20);
            gauge = new MentalityGauge(settings, new EventBus(new NoOpEventExceptionHandler()));
            var location = new PlayerLocation(initialPosition);
            playerLocation = location;
            return new MemoryRoomMovementProcessor(MakeGraph(), tracker, gauge, settings, location);
        }

        [Test]
        public void 가로_연결은_조건_없이_통행된다()
        {
            var processor = MakeProcessor(
                out _, out _, out _, initialPosition: MemoryGraphNodeId.OfRoom(Room1));

            var result = processor.Move(MemoryGraphNodeId.OfRoom(Room2));

            Assert.IsTrue(result.Succeeded);
        }

        [Test]
        public void 사다리는_아래_방이_복원되기_전에는_막힌다()
        {
            var processor = MakeProcessor(
                out _, out _, out _, initialPosition: MemoryGraphNodeId.OfRoom(Room2));

            var result = processor.Move(MemoryGraphNodeId.OfRoom(Room3));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(MemoryGraphMoveFailureReason.LadderLocked, result.FailureReason);
        }

        [Test]
        public void 사다리는_아래_방이_복원되면_열린다()
        {
            var processor = MakeProcessor(
                out var tracker, out _, out _, initialPosition: MemoryGraphNodeId.OfRoom(Room2));
            tracker.MarkRestored(Room2);

            var result = processor.Move(MemoryGraphNodeId.OfRoom(Room3));

            Assert.IsTrue(result.Succeeded);
        }

        [Test]
        public void 기억_방_이동은_정신력을_1_소모한다()
        {
            var processor = MakeProcessor(
                out _, out var gauge, out _, initialPosition: MemoryGraphNodeId.OfRoom(Room1));

            processor.Move(MemoryGraphNodeId.OfRoom(Room2));

            Assert.AreEqual(99, gauge.CurrentValue);
        }

        [Test]
        public void 허브_이동과_허브_방_이동은_정신력을_소모하지_않는다()
        {
            var processor = MakeProcessor(out _, out var gauge, out _, initialPosition: Staircase);

            processor.Move(AnalysisRoom);
            processor.Move(MemoryGraphNodeId.OfRoom(Room1));

            Assert.AreEqual(100, gauge.CurrentValue);
        }

        [Test]
        public void 정신력이_0이어도_기억_방을_지나_계단까지_이동할_수_있다()
        {
            var processor = MakeProcessor(
                out _, out var gauge, out var location,
                initialPosition: MemoryGraphNodeId.OfRoom(Room2), initialMentality: 0);

            var step1 = processor.Move(MemoryGraphNodeId.OfRoom(Room1));
            var step2 = processor.Move(AnalysisRoom);
            var step3 = processor.Move(Staircase);

            Assert.IsTrue(step1.Succeeded);
            Assert.IsTrue(step2.Succeeded);
            Assert.IsTrue(step3.Succeeded);
            Assert.AreEqual(0, gauge.CurrentValue);
            Assert.AreEqual(Staircase, location.Current);
        }

        [Test]
        public void 연결이_없으면_이동이_실패하고_사유는_NoConnection이다()
        {
            var processor = MakeProcessor(
                out _, out _, out _, initialPosition: MemoryGraphNodeId.OfRoom(Room1));

            var result = processor.Move(PerfumeryRoom);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(MemoryGraphMoveFailureReason.NoConnection, result.FailureReason);
        }

        [Test]
        public void 정신력이_바닥은_아니지만_부족하면_사유는_InsufficientMentality다()
        {
            // 잔량(1)보다 이동 비용(2)이 큰, "바닥은 아니지만 부족한" 상황을 만든다.
            var processor = MakeProcessor(
                out _, out var gauge, out _,
                initialPosition: MemoryGraphNodeId.OfRoom(Room1), initialMentality: 1, moveCost: 2);

            var result = processor.Move(MemoryGraphNodeId.OfRoom(Room2));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(MemoryGraphMoveFailureReason.InsufficientMentality, result.FailureReason);
            Assert.AreEqual(1, gauge.CurrentValue);
        }

        [Test]
        public void 이동에_성공하면_현재_위치가_목적지로_갱신된다()
        {
            var processor = MakeProcessor(
                out _, out _, out var location, initialPosition: MemoryGraphNodeId.OfRoom(Room1));

            processor.Move(MemoryGraphNodeId.OfRoom(Room2));

            Assert.AreEqual(MemoryGraphNodeId.OfRoom(Room2), location.Current);
        }

        [Test]
        public void 이동에_실패하면_현재_위치가_그대로_유지된다()
        {
            var initialPosition = MemoryGraphNodeId.OfRoom(Room1);
            var processor = MakeProcessor(out _, out _, out var location, initialPosition: initialPosition);

            // Room1 -> PerfumeryRoom은 연결이 없어 실패해야 한다.
            var result = processor.Move(PerfumeryRoom);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(initialPosition, location.Current);
        }
    }
}
