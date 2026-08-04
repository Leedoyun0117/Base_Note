using System;
using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Clues;
using GameName.Core.Commissions;
using GameName.Core.Dialogue;
using GameName.Core.Emotions;
using GameName.Core.Events;
using GameName.Core.FinalCrafting;
using GameName.Core.Inventory;
using GameName.Core.Journal;
using GameName.Core.Judging;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using GameName.Core.Rewards;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 섹션 2 검증: 모든 방에 최종 향이 채워져야만 의뢰를 완료(제공)할 수 있고,
    // 완료되면 CommissionSession 단계가 Completed로 바뀌며 보상이 진열에
    // 더해진다.
    public class CommissionCompletionProcessorTests
    {
        private static readonly MemoryGraphNodeId EntryNode = new MemoryGraphNodeId("staircase");
        private static readonly MemoryRoomId RoomA = new MemoryRoomId("room-a");
        private static readonly MemoryRoomId RoomB = new MemoryRoomId("room-b");

        private sealed class Fixture
        {
            public FinalCraftingBoard Board;
            public MemoryRoomAnswerRepository AnswerRepository;
            public CommissionCompletionProcessor Processor;
            public CommissionSession Session;
            public DisplayCollection DisplayCollection;
            public RewardTable RewardTable;
            public IReadOnlyList<MemoryRoomId> RoomIds;
            public EventBus EventBus;
        }

        private static MemoryRoomAnswer MakeAnswer(MemoryRoomId roomId, EmotionType baseEmotion, int loveIntensity) =>
            new MemoryRoomAnswer(
                roomId, new Scent(baseEmotion, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, loveIntensity) })));

        private static Fixture MakeFixture()
        {
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var board = new FinalCraftingBoard();
            var answerRepository = new MemoryRoomAnswerRepository(new[]
            {
                MakeAnswer(RoomA, EmotionType.Joy, 5),
                MakeAnswer(RoomB, EmotionType.Fear, 5),
            });
            var scentJudge = new ScentJudge(new ScentJudgementSettings(highAccuracyThreshold: 0.8));
            var displayCollection = new DisplayCollection();

            var mentalityGauge = new MentalityGauge(
                new MentalityCostSettings(100, 100, 1, 20, 30, 8, 20), eventBus);
            var inventory = new PlayerInventory(new InventorySettings(5), new SharedSlotInventoryPolicy());
            var storage = new AmpouleStorage(new AmpouleStorageSettings(3));
            var restorationTracker = new MemoryRoomRestorationTracker(eventBus);
            var analysisProgress = new ClueAnalysisProgress();
            var journal = new PlayerJournal(eventBus, storage, inventory);

            var dialogueNodes = new List<DialogueNode>
            {
                new DialogueNode(new DialogueLine("A", "끝"), Array.Empty<DialogueOption>()),
            };
            var dialogueProgressor = new DialogueProgressor(new DialogueScript(dialogueNodes), eventBus);
            var playerLocation = new PlayerLocation(EntryNode);

            var resettables = new List<IResettable>
            {
                mentalityGauge, (IResettable)inventory, (IResettable)storage, restorationTracker, analysisProgress, board,
            };

            var session = new CommissionSession(
                eventBus, resettables, journal, dialogueProgressor, playerLocation, EntryNode);

            var processor = new CommissionCompletionProcessor(
                board, answerRepository, scentJudge, displayCollection, session, eventBus);

            // 완료를 시도하려면 ReturnedToReality 단계여야 한다.
            session.Begin(new CommissionId("commission-1"));
            session.TryAdvanceToMemory();
            session.TryReturnToReality();

            var rewardTable = new RewardTable(new[]
            {
                new RewardTier(minimumAverageAccuracy: 0.0, emotionalValue: 10, reactionDialogue: "그저 그렇네요."),
                new RewardTier(minimumAverageAccuracy: 0.9, emotionalValue: 50, reactionDialogue: "정말 감사합니다!"),
            });

            return new Fixture
            {
                Board = board,
                AnswerRepository = answerRepository,
                Processor = processor,
                Session = session,
                DisplayCollection = displayCollection,
                RewardTable = rewardTable,
                RoomIds = new[] { RoomA, RoomB },
                EventBus = eventBus,
            };
        }

        [Test]
        public void 방이_하나라도_비어있으면_완료할_수_없다()
        {
            var fixture = MakeFixture();
            fixture.Board.Set(RoomA, new Scent(EmotionType.Joy, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) })));

            Assert.IsFalse(fixture.Processor.CanComplete(fixture.RoomIds));

            var result = fixture.Processor.Complete(fixture.RoomIds, fixture.RewardTable);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(CommissionCompletionFailureReason.RoomsIncomplete, result.FailureReason);
            Assert.AreEqual(CommissionStage.ReturnedToReality, fixture.Session.Stage);
        }

        [Test]
        public void 모든_방이_채워지면_완료되고_단계가_Completed로_바뀌며_보상이_진열에_더해진다()
        {
            var fixture = MakeFixture();
            fixture.Board.Set(RoomA, new Scent(EmotionType.Joy, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) })));
            fixture.Board.Set(RoomB, new Scent(EmotionType.Fear, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) })));

            Assert.IsTrue(fixture.Processor.CanComplete(fixture.RoomIds));

            var result = fixture.Processor.Complete(fixture.RoomIds, fixture.RewardTable);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1.0, result.AverageAccuracy);
            Assert.AreEqual(50, result.Gift.EmotionalValue);
            Assert.AreEqual(CommissionStage.Completed, fixture.Session.Stage);
            Assert.AreEqual(50, fixture.DisplayCollection.AvailableEmotionalValue);
        }

        [Test]
        public void 바탕_감정이_틀린_방이_섞이면_평균_정확도가_낮아져_낮은_등급_보상을_받는다()
        {
            var fixture = MakeFixture();
            fixture.Board.Set(RoomA, new Scent(EmotionType.Joy, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) })));
            // RoomB 정답의 바탕 감정은 Fear인데 Joy로 확정한다 — 보조 감정
            // 배합은 정답과 완전히 같지만 바탕이 틀렸다.
            fixture.Board.Set(RoomB, new Scent(EmotionType.Joy, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) })));

            var result = fixture.Processor.Complete(fixture.RoomIds, fixture.RewardTable);

            Assert.IsTrue(result.Succeeded);
            // RoomA는 1.0, RoomB는 바탕이 틀려 유효 정확도 0.0 -> 평균 0.5.
            Assert.AreEqual(0.5, result.AverageAccuracy);
            Assert.AreEqual(10, result.Gift.EmotionalValue);
        }

        // 회귀 테스트: 완료 화면(UI)은 CommissionStageChangedEvent를 구독하는
        // SceneScreenSwitcher가 즉시(동기적으로) 전환시키므로, 그 화면의
        // 컨트롤러가 결과를 읽으러 오는 시점에는 이미 CommissionCompletedEvent가
        // 먼저 발행되어 있어야 한다. 순서가 바뀌면 완료 화면이 빈 결과("-")로
        // 한 번 그려진 뒤 다시는 갱신되지 않는 버그가 재현된다.
        [Test]
        public void CommissionCompletedEvent는_CommissionStageChangedEvent보다_먼저_발행된다()
        {
            var fixture = MakeFixture();
            fixture.Board.Set(RoomA, new Scent(EmotionType.Joy, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) })));
            fixture.Board.Set(RoomB, new Scent(EmotionType.Fear, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) })));

            var order = new List<string>();
            using (fixture.EventBus.Subscribe<CommissionCompletedEvent>(_ => order.Add("completed")))
            using (fixture.EventBus.Subscribe<CommissionStageChangedEvent>(_ => order.Add("stage-changed")))
            {
                fixture.Processor.Complete(fixture.RoomIds, fixture.RewardTable);
            }

            CollectionAssert.AreEqual(new[] { "completed", "stage-changed" }, order);
        }
    }
}
