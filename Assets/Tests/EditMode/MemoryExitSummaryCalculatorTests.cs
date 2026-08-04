using System.Collections.Generic;
using GameName.Core.Analysis;
using GameName.Core.Clues;
using GameName.Core.Commissions;
using GameName.Core.Emotions;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Judging;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 이탈 확인 화면이 보여줄 "지금 나가면 무엇을 잃는지" 개수 계산을 검증한다.
    public class MemoryExitSummaryCalculatorTests
    {
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");
        private static readonly MemoryRoomId Room2 = new MemoryRoomId("room-2");
        private static readonly MemoryRoomId Room3 = new MemoryRoomId("room-3");

        private static ClueInfo MakeClueInfo(string id, MemoryRoomId roomId) =>
            new ClueInfo(
                new ClueId(id), roomId,
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Joy, 1) }));

        [Test]
        public void 인벤토리와_보관대의_단서_중_한_번도_분석하지_않은_것만_센다()
        {
            var inventory = new PlayerInventory(new InventorySettings(5), new SharedSlotInventoryPolicy());
            var clueStorage = new ClueStorage(new ClueStorageSettings(5));
            var analysisProgress = new ClueAnalysisProgress();
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var restorationTracker = new MemoryRoomRestorationTracker(eventBus);

            inventory.TryStore(MakeClueInfo("clue-1", Room1)); // 미분석
            inventory.TryStore(MakeClueInfo("clue-2", Room1));
            analysisProgress.RecordDepth(new ClueId("clue-2"), AnalysisDepth.Basic); // 분석 완료
            clueStorage.TryStore(MakeClueInfo("clue-3", Room1)); // 미분석

            var summary = MemoryExitSummaryCalculator.Calculate(
                inventory, clueStorage, analysisProgress, new[] { Room1 }, restorationTracker);

            Assert.AreEqual(2, summary.UnanalyzedClueCount);
        }

        [Test]
        public void 아직_복원하지_못한_방의_개수를_센다()
        {
            var inventory = new PlayerInventory(new InventorySettings(5), new SharedSlotInventoryPolicy());
            var clueStorage = new ClueStorage(new ClueStorageSettings(5));
            var analysisProgress = new ClueAnalysisProgress();
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var restorationTracker = new MemoryRoomRestorationTracker(eventBus);

            restorationTracker.ReportJudgement(
                Room1,
                new ScentJudgementResult(isBaseEmotionCorrect: true, stage: FeedbackStage.PianoAndViolinAndDrum, accuracy: 1.0));

            var roomIds = new List<MemoryRoomId> { Room1, Room2, Room3 };

            var summary = MemoryExitSummaryCalculator.Calculate(
                inventory, clueStorage, analysisProgress, roomIds, restorationTracker);

            Assert.AreEqual(2, summary.UnrestoredRoomCount);
        }

        [Test]
        public void 더_잃을_것이_없으면_둘_다_0이다()
        {
            var inventory = new PlayerInventory(new InventorySettings(5), new SharedSlotInventoryPolicy());
            var clueStorage = new ClueStorage(new ClueStorageSettings(5));
            var analysisProgress = new ClueAnalysisProgress();
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var restorationTracker = new MemoryRoomRestorationTracker(eventBus);

            restorationTracker.ReportJudgement(
                Room1,
                new ScentJudgementResult(isBaseEmotionCorrect: true, stage: FeedbackStage.PianoAndViolinAndDrum, accuracy: 1.0));

            var summary = MemoryExitSummaryCalculator.Calculate(
                inventory, clueStorage, analysisProgress, new[] { Room1 }, restorationTracker);

            Assert.AreEqual(0, summary.UnanalyzedClueCount);
            Assert.AreEqual(0, summary.UnrestoredRoomCount);
        }
    }
}
