using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameName.Core.Ampoules;
using GameName.Core.Analysis;
using GameName.Core.Clues;
using GameName.Core.Emotions;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Journal;
using GameName.Core.Judging;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class JournalTests
    {
        private static PlayerJournal MakeJournal(out EventBus eventBus, out IAmpouleStorage storage, out IPlayerInventory inventory)
        {
            eventBus = new EventBus(new NoOpEventExceptionHandler());
            storage = new AmpouleStorage(new AmpouleStorageSettings(5));
            inventory = new PlayerInventory(new InventorySettings(5), new SharedSlotInventoryPolicy());
            return new PlayerJournal(eventBus, storage, inventory);
        }

        private static Scent MakeScent() =>
            new Scent(EmotionType.Joy, new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Love, 6),
                new EmotionBlendEntry(EmotionType.Anger, 4),
            }));

        [Test]
        public void 서로_다른_의뢰의_기록은_섞이지_않는다()
        {
            var journal = MakeJournal(out _, out _, out _);
            var commissionA = new CommissionId("commission-a");
            var commissionB = new CommissionId("commission-b");

            journal.BeginCommission(commissionA);
            journal.RecordDialogue(new DialogueLine("피해자", "그날 밤 무슨 일이 있었나요"));

            journal.BeginCommission(commissionB);
            journal.RecordDialogue(new DialogueLine("의뢰인", "다른 사건입니다"));

            var recordsA = journal.GetDialogue(commissionA);
            var recordsB = journal.GetDialogue(commissionB);

            Assert.AreEqual(1, recordsA.Count);
            Assert.AreEqual("그날 밤 무슨 일이 있었나요", recordsA[0].Text);
            Assert.AreEqual(1, recordsB.Count);
            Assert.AreEqual("다른 사건입니다", recordsB[0].Text);
        }

        [Test]
        public void 화면이_받아간_기록_목록을_바꿔도_원본은_그대로다()
        {
            var journal = MakeJournal(out _, out _, out _);
            var commission = new CommissionId("commission-a");
            journal.BeginCommission(commission);
            journal.RecordDialogue(new DialogueLine("A", "첫 줄"));

            var records = journal.GetDialogue(commission);

            // 반환된 목록이 우연히 List<T>로 캐스팅되더라도, 그건 매 조회마다
            // 새로 만든 방어적 복사본이다 — 여기서 손을 대도 내부 저장소에는
            // 영향이 없다.
            if (records is List<DialogueLine> mutableList)
                mutableList.Add(new DialogueLine("몰래", "추가된 줄"));

            var recordsAgain = journal.GetDialogue(commission);
            Assert.AreEqual(1, recordsAgain.Count);
            Assert.AreEqual("첫 줄", recordsAgain[0].Text);
        }

        [Test]
        public void 앰플_기록은_보관함_인벤토리_소모_세_상태를_구분한다()
        {
            var journal = MakeJournal(out var eventBus, out var storage, out var inventory);
            var commission = new CommissionId("commission-a");
            journal.BeginCommission(commission);

            var room = new MemoryRoomId("room-1");
            var scent = MakeScent();

            var stayedInStorage = new Ampoule(new AmpouleId("a-1"), room, scent);
            var movedToInventory = new Ampoule(new AmpouleId("a-2"), room, scent);
            var consumed = new Ampoule(new AmpouleId("a-3"), room, scent);

            storage.TryStore(stayedInStorage);
            storage.TryStore(movedToInventory);
            storage.TryRemove(movedToInventory);
            inventory.TryStore(movedToInventory);
            // consumed는 보관함/인벤토리 어디에도 남기지 않는다 — 시향으로 사라진
            // 실제 상황을 흉내낸다.

            var recipes = new List<AmpouleRecipe>
            {
                new AmpouleRecipe(stayedInStorage.Id, room, scent),
                new AmpouleRecipe(movedToInventory.Id, room, scent),
                new AmpouleRecipe(consumed.Id, room, scent),
            };
            eventBus.Publish(new AmpouleCraftedEvent(recipes));

            var judgement = new ScentJudgementResult(isBaseEmotionCorrect: true, stage: FeedbackStage.Piano, accuracy: 0.5);
            eventBus.Publish(new ScentJudgedEvent(consumed.Id, room, scent, judgement));

            var recordsById = new Dictionary<AmpouleId, AmpouleRecord>();
            foreach (var record in journal.GetAmpoules(commission))
                recordsById[record.AmpouleId] = record;

            Assert.AreEqual(AmpouleRecordState.InStorage, recordsById[stayedInStorage.Id].State);
            Assert.IsNull(recordsById[stayedInStorage.Id].TestResult);

            Assert.AreEqual(AmpouleRecordState.InInventory, recordsById[movedToInventory.Id].State);
            Assert.IsNull(recordsById[movedToInventory.Id].TestResult);

            Assert.AreEqual(AmpouleRecordState.Consumed, recordsById[consumed.Id].State);
            Assert.IsNotNull(recordsById[consumed.Id].TestResult);
            Assert.AreEqual(FeedbackStage.Piano, recordsById[consumed.Id].TestResult.Result.Stage);
        }

        [Test]
        public void 분석_이벤트_하나는_기록_하나만_남긴다()
        {
            var journal = MakeJournal(out var eventBus, out _, out _);
            var commission = new CommissionId("commission-a");
            journal.BeginCommission(commission);

            var clueId = new ClueId("clue-1");
            var result = new EmotionAnalysisResult(
                AnalysisDepth.Basic, new[] { new DetectedEmotion(EmotionType.Love, null) });

            eventBus.Publish(new ClueAnalyzedEvent(clueId, result));

            Assert.AreEqual(1, journal.GetAnalyses(commission).Count);
        }

        // "기록 경로를 이벤트 하나로 통일한다"는 설계 결정을, 값 검사가 아니라
        // 타입 수준에서 증명한다 — 세 처리기 모두 생성자에 IJournal을 받을 자리
        // 자체가 없으므로, 구현이 실수로라도 이벤트 발행과 별개로 직접 기록을
        // 남기는 두 번째 경로를 만들 수 없다.
        [Test]
        public void 처리기들은_더_이상_IJournal을_직접_의존하지_않는다()
        {
            AssertNoJournalDependency(typeof(ClueAnalyzer));
            AssertNoJournalDependency(typeof(AmpouleCraftingProcessor));
            AssertNoJournalDependency(typeof(ScentTestingProcessor));
        }

        private static void AssertNoJournalDependency(Type processorType)
        {
            var parameterTypes = processorType
                .GetConstructors()
                .Single()
                .GetParameters()
                .Select(p => p.ParameterType)
                .ToArray();

            CollectionAssert.DoesNotContain(parameterTypes, typeof(IJournal));
        }

        // 이 프로젝트에서 이미 세 번 어긴 규칙: 기록 타입에 정답/단서 진실 타입이
        // 섞여 들어가면 안 된다. 런타임 값이 아니라 타입 자체를 리플렉션으로
        // 검사해, 나중에 필드가 실수로 추가되어도 이 테스트가 즉시 잡아낸다.
        [Test]
        public void 기록_타입에는_정답이나_단서_진실_타입이_없다()
        {
            var recordTypes = new[]
            {
                typeof(DialogueLine),
                typeof(AnalysisRecord),
                typeof(AmpouleRecord),
                typeof(ScentTestRecord),
            };
            var forbiddenTypes = new[] { typeof(MemoryRoomAnswer), typeof(ClueDefinition) };

            foreach (var recordType in recordTypes)
            {
                var propertyTypes = recordType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => p.PropertyType)
                    .ToArray();

                foreach (var forbidden in forbiddenTypes)
                {
                    CollectionAssert.DoesNotContain(
                        propertyTypes, forbidden, $"{recordType.Name}에 {forbidden.Name}이 노출되어 있다.");
                }
            }
        }

        [Test]
        public void 기록_순서는_삽입_순서와_항상_같다()
        {
            var journal = MakeJournal(out _, out _, out _);
            var commission = new CommissionId("commission-a");
            journal.BeginCommission(commission);

            journal.RecordDialogue(new DialogueLine("A", "첫"));
            journal.RecordDialogue(new DialogueLine("B", "둘"));
            journal.RecordDialogue(new DialogueLine("C", "셋"));

            var expected = new[] { "첫", "둘", "셋" };
            var firstRead = journal.GetDialogue(commission).Select(l => l.Text).ToArray();
            var secondRead = journal.GetDialogue(commission).Select(l => l.Text).ToArray();

            CollectionAssert.AreEqual(expected, firstRead);
            CollectionAssert.AreEqual(expected, secondRead);
        }

        // EventBus는 구독자 예외를 밖으로 다시 던지지 않고 IEventExceptionHandler로
        // 넘기므로(EventBus.cs 참고), Assert.Throws로 직접 잡을 수 없다 — 대신
        // 예외를 가로채 기록해 두는 전용 핸들러로 실제로 던져졌는지 확인한다.
        private sealed class CapturingEventExceptionHandler : IEventExceptionHandler
        {
            public Exception LastException;
            public void Handle(Type eventType, Exception exception) => LastException = exception;
        }

        [Test]
        public void 제작_기록이_없는_앰플의_시향_결과는_연결할_수_없어_예외가_발생한다()
        {
            var exceptionHandler = new CapturingEventExceptionHandler();
            var eventBus = new EventBus(exceptionHandler);
            var storage = new AmpouleStorage(new AmpouleStorageSettings(5));
            var inventory = new PlayerInventory(new InventorySettings(5), new SharedSlotInventoryPolicy());
            var journal = new PlayerJournal(eventBus, storage, inventory);
            journal.BeginCommission(new CommissionId("commission-a"));

            var room = new MemoryRoomId("room-1");
            var scent = MakeScent();
            var judgement = new ScentJudgementResult(isBaseEmotionCorrect: true, stage: FeedbackStage.Piano, accuracy: 0.5);

            eventBus.Publish(new ScentJudgedEvent(new AmpouleId("unknown"), room, scent, judgement));

            Assert.IsInstanceOf<InvalidOperationException>(exceptionHandler.LastException);
        }

        [Test]
        public void 활성_의뢰가_없으면_기록이_조용히_무시된다()
        {
            var journal = MakeJournal(out _, out _, out _);

            Assert.IsNull(journal.ActiveCommissionId);
            Assert.DoesNotThrow(() => journal.RecordDialogue(new DialogueLine("A", "누구도 듣지 않는 말")));
        }
    }
}
