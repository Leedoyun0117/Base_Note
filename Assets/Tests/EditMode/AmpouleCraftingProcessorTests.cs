using System.Linq;
using GameName.Core.Ampoules;
using GameName.Core.Emotions;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using GameName.Core.Validation;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class AmpouleCraftingProcessorTests
    {
        // 결정적인 식별자를 내어주는 테스트 전용 생성기. 실제 조향 처리기는
        // 기본적으로 GuidAmpouleIdGenerator를 쓰지만, 테스트에서는 예측 가능한
        // 값이 필요하다.
        private sealed class SequentialAmpouleIdGenerator : IAmpouleIdGenerator
        {
            private int _next = 1;
            public AmpouleId Generate() => new AmpouleId($"ampoule-{_next++}");
        }

        private sealed class CraftingFixture
        {
            public AmpouleCraftingProcessor Processor;
            public IMentalityGauge Gauge;
            public IAmpouleStorage Storage;
            public IPlayerInventory Inventory;
        }

        private static readonly MemoryGraphNodeId PerfumeryRoom = new MemoryGraphNodeId("perfumery-room");
        private static readonly MemoryGraphNodeId OtherRoom = new MemoryGraphNodeId("other-room");
        private static readonly MemoryRoomId TargetRoom = new MemoryRoomId("room-1");

        // 목표 방의 실제 요구 총량(15)을 등록해 두는 저장소. 조향 처리기는
        // 요청이 아니라 이 저장소에서 총량을 조회해야 한다.
        private static MemoryRoomAnswerRepository MakeAnswerRepository()
        {
            var blend = new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Love, 10),
                new EmotionBlendEntry(EmotionType.Anger, 5),
            });

            var answer = new MemoryRoomAnswer(TargetRoom, new Scent(EmotionType.Sadness, blend));
            return new MemoryRoomAnswerRepository(new[] { answer });
        }

        private static CraftingFixture MakeFixture(
            MemoryGraphNodeId playerPosition,
            int initialMentality = 100,
            int maxStoredAmpoules = 3,
            int inventoryCapacity = 10,
            IAmpouleIdGenerator idGenerator = null)
        {
            var settings = new MentalityCostSettings(
                initialMentality: initialMentality, maxMentality: 100,
                memoryRoomMoveCost: 1, basicAnalysisCost: 20, advancedAnalysisCost: 30,
                ampouleCraftingCost: 8, memoryRoomFullRestorationRecovery: 20);
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var gauge = new MentalityGauge(settings, eventBus);
            var policy = new EmotionCompositionPolicy(
                minSupportingEmotionCount: 2, maxSupportingEmotionCount: 4, allowSupportingEmotionSameAsBase: false);
            var validator = new ScentCompositionValidator(policy);
            var storage = new AmpouleStorage(new AmpouleStorageSettings(maxStoredAmpoules));
            var inventory = new PlayerInventory(new InventorySettings(inventoryCapacity), new SharedSlotInventoryPolicy());
            var location = new PlayerLocation(playerPosition);

            var processor = new AmpouleCraftingProcessor(
                location,
                PerfumeryRoom,
                gauge,
                settings,
                validator,
                MakeAnswerRepository(),
                storage,
                idGenerator ?? new SequentialAmpouleIdGenerator(),
                eventBus);

            return new CraftingFixture { Processor = processor, Gauge = gauge, Storage = storage, Inventory = inventory };
        }

        private static Scent MakeValidScent() =>
            new Scent(EmotionType.Joy, new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Love, 10),
                new EmotionBlendEntry(EmotionType.Anger, 5),
            }));

        private static AmpouleCraftingRequest MakeValidRequest() =>
            new AmpouleCraftingRequest(TargetRoom, MakeValidScent());

        [Test]
        public void 조향_요청에는_요구_총량을_임의로_넣을_방법이_없다()
        {
            var parameters = typeof(AmpouleCraftingRequest).GetConstructors().Single().GetParameters();

            Assert.AreEqual(2, parameters.Length);
            Assert.IsFalse(parameters.Any(p => p.ParameterType == typeof(int)));
        }

        [Test]
        public void 조향실이_아닌_곳에서는_앰플을_만들_수_없다()
        {
            var fixture = MakeFixture(OtherRoom);

            var result = fixture.Processor.Craft(new[] { MakeValidRequest() });

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(AmpouleCraftingFailureReason.NotInPerfumeryRoom, result.FailureReason);
        }

        [Test]
        public void 제작한_앰플이_인벤토리가_아니라_보관함에_들어간다()
        {
            var fixture = MakeFixture(PerfumeryRoom);

            var result = fixture.Processor.Craft(new[] { MakeValidRequest() });

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1, fixture.Storage.Ampoules.Count);
            Assert.AreEqual(0, fixture.Inventory.Items.Count);
        }

        [Test]
        public void 인벤토리가_가득_차_있어도_앰플_3개를_만들_수_있다()
        {
            var fixture = MakeFixture(PerfumeryRoom, inventoryCapacity: 0);
            var requests = new[] { MakeValidRequest(), MakeValidRequest(), MakeValidRequest() };

            var result = fixture.Processor.Craft(requests);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(3, fixture.Storage.Ampoules.Count);
        }

        [Test]
        public void 앰플_3개를_한_번에_만들어도_정신력이_8만_소모된다()
        {
            var fixture = MakeFixture(PerfumeryRoom);
            var requests = new[] { MakeValidRequest(), MakeValidRequest(), MakeValidRequest() };

            var result = fixture.Processor.Craft(requests);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(3, result.CraftedAmpoules.Count);
            Assert.AreEqual(100 - 8, fixture.Gauge.CurrentValue);
        }

        [Test]
        public void 결정적_식별자_생성기를_주입하면_앰플_식별자가_예측_가능하다()
        {
            var fixture = MakeFixture(PerfumeryRoom, idGenerator: new SequentialAmpouleIdGenerator());
            var requests = new[] { MakeValidRequest(), MakeValidRequest() };

            var result = fixture.Processor.Craft(requests);

            Assert.AreEqual(new AmpouleId("ampoule-1"), result.CraftedAmpoules[0].Id);
            Assert.AreEqual(new AmpouleId("ampoule-2"), result.CraftedAmpoules[1].Id);
        }

        [Test]
        public void 정신력이_부족하면_앰플이_하나도_만들어지지_않는다()
        {
            var fixture = MakeFixture(PerfumeryRoom, initialMentality: 5);

            var result = fixture.Processor.Craft(new[] { MakeValidRequest() });

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(AmpouleCraftingFailureReason.InsufficientMentality, result.FailureReason);
            Assert.AreEqual(5, fixture.Gauge.CurrentValue);
            Assert.AreEqual(0, fixture.Storage.Ampoules.Count);
        }

        [Test]
        public void 유효하지_않은_배합이_하나라도_섞이면_전체가_실패한다()
        {
            var fixture = MakeFixture(PerfumeryRoom);

            // 보조 감정 1종 뿐이라 정책의 최소 개수(2)를 위반한다.
            var invalidScent = new Scent(EmotionType.Joy, new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Love, 15),
            }));
            var invalidRequest = new AmpouleCraftingRequest(TargetRoom, invalidScent);

            var result = fixture.Processor.Craft(new[] { MakeValidRequest(), invalidRequest });

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(AmpouleCraftingFailureReason.InvalidComposition, result.FailureReason);
            Assert.AreEqual(100, fixture.Gauge.CurrentValue);
            Assert.AreEqual(0, fixture.Storage.Ampoules.Count);
        }

        [Test]
        public void 등록된_요구_총량과_다르면_유효성_검사에서_실패한다()
        {
            // 저장소에 등록된 목표 방의 실제 요구 총량은 15인데, 요청은 20짜리
            // 배합을 들고 온다 — 호출부가 총량을 조작할 방법이 없으므로 이
            // 배합은 저장소의 진짜 총량과 비교되어 그대로 걸러진다.
            var fixture = MakeFixture(PerfumeryRoom);
            var mismatchedScent = new Scent(EmotionType.Joy, new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Love, 12),
                new EmotionBlendEntry(EmotionType.Anger, 8),
            }));
            var request = new AmpouleCraftingRequest(TargetRoom, mismatchedScent);

            var result = fixture.Processor.Craft(new[] { request });

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(AmpouleCraftingFailureReason.InvalidComposition, result.FailureReason);
        }

        [Test]
        public void 보관_상한을_넘기는_제작은_실패하고_이미_담긴_것도_되돌아간다()
        {
            // 사전 개수 검사가 없으므로, 2개까지는 실제로 담겼다가 3번째에서
            // 실패해 롤백되는 경로를 그대로 탄다.
            var fixture = MakeFixture(PerfumeryRoom, maxStoredAmpoules: 2);

            var result = fixture.Processor.Craft(new[] { MakeValidRequest(), MakeValidRequest(), MakeValidRequest() });

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(AmpouleCraftingFailureReason.StorageFull, result.FailureReason);
            Assert.AreEqual(100, fixture.Gauge.CurrentValue);
            Assert.AreEqual(0, fixture.Storage.Ampoules.Count);
        }
    }
}
