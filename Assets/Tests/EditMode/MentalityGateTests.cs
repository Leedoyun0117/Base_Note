using GameName.Core.Ampoules;
using GameName.Core.Analysis;
using GameName.Core.Clues;
using GameName.Core.Emotions;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Judging;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using GameName.Core.Validation;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 정신력 0일 때 무엇이 막히고 무엇이 막히지 않는지를 시스템 전체 관점에서
    // 확인한다: 분석과 조향은 막히지만(둘 다 IMentalityGauge.CanAct를 직접
    // 확인한다), 시향은 막히지 않는다 — ScentTestingProcessor는 애초에
    // IMentalityGauge를 받을 수단이 없다(ScentTestingProcessorTests의
    // "시향_처리기는_정신력_게이지를_받을_수단이_없다" 참고). 시향까지 막히면
    // 정신력이 바닥난 플레이어가 방을 복원해 정신력을 회복할 유일한 수단마저
    // 잃어 갇혀버리기 때문에, 이 예외는 설계상 의도된 것이다.
    public class MentalityGateTests
    {
        private static readonly MemoryGraphNodeId AnalysisRoom = new MemoryGraphNodeId("analysis-room");
        private static readonly MemoryGraphNodeId PerfumeryRoom = new MemoryGraphNodeId("perfumery-room");
        private static readonly MemoryRoomId TargetRoom = new MemoryRoomId("room-1");
        private static readonly MemoryGraphNodeId TargetRoomNode = MemoryGraphNodeId.OfRoom(TargetRoom);

        private static IMentalityCostSettings MakeZeroMentalitySettings() =>
            new MentalityCostSettings(
                initialMentality: 0, maxMentality: 100,
                memoryRoomMoveCost: 1, basicAnalysisCost: 20, advancedAnalysisCost: 30,
                ampouleCraftingCost: 8, memoryRoomFullRestorationRecovery: 20);

        [Test]
        public void 정신력이_0이면_단서_분석이_막힌다()
        {
            var settings = MakeZeroMentalitySettings();
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var gauge = new MentalityGauge(settings, eventBus);
            var location = new PlayerLocation(AnalysisRoom);
            var progress = new ClueAnalysisProgress();
            var inventory = new PlayerInventory(new InventorySettings(10), new SharedSlotInventoryPolicy());

            var clue = new ClueDefinition(
                new ClueId("clue-1"), TargetRoom,
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }),
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));
            var tracker = new MemoryRoomClueTracker(new[] { clue });
            inventory.TryStore(clue.ToInfo());

            var analyzer = new ClueAnalyzer(location, AnalysisRoom, gauge, settings, eventBus, progress, tracker, inventory);

            var result = analyzer.Analyze(clue.Id, AnalysisDepth.Basic);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ClueAnalysisFailureReason.InsufficientMentality, result.FailureReason);
        }

        [Test]
        public void 정신력이_0이면_조향이_막힌다()
        {
            var settings = MakeZeroMentalitySettings();
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var gauge = new MentalityGauge(settings, eventBus);
            var location = new PlayerLocation(PerfumeryRoom);

            var policy = new EmotionCompositionPolicy(
                minSupportingEmotionCount: 2, maxSupportingEmotionCount: 4, allowSupportingEmotionSameAsBase: false);
            var validator = new ScentCompositionValidator(policy);
            var storage = new AmpouleStorage(new AmpouleStorageSettings(3));

            var answerBlend = new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Love, 10),
                new EmotionBlendEntry(EmotionType.Anger, 5),
            });
            var answerRepository = new MemoryRoomAnswerRepository(new[]
            {
                new MemoryRoomAnswer(TargetRoom, new Scent(EmotionType.Sadness, answerBlend)),
            });

            var craftingProcessor = new AmpouleCraftingProcessor(
                location, PerfumeryRoom, gauge, settings, validator, answerRepository, storage,
                new GuidAmpouleIdGenerator(), eventBus);

            var validScent = new Scent(EmotionType.Joy, new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Love, 10),
                new EmotionBlendEntry(EmotionType.Anger, 5),
            }));
            var request = new AmpouleCraftingRequest(TargetRoom, validScent);

            var result = craftingProcessor.Craft(new[] { request });

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(AmpouleCraftingFailureReason.InsufficientMentality, result.FailureReason);
        }

        [Test]
        public void 정신력이_0이어도_시향은_막히지_않는다()
        {
            // 정신력 0이라는 상황을 실제로 관측 가능하게 만들기 위해 게이지를
            // 하나 만들어 0으로 둔다 — 하지만 이 게이지는 ScentTestingProcessor의
            // 생성자 어디에도 들어가지 않는다. 시향이 막히지 않는 이유가 "우연히
            // 이 테스트에서 안 막았기 때문"이 아니라 "애초에 막을 방법이 없기
            // 때문"임을 보여주기 위함이다.
            var settings = MakeZeroMentalitySettings();
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var gauge = new MentalityGauge(settings, eventBus);
            Assert.IsFalse(gauge.CanAct);

            var inventory = new PlayerInventory(new InventorySettings(10), new SharedSlotInventoryPolicy());
            var judge = new ScentJudge(new ScentJudgementSettings(0.8));
            var tracker = new MemoryRoomRestorationTracker(eventBus);
            var location = new PlayerLocation(TargetRoomNode);

            var answerBlend = new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Love, 10),
                new EmotionBlendEntry(EmotionType.Anger, 5),
            });
            var answerRepository = new MemoryRoomAnswerRepository(new[]
            {
                new MemoryRoomAnswer(TargetRoom, new Scent(EmotionType.Joy, answerBlend)),
            });

            var testingProcessor = new ScentTestingProcessor(location, inventory, judge, tracker, answerRepository, eventBus);

            var ampoule = new Ampoule(new AmpouleId("ampoule-1"), TargetRoom, new Scent(EmotionType.Joy, answerBlend));
            inventory.TryStore(ampoule);

            var result = testingProcessor.Test(ampoule);

            Assert.IsTrue(result.Succeeded);
        }
    }
}
