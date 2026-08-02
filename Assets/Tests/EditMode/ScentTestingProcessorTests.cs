using System;
using System.Linq;
using System.Reflection;
using GameName.Core.Ampoules;
using GameName.Core.Emotions;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Judging;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class ScentTestingProcessorTests
    {
        private sealed class TestingFixture
        {
            public ScentTestingProcessor Processor;
            public IPlayerInventory Inventory;
            public IMemoryRoomRestorationTracker RestorationTracker;
            public EventBus EventBus;
        }

        private static readonly MemoryRoomId TargetRoom = new MemoryRoomId("room-1");
        private static readonly MemoryGraphNodeId TargetRoomNode = MemoryGraphNodeId.OfRoom(TargetRoom);
        private static readonly MemoryGraphNodeId OtherRoomNode = new MemoryGraphNodeId("other-room");

        private static MemoryRoomAnswer MakeAnswer() =>
            new MemoryRoomAnswer(TargetRoom, new Scent(EmotionType.Joy, new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Love, 10),
                new EmotionBlendEntry(EmotionType.Anger, 5),
            })));

        private static TestingFixture MakeFixture(
            MemoryGraphNodeId playerPosition, IMemoryRoomAnswerRepository answerRepository = null)
        {
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var inventory = new PlayerInventory(new InventorySettings(10), new SharedSlotInventoryPolicy());
            var judge = new ScentJudge(new ScentJudgementSettings(0.8));
            var tracker = new MemoryRoomRestorationTracker(eventBus);
            var location = new PlayerLocation(playerPosition);
            var repository = answerRepository ?? new MemoryRoomAnswerRepository(new[] { MakeAnswer() });

            var processor = new ScentTestingProcessor(location, inventory, judge, tracker, repository, eventBus);

            return new TestingFixture
            {
                Processor = processor,
                Inventory = inventory,
                RestorationTracker = tracker,
                EventBus = eventBus,
            };
        }

        [Test]
        public void 시향_처리기의_공개_메서드_인자에는_정답_타입이_없다()
        {
            var publicMethodParameterTypes = typeof(ScentTestingProcessor)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .SelectMany(m => m.GetParameters())
                .Select(p => p.ParameterType)
                .ToArray();

            CollectionAssert.DoesNotContain(publicMethodParameterTypes, typeof(MemoryRoomAnswer));
        }

        [Test]
        public void 목표_방이_아닌_곳에서는_시향할_수_없다()
        {
            var fixture = MakeFixture(OtherRoomNode);
            var ampoule = new Ampoule(new AmpouleId("ampoule-1"), TargetRoom, MakeAnswer().CorrectScent);
            fixture.Inventory.TryStore(ampoule);

            var result = fixture.Processor.Test(ampoule);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ScentTestFailureReason.WrongRoom, result.FailureReason);
            Assert.AreEqual(1, fixture.Inventory.Items.Count);
        }

        [Test]
        public void 인벤토리에_없는_앰플은_시향할_수_없다()
        {
            var fixture = MakeFixture(TargetRoomNode);
            var ampoule = new Ampoule(new AmpouleId("ampoule-1"), TargetRoom, MakeAnswer().CorrectScent);
            // 인벤토리에 담지 않는다.

            var result = fixture.Processor.Test(ampoule);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ScentTestFailureReason.AmpouleNotInInventory, result.FailureReason);
        }

        [Test]
        public void 보관함에만_있고_인벤토리에는_없는_앰플은_시향할_수_없다()
        {
            // 조향실 보관함과 인벤토리는 서로 다른 장소다 — 보관함에 있다는
            // 사실만으로는 시향 가능 여부에 아무 영향을 주지 않는다.
            var fixture = MakeFixture(TargetRoomNode);
            var ampoule = new Ampoule(new AmpouleId("ampoule-1"), TargetRoom, MakeAnswer().CorrectScent);
            var storage = new AmpouleStorage(new AmpouleStorageSettings(3));
            storage.TryStore(ampoule);

            var result = fixture.Processor.Test(ampoule);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ScentTestFailureReason.AmpouleNotInInventory, result.FailureReason);
            Assert.AreEqual(1, storage.Ampoules.Count);
        }

        [Test]
        public void 정답이_등록되지_않은_방을_시향하면_예외가_발생한다()
        {
            // 목표 방을 가리키는 앰플은 있지만, 저장소에는 그 방의 정답이
            // 등록되어 있지 않은 데이터 불일치 상황을 재현한다.
            var emptyRepository = new MemoryRoomAnswerRepository(Array.Empty<MemoryRoomAnswer>());
            var fixture = MakeFixture(TargetRoomNode, emptyRepository);
            var ampoule = new Ampoule(new AmpouleId("ampoule-1"), TargetRoom, MakeAnswer().CorrectScent);
            fixture.Inventory.TryStore(ampoule);

            Assert.Throws<InvalidOperationException>(() => fixture.Processor.Test(ampoule));
        }

        [Test]
        public void 시향_후_앰플은_판정_결과와_무관하게_인벤토리에서_사라진다()
        {
            var fixture = MakeFixture(TargetRoomNode);
            var wrongScent = new Scent(EmotionType.Fear, new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Love, 1),
            }));
            var ampoule = new Ampoule(new AmpouleId("ampoule-1"), TargetRoom, wrongScent);
            fixture.Inventory.TryStore(ampoule);

            ScentJudgedEvent? received = null;
            using (fixture.EventBus.Subscribe<ScentJudgedEvent>(e => received = e))
            {
                var result = fixture.Processor.Test(ampoule);

                Assert.IsTrue(result.Succeeded);
                Assert.AreEqual(FeedbackStage.Silence, result.Judgement.Stage);
                Assert.AreEqual(0, fixture.Inventory.Items.Count);
            }

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(ampoule.Id, received.Value.AmpouleId);
        }

        [Test]
        public void 최상위_단계_판정이면_복원_트래커에_보고된다()
        {
            var fixture = MakeFixture(TargetRoomNode);
            var ampoule = new Ampoule(new AmpouleId("ampoule-1"), TargetRoom, MakeAnswer().CorrectScent);
            fixture.Inventory.TryStore(ampoule);

            var result = fixture.Processor.Test(ampoule);

            Assert.AreEqual(FeedbackStage.PianoAndViolinAndDrum, result.Judgement.Stage);
            Assert.IsTrue(fixture.RestorationTracker.IsRestored(TargetRoom));
        }

        [Test]
        public void 이미_복원된_방을_다시_시향해도_막히지_않고_앰플만_소모된다()
        {
            var fixture = MakeFixture(TargetRoomNode);
            var correctScent = MakeAnswer().CorrectScent;
            var firstAmpoule = new Ampoule(new AmpouleId("ampoule-1"), TargetRoom, correctScent);
            var secondAmpoule = new Ampoule(new AmpouleId("ampoule-2"), TargetRoom, correctScent);
            fixture.Inventory.TryStore(firstAmpoule);
            fixture.Inventory.TryStore(secondAmpoule);

            fixture.Processor.Test(firstAmpoule);
            Assert.IsTrue(fixture.RestorationTracker.IsRestored(TargetRoom));

            var secondResult = fixture.Processor.Test(secondAmpoule);

            Assert.IsTrue(secondResult.Succeeded);
            Assert.AreEqual(FeedbackStage.PianoAndViolinAndDrum, secondResult.Judgement.Stage);
            Assert.AreEqual(0, fixture.Inventory.Items.Count);
        }

        // "시향 자체는 정신력을 소모하지 않는다"는 규칙을 런타임 값 검사가 아니라
        // 타입 수준에서 증명한다 — 이 처리기의 생성자에는 애초에 IMentalityGauge를
        // 넘길 자리가 없으므로, 구현이 실수로라도 정신력을 건드릴 방법이 없다.
        [Test]
        public void 시향_처리기는_정신력_게이지를_받을_수단이_없다()
        {
            var constructorParameterTypes = typeof(ScentTestingProcessor)
                .GetConstructors()
                .Single()
                .GetParameters()
                .Select(p => p.ParameterType)
                .ToArray();

            CollectionAssert.DoesNotContain(constructorParameterTypes, typeof(IMentalityGauge));
        }
    }
}
