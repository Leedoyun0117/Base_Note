using System;
using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Analysis;
using GameName.Core.Clues;
using GameName.Core.Commissions;
using GameName.Core.Dialogue;
using GameName.Core.Emotions;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Journal;
using GameName.Core.Judging;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class CommissionSessionTests
    {
        private sealed class Fixture
        {
            public CommissionSession Session;
            public EventBus EventBus;
            public MentalityGauge MentalityGauge;
            public IPlayerInventory Inventory;
            public IAmpouleStorage Storage;
            public IMemoryRoomRestorationTracker RestorationTracker;
            public IClueAnalysisProgress AnalysisProgress;
            public IClueStorage ClueStorage;
            public PlayerJournal Journal;
            public DialogueProgressor DialogueProgressor;
            public PlayerLocation PlayerLocation;
        }

        private static readonly MemoryGraphNodeId EntryNode = new MemoryGraphNodeId("staircase");
        private static readonly MemoryGraphNodeId OtherNode = new MemoryGraphNodeId("room-1");

        private static Fixture MakeFixture()
        {
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var costSettings = new MentalityCostSettings(
                initialMentality: 100, maxMentality: 100,
                memoryRoomMoveCost: 1, basicAnalysisCost: 20, advancedAnalysisCost: 30,
                ampouleCraftingCost: 8, memoryRoomFullRestorationRecovery: 20);
            var mentalityGauge = new MentalityGauge(costSettings, eventBus);
            var inventory = new PlayerInventory(new InventorySettings(5), new SharedSlotInventoryPolicy());
            var storage = new AmpouleStorage(new AmpouleStorageSettings(3));
            var restorationTracker = new MemoryRoomRestorationTracker(eventBus);
            var analysisProgress = new ClueAnalysisProgress();
            var clueStorage = new ClueStorage(new ClueStorageSettings(6));
            var journal = new PlayerJournal(eventBus, storage, inventory);

            var dialogueProgressor = new DialogueProgressor(MakeDialogueScript(), eventBus);

            var playerLocation = new PlayerLocation(EntryNode);

            var resettableSystems = new List<IResettable>
            {
                mentalityGauge, (IResettable)inventory, (IResettable)storage, restorationTracker, analysisProgress,
                clueStorage,
            };

            var session = new CommissionSession(
                eventBus, resettableSystems, journal, dialogueProgressor, playerLocation, EntryNode);

            return new Fixture
            {
                Session = session,
                EventBus = eventBus,
                MentalityGauge = mentalityGauge,
                Inventory = inventory,
                Storage = storage,
                RestorationTracker = restorationTracker,
                AnalysisProgress = analysisProgress,
                ClueStorage = clueStorage,
                Journal = journal,
                DialogueProgressor = dialogueProgressor,
                PlayerLocation = playerLocation,
            };
        }

        private static DialogueScript MakeDialogueScript() => new DialogueScript(new List<DialogueNode>
        {
            new DialogueNode(new DialogueLine("A", "첫"), new[] { new DialogueOption(1) }),
            new DialogueNode(new DialogueLine("B", "끝"), Array.Empty<DialogueOption>()),
        });

        private static ClueInfo MakeClueInfo(string id) =>
            new ClueInfo(
                new ClueId(id), new MemoryRoomId("room-1"),
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Joy, 1) }));

        private static Ampoule MakeAmpoule(string id) =>
            new Ampoule(
                new AmpouleId(id), new MemoryRoomId("room-1"),
                new Scent(EmotionType.Joy, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 1) })));

        [Test]
        public void Begin하면_정신력_인벤토리_보관함_복원상태_분석진행도_단서보관대가_초기화된다()
        {
            var fixture = MakeFixture();
            fixture.MentalityGauge.Consume(50);
            fixture.Inventory.TryStore(MakeClueInfo("clue-1"));
            fixture.Storage.TryStore(MakeAmpoule("ampoule-1"));
            fixture.RestorationTracker.ReportJudgement(
                new MemoryRoomId("room-1"),
                new ScentJudgementResult(isBaseEmotionCorrect: true, stage: FeedbackStage.PianoAndViolinAndDrum, accuracy: 1.0));
            fixture.AnalysisProgress.RecordDepth(new ClueId("clue-x"), AnalysisDepth.Advanced);
            fixture.ClueStorage.TryStore(MakeClueInfo("clue-2"));

            fixture.Session.Begin(new CommissionId("commission-2"));

            Assert.AreEqual(100, fixture.MentalityGauge.CurrentValue);
            Assert.AreEqual(0, fixture.Inventory.Items.Count);
            Assert.AreEqual(0, fixture.Storage.Ampoules.Count);
            Assert.IsFalse(fixture.RestorationTracker.IsRestored(new MemoryRoomId("room-1")));
            Assert.IsFalse(fixture.AnalysisProgress.TryGetBestDepth(new ClueId("clue-x"), out _));
            Assert.AreEqual(0, fixture.ClueStorage.Clues.Count);
        }

        [Test]
        public void 기록지는_초기화되지_않고_의뢰별로_분리된다()
        {
            var fixture = MakeFixture();
            var firstCommission = new CommissionId("commission-1");
            var secondCommission = new CommissionId("commission-2");

            // CommissionSession.Begin() 자체는 더 이상 대화 진행도를 건드리지
            // 않는다(그 책임은 GameSession.LoadCommission이 Begin() 이후
            // dialogueProgressor.LoadScript()를 호출하는 것으로 옮겨졌다).
            // 이 테스트는 그 실제 호출 순서를 그대로 재현해, 대화 대본을 로드해야
            // 나오는 첫 줄 기록이 의뢰별로 잘 분리되는지 확인한다.
            fixture.Session.Begin(firstCommission);
            fixture.DialogueProgressor.LoadScript(MakeDialogueScript());
            var afterFirstBegin = fixture.Journal.GetDialogue(firstCommission).Count;

            fixture.Session.Begin(secondCommission);
            fixture.DialogueProgressor.LoadScript(MakeDialogueScript());

            var firstCommissionRecords = fixture.Journal.GetDialogue(firstCommission);
            var secondCommissionRecords = fixture.Journal.GetDialogue(secondCommission);

            Assert.AreEqual(1, afterFirstBegin);
            Assert.AreEqual(1, firstCommissionRecords.Count);
            Assert.AreEqual(1, secondCommissionRecords.Count);
        }

        [Test]
        public void 대화가_끝나지_않았으면_기억으로_들어갈_수_없다()
        {
            var fixture = MakeFixture();
            fixture.Session.Begin(new CommissionId("commission-1"));

            var succeeded = fixture.Session.TryAdvanceToMemory();

            Assert.IsFalse(succeeded);
            Assert.AreEqual(CommissionStage.PreConversation, fixture.Session.Stage);
        }

        [Test]
        public void 대화_중에는_현실로_복귀할_수_없다()
        {
            var fixture = MakeFixture();
            fixture.Session.Begin(new CommissionId("commission-1"));

            var succeeded = fixture.Session.TryReturnToReality();

            Assert.IsFalse(succeeded);
            Assert.AreEqual(CommissionStage.PreConversation, fixture.Session.Stage);
        }

        [Test]
        public void 대화가_끝나면_기억으로_들어갈_수_있고_위치가_시작_지점이_된다()
        {
            var fixture = MakeFixture();
            fixture.Session.Begin(new CommissionId("commission-1"));
            fixture.DialogueProgressor.Advance(0);

            var succeeded = fixture.Session.TryAdvanceToMemory();

            Assert.IsTrue(succeeded);
            Assert.AreEqual(CommissionStage.InMemory, fixture.Session.Stage);
            Assert.AreEqual(EntryNode, fixture.PlayerLocation.Current);
        }

        [Test]
        public void 기억_진입_지점이_아닌_곳에서는_현실로_복귀할_수_없다()
        {
            var fixture = MakeFixture();
            fixture.Session.Begin(new CommissionId("commission-1"));
            fixture.DialogueProgressor.Advance(0);
            fixture.Session.TryAdvanceToMemory();
            fixture.PlayerLocation.MoveTo(OtherNode);

            var succeeded = fixture.Session.TryReturnToReality();

            Assert.IsFalse(succeeded);
            Assert.AreEqual(CommissionStage.InMemory, fixture.Session.Stage);
        }

        [Test]
        public void 현실로_복귀한_뒤에는_다시_기억으로_들어갈_수_없다()
        {
            var fixture = MakeFixture();
            fixture.Session.Begin(new CommissionId("commission-1"));
            fixture.DialogueProgressor.Advance(0);
            fixture.Session.TryAdvanceToMemory();
            fixture.Session.TryReturnToReality();

            var retrySucceeded = fixture.Session.TryAdvanceToMemory();

            Assert.IsFalse(retrySucceeded);
            Assert.AreEqual(CommissionStage.ReturnedToReality, fixture.Session.Stage);
        }

        [Test]
        public void 기억_어디에_있든_이탈_지점_복귀를_요청하면_계단으로_즉시_돌아간다()
        {
            var fixture = MakeFixture();
            fixture.Session.Begin(new CommissionId("commission-1"));
            fixture.DialogueProgressor.Advance(0);
            fixture.Session.TryAdvanceToMemory();
            fixture.PlayerLocation.MoveTo(OtherNode);

            var succeeded = fixture.Session.TryReturnToEntryPoint();

            Assert.IsTrue(succeeded);
            Assert.AreEqual(EntryNode, fixture.PlayerLocation.Current);
            // 단계는 바뀌지 않는다 — 위치만 옮길 뿐, 실제 이탈 확정은 여전히
            // TryReturnToReality()가 별도로 한다.
            Assert.AreEqual(CommissionStage.InMemory, fixture.Session.Stage);
        }

        [Test]
        public void 이탈_지점_복귀는_인접_여부와_무관하게_비용_없이_이동_완료_이벤트를_발행한다()
        {
            var fixture = MakeFixture();
            fixture.Session.Begin(new CommissionId("commission-1"));
            fixture.DialogueProgressor.Advance(0);
            fixture.Session.TryAdvanceToMemory();
            fixture.PlayerLocation.MoveTo(OtherNode);
            var mentalityBefore = fixture.MentalityGauge.CurrentValue;

            MemoryRoomMoveCompletedEvent? received = null;
            using (fixture.EventBus.Subscribe<MemoryRoomMoveCompletedEvent>(e => received = e))
            {
                fixture.Session.TryReturnToEntryPoint();
            }

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(OtherNode, received.Value.PreviousPosition);
            Assert.AreEqual(EntryNode, received.Value.NewPosition);
            Assert.AreEqual(mentalityBefore, fixture.MentalityGauge.CurrentValue);
        }

        [Test]
        public void 이탈_지점_복귀에_성공하면_이탈_요청_이벤트가_발행된다()
        {
            // FlowOverlayController가 "계단 위에 있다"는 사실만으로 복귀 확인
            // 화면을 띄우지 않고 이 이벤트로만 띄우는 이유는, 기억 진입 직후
            // 시작 위치가 우연히 계단인 경우(TryAdvanceToMemory)까지 이탈
            // 의사로 오인하지 않기 위해서다 — 그래서 이 이벤트가 실제로
            // 발행되는지가 그 구분의 핵심이다.
            var fixture = MakeFixture();
            fixture.Session.Begin(new CommissionId("commission-1"));
            fixture.DialogueProgressor.Advance(0);
            fixture.Session.TryAdvanceToMemory();
            fixture.PlayerLocation.MoveTo(OtherNode);

            var receivedCount = 0;
            using (fixture.EventBus.Subscribe<MemoryExitRequestedEvent>(_ => receivedCount++))
            {
                fixture.Session.TryReturnToEntryPoint();
            }

            Assert.AreEqual(1, receivedCount);
        }

        [Test]
        public void 기억_진입_직후_시작_위치가_계단이어도_이탈_요청_이벤트는_발행되지_않는다()
        {
            var fixture = MakeFixture();
            fixture.Session.Begin(new CommissionId("commission-1"));
            fixture.DialogueProgressor.Advance(0);

            var receivedCount = 0;
            using (fixture.EventBus.Subscribe<MemoryExitRequestedEvent>(_ => receivedCount++))
            {
                fixture.Session.TryAdvanceToMemory();
            }

            Assert.AreEqual(EntryNode, fixture.PlayerLocation.Current);
            Assert.AreEqual(0, receivedCount);
        }

        [Test]
        public void 이미_계단_위여도_다시_요청하면_이탈_요청_이벤트가_발행된다()
        {
            var fixture = MakeFixture();
            fixture.Session.Begin(new CommissionId("commission-1"));
            fixture.DialogueProgressor.Advance(0);
            fixture.Session.TryAdvanceToMemory();

            var receivedCount = 0;
            using (fixture.EventBus.Subscribe<MemoryExitRequestedEvent>(_ => receivedCount++))
            {
                fixture.Session.TryReturnToEntryPoint();
            }

            Assert.AreEqual(1, receivedCount);
        }

        [Test]
        public void 기억_안이_아니면_이탈_지점_복귀를_할_수_없다()
        {
            var fixture = MakeFixture();
            fixture.Session.Begin(new CommissionId("commission-1"));

            var succeeded = fixture.Session.TryReturnToEntryPoint();

            Assert.IsFalse(succeeded);
        }

        [Test]
        public void 이탈_지점으로_돌아온_뒤_복귀를_확정해도_재진입은_여전히_막힌다()
        {
            var fixture = MakeFixture();
            fixture.Session.Begin(new CommissionId("commission-1"));
            fixture.DialogueProgressor.Advance(0);
            fixture.Session.TryAdvanceToMemory();
            fixture.PlayerLocation.MoveTo(OtherNode);

            fixture.Session.TryReturnToEntryPoint();
            var returned = fixture.Session.TryReturnToReality();
            var retrySucceeded = fixture.Session.TryAdvanceToMemory();

            Assert.IsTrue(returned);
            Assert.IsFalse(retrySucceeded);
            Assert.AreEqual(CommissionStage.ReturnedToReality, fixture.Session.Stage);
        }

        [Test]
        public void 단계가_바뀌면_이벤트가_발행된다()
        {
            var fixture = MakeFixture();
            fixture.Session.Begin(new CommissionId("commission-1"));
            fixture.DialogueProgressor.Advance(0);

            CommissionStageChangedEvent? received = null;
            using (fixture.EventBus.Subscribe<CommissionStageChangedEvent>(e => received = e))
            {
                fixture.Session.TryAdvanceToMemory();
            }

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(CommissionStage.PreConversation, received.Value.PreviousStage);
            Assert.AreEqual(CommissionStage.InMemory, received.Value.NewStage);
        }
    }
}
