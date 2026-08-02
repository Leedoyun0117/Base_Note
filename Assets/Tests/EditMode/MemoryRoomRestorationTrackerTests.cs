using GameName.Core.Events;
using GameName.Core.Judging;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class MemoryRoomRestorationTrackerTests
    {
        private static ScentJudgementResult MakeResult(FeedbackStage stage) =>
            new ScentJudgementResult(
                isBaseEmotionCorrect: true,
                stage: stage,
                accuracy: stage == FeedbackStage.PianoAndViolinAndDrum ? 1.0 : 0.5);

        [Test]
        public void 최상위_단계면_복원으로_기록되고_이벤트가_발행된다()
        {
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var receivedCount = 0;

            using (eventBus.Subscribe<MemoryRoomRestoredEvent>(e => receivedCount++))
            {
                var tracker = new MemoryRoomRestorationTracker(eventBus);
                var roomId = new MemoryRoomId("room-1");

                tracker.ReportJudgement(roomId, MakeResult(FeedbackStage.PianoAndViolinAndDrum));

                Assert.IsTrue(tracker.IsRestored(roomId));
                Assert.AreEqual(1, receivedCount);
            }
        }

        [Test]
        public void 최상위_단계가_아니면_복원되지_않는다()
        {
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var tracker = new MemoryRoomRestorationTracker(eventBus);
            var roomId = new MemoryRoomId("room-1");

            tracker.ReportJudgement(roomId, MakeResult(FeedbackStage.PianoAndViolin));

            Assert.IsFalse(tracker.IsRestored(roomId));
        }

        [Test]
        public void 같은_방을_두_번_복원해도_이벤트는_한_번만_발행된다()
        {
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var receivedCount = 0;

            using (eventBus.Subscribe<MemoryRoomRestoredEvent>(e => receivedCount++))
            {
                var tracker = new MemoryRoomRestorationTracker(eventBus);
                var roomId = new MemoryRoomId("room-1");

                tracker.ReportJudgement(roomId, MakeResult(FeedbackStage.PianoAndViolinAndDrum));
                tracker.ReportJudgement(roomId, MakeResult(FeedbackStage.PianoAndViolinAndDrum));

                Assert.AreEqual(1, receivedCount);
            }
        }

        [Test]
        public void Reset_후에는_복원했던_방도_다시_미복원_상태다()
        {
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var tracker = new MemoryRoomRestorationTracker(eventBus);
            var roomId = new MemoryRoomId("room-1");
            tracker.ReportJudgement(roomId, MakeResult(FeedbackStage.PianoAndViolinAndDrum));

            tracker.Reset();

            Assert.IsFalse(tracker.IsRestored(roomId));
        }

        [Test]
        public void Reset은_복원_이벤트를_발행하지_않는다()
        {
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var tracker = new MemoryRoomRestorationTracker(eventBus);
            var roomId = new MemoryRoomId("room-1");
            tracker.ReportJudgement(roomId, MakeResult(FeedbackStage.PianoAndViolinAndDrum));

            var receivedCount = 0;
            using (eventBus.Subscribe<MemoryRoomRestoredEvent>(e => receivedCount++))
            {
                tracker.Reset();

                Assert.AreEqual(0, receivedCount);
            }
        }
    }
}
