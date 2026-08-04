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
            public void Reset() => _restored.Clear();
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
                new MemoryGraphNode(Staircase, MemoryGraphNodeType.Staircase, new MemoryGraphCoordinate(0, 0)),
                new MemoryGraphNode(AnalysisRoom, MemoryGraphNodeType.AnalysisRoom, new MemoryGraphCoordinate(1, 0)),
                new MemoryGraphNode(PerfumeryRoom, MemoryGraphNodeType.PerfumeryRoom, new MemoryGraphCoordinate(2, 0)),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room1), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(1, 3)),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room2), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(1, 2)),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room3), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(1, 1)),
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
            out EventBus eventBus,
            MemoryGraphNodeId initialPosition,
            int initialMentality = 100,
            int moveCost = 1)
        {
            tracker = new FakeRestorationTracker();
            var settings = new MentalityCostSettings(
                initialMentality: initialMentality, maxMentality: 100,
                memoryRoomMoveCost: moveCost, basicAnalysisCost: 20, advancedAnalysisCost: 30,
                ampouleCraftingCost: 8, memoryRoomFullRestorationRecovery: 20);
            eventBus = new EventBus(new NoOpEventExceptionHandler());
            gauge = new MentalityGauge(settings, eventBus);
            var location = new PlayerLocation(initialPosition);
            playerLocation = location;
            return new MemoryRoomMovementProcessor(MakeGraph(), tracker, gauge, settings, location, eventBus);
        }

        [Test]
        public void 가로_연결은_조건_없이_통행된다()
        {
            var processor = MakeProcessor(
                out _, out _, out _, out _, initialPosition: MemoryGraphNodeId.OfRoom(Room1));

            var result = processor.Move(MemoryGraphNodeId.OfRoom(Room2));

            Assert.IsTrue(result.Succeeded);
        }

        [Test]
        public void 사다리는_아래_방이_복원되기_전에는_막힌다()
        {
            var processor = MakeProcessor(
                out _, out _, out _, out _, initialPosition: MemoryGraphNodeId.OfRoom(Room2));

            var result = processor.Move(MemoryGraphNodeId.OfRoom(Room3));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(MemoryGraphMoveFailureReason.LadderLocked, result.FailureReason);
        }

        [Test]
        public void 사다리는_아래_방이_복원되면_열린다()
        {
            var processor = MakeProcessor(
                out var tracker, out _, out _, out _, initialPosition: MemoryGraphNodeId.OfRoom(Room2));
            tracker.MarkRestored(Room2);

            var result = processor.Move(MemoryGraphNodeId.OfRoom(Room3));

            Assert.IsTrue(result.Succeeded);
        }

        [Test]
        public void 기억_방_이동은_정신력을_1_소모한다()
        {
            var processor = MakeProcessor(
                out _, out var gauge, out _, out _, initialPosition: MemoryGraphNodeId.OfRoom(Room1));

            processor.Move(MemoryGraphNodeId.OfRoom(Room2));

            Assert.AreEqual(99, gauge.CurrentValue);
        }

        [Test]
        public void 이미_복원된_방으로_이동하면_정신력을_소모하지_않는다()
        {
            var processor = MakeProcessor(
                out var tracker, out var gauge, out _, out _, initialPosition: MemoryGraphNodeId.OfRoom(Room1));
            tracker.MarkRestored(Room2);

            var result = processor.Move(MemoryGraphNodeId.OfRoom(Room2));

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(100, gauge.CurrentValue);
        }

        [Test]
        public void 한_번_가_본_방으로_다시_이동하면_복원_여부와_무관하게_정신력을_소모하지_않는다()
        {
            var processor = MakeProcessor(
                out _, out var gauge, out _, out _, initialPosition: MemoryGraphNodeId.OfRoom(Room1));

            processor.Move(MemoryGraphNodeId.OfRoom(Room2)); // 첫 방문 — 1 소모(99)
            processor.Move(MemoryGraphNodeId.OfRoom(Room1)); // 되돌아옴 — Room1은 이미 가 본 곳
            var result = processor.Move(MemoryGraphNodeId.OfRoom(Room2)); // 재방문 — 무료

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(99, gauge.CurrentValue);
        }

        [Test]
        public void Reset하면_방문_기록이_지워져_다시_비용이_청구된다()
        {
            var processor = MakeProcessor(
                out _, out var gauge, out _, out _, initialPosition: MemoryGraphNodeId.OfRoom(Room1));
            processor.Move(MemoryGraphNodeId.OfRoom(Room2));

            processor.Reset();
            processor.Move(MemoryGraphNodeId.OfRoom(Room1)); // 방문 기록이 지워졌으니 다시 유료(98)

            // Room2 -> Room1로 떠나는 이 이동 자체가 Room2를 "가 본 곳"으로 다시
            // 표시하므로(76-81행), 곧바로 Room2로 되돌아가는 다음 이동은 무료다.
            var result = processor.Move(MemoryGraphNodeId.OfRoom(Room2));

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(98, gauge.CurrentValue);
        }

        [Test]
        public void 허브_이동과_허브_방_이동은_정신력을_소모하지_않는다()
        {
            var processor = MakeProcessor(out _, out var gauge, out _, out _, initialPosition: Staircase);

            processor.Move(AnalysisRoom);
            processor.Move(MemoryGraphNodeId.OfRoom(Room1));

            Assert.AreEqual(100, gauge.CurrentValue);
        }

        // 조향실 화면이 가장 먼저 만들어져 이동 개념이 생기기 전에 굳는
        // 바람에, 조향실에서 나가는 이동 수단이 UI에 빠져 있던 적이 있었다.
        // 그래프 자체(계단/분석실과의 연결)는 처음부터 있었으므로 여기서는
        // 그 연결이 실제로 동작하는지만 다시 확인한다.
        [Test]
        public void 조향실에서_계단으로_이동할_수_있고_정신력을_소모하지_않는다()
        {
            var processor = MakeProcessor(out _, out var gauge, out _, out _, initialPosition: PerfumeryRoom);

            var result = processor.Move(Staircase);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(100, gauge.CurrentValue);
        }

        [Test]
        public void 조향실에서_분석실로_이동할_수_있고_정신력을_소모하지_않는다()
        {
            var processor = MakeProcessor(out _, out var gauge, out _, out _, initialPosition: PerfumeryRoom);

            var result = processor.Move(AnalysisRoom);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(100, gauge.CurrentValue);
        }

        [Test]
        public void 정신력이_0이어도_기억_방을_지나_계단까지_이동할_수_있다()
        {
            var processor = MakeProcessor(
                out _, out var gauge, out var location, out _,
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
                out _, out _, out _, out _, initialPosition: MemoryGraphNodeId.OfRoom(Room1));

            var result = processor.Move(PerfumeryRoom);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(MemoryGraphMoveFailureReason.NoConnection, result.FailureReason);
        }

        [Test]
        public void 정신력이_바닥은_아니지만_부족하면_사유는_InsufficientMentality다()
        {
            // 잔량(1)보다 이동 비용(2)이 큰, "바닥은 아니지만 부족한" 상황을 만든다.
            var processor = MakeProcessor(
                out _, out var gauge, out _, out _,
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
                out _, out _, out var location, out _, initialPosition: MemoryGraphNodeId.OfRoom(Room1));

            processor.Move(MemoryGraphNodeId.OfRoom(Room2));

            Assert.AreEqual(MemoryGraphNodeId.OfRoom(Room2), location.Current);
        }

        [Test]
        public void 이동에_실패하면_현재_위치가_그대로_유지된다()
        {
            var initialPosition = MemoryGraphNodeId.OfRoom(Room1);
            var processor = MakeProcessor(
                out _, out _, out var location, out _, initialPosition: initialPosition);

            // Room1 -> PerfumeryRoom은 연결이 없어 실패해야 한다.
            var result = processor.Move(PerfumeryRoom);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(initialPosition, location.Current);
        }

        [Test]
        public void 기억_방_사이_이동_비용_미리보기는_실제_소모와_같다()
        {
            var processor = MakeProcessor(
                out _, out var gauge, out _, out _, initialPosition: MemoryGraphNodeId.OfRoom(Room1));

            var previewedCost = processor.PreviewCost(MemoryGraphNodeId.OfRoom(Room2));
            processor.Move(MemoryGraphNodeId.OfRoom(Room2));

            Assert.AreEqual(1, previewedCost);
            Assert.AreEqual(100 - previewedCost, gauge.CurrentValue);
        }

        [Test]
        public void 허브로_가는_이동_비용_미리보기는_0이다()
        {
            var processor = MakeProcessor(out _, out _, out _, out _, initialPosition: MemoryGraphNodeId.OfRoom(Room1));

            var previewedCost = processor.PreviewCost(AnalysisRoom);

            Assert.AreEqual(0, previewedCost);
        }

        [Test]
        public void 정신력이_0이면_기억_방_이동_비용_미리보기도_0이다()
        {
            var processor = MakeProcessor(
                out _, out _, out _, out _,
                initialPosition: MemoryGraphNodeId.OfRoom(Room1), initialMentality: 0);

            var previewedCost = processor.PreviewCost(MemoryGraphNodeId.OfRoom(Room2));

            Assert.AreEqual(0, previewedCost);
        }

        [Test]
        public void 이동에_성공하면_이동_완료_이벤트가_전후_위치와_함께_발행된다()
        {
            var processor = MakeProcessor(
                out _, out _, out _, out var eventBus, initialPosition: MemoryGraphNodeId.OfRoom(Room1));

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
            var processor = MakeProcessor(
                out _, out _, out _, out var eventBus, initialPosition: MemoryGraphNodeId.OfRoom(Room1));

            var receivedCount = 0;
            using (eventBus.Subscribe<MemoryRoomMoveCompletedEvent>(e => receivedCount++))
            {
                processor.Move(PerfumeryRoom); // 연결이 없어 실패해야 한다.
            }

            Assert.AreEqual(0, receivedCount);
        }
    }
}
