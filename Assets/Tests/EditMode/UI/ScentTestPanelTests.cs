using System;
using GameName.Core.Ampoules;
using GameName.Core.Emotions;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Judging;
using GameName.Core.MemoryRooms;
using GameName.UI.MemoryRoom;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // 시향 패널의 두 가지를 검증한다.
    //   · 결과 문구가 정해진 조건에서 사라지는가(예전에는 영영 남았다).
    //   · 시향이 실제로 가능할 때만 그렇다고 알리는가.
    public class ScentTestPanelTests
    {
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");
        private static readonly MemoryRoomId Room2 = new MemoryRoomId("room-2");
        private static readonly MemoryGraphNodeId Room1Node = MemoryGraphNodeId.OfRoom(Room1);
        private static readonly MemoryGraphNodeId Room2Node = MemoryGraphNodeId.OfRoom(Room2);

        // 시간 기반 소멸은 패널에 붙어야 도는 스케줄러의 몫이라 여기서 재지
        // 않는다. 이 테스트가 보는 것은 시간과 무관하게 확정적으로 사라져야
        // 하는 조건들이다.
        private const float ResultSeconds = 4f;

        private sealed class Fixture
        {
            public ScentTestingPanelController Controller;
            public ScentTestingPanelView View;
            public VisualElement Root;
            public PlayerLocation PlayerLocation;
            public EventBus EventBus;
            public PlayerInventory Inventory;
        }

        private static VisualElement MakeScentRoot()
        {
            var root = new VisualElement { name = "scent-test-panel" };
            root.Add(new Button { name = "scent-test-close-button" });
            root.Add(new ScrollView { name = "scent-test-list" });
            root.Add(new VisualElement { name = "scent-test-result" });
            return root;
        }

        private static Scent MakeScent() =>
            new Scent(EmotionType.Joy, new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Love, 5),
                new EmotionBlendEntry(EmotionType.Fear, 5),
            }));

        private static Ampoule MakeAmpoule(string id, MemoryRoomId targetRoom) =>
            new Ampoule(new AmpouleId(id), targetRoom, MakeScent());

        private static Fixture MakeFixture(params Ampoule[] carried)
        {
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var playerLocation = new PlayerLocation(Room1Node);
            var inventory = new PlayerInventory(new InventorySettings(5), new SharedSlotInventoryPolicy());
            foreach (var ampoule in carried)
                inventory.TryStore(ampoule);

            var answers = new MemoryRoomAnswerRepository(new[]
            {
                new MemoryRoomAnswer(Room1, MakeScent()),
                new MemoryRoomAnswer(Room2, MakeScent()),
            });
            var restorationTracker = new MemoryRoomRestorationTracker(eventBus);
            var judge = new ScentJudge(new ScentJudgementSettings(highAccuracyThreshold: 0.8));

            var processor = new ScentTestingProcessor(
                playerLocation, inventory, judge, restorationTracker, answers, eventBus);

            var root = MakeScentRoot();
            var view = new ScentTestingPanelView(root, ResultSeconds);
            var controller = new ScentTestingPanelController(
                view, playerLocation, inventory, processor, restorationTracker,
                new[] { Room1, Room2 }, eventBus);

            return new Fixture
            {
                Controller = controller,
                View = view,
                Root = root,
                PlayerLocation = playerLocation,
                EventBus = eventBus,
                Inventory = inventory,
            };
        }

        private static bool HasResult(Fixture fixture) =>
            fixture.Root.Q<VisualElement>("scent-test-result").childCount > 0;

        [Test]
        public void 방을_옮기면_시향_결과가_사라진다()
        {
            var fixture = MakeFixture(MakeAmpoule("ampoule-1", Room1));
            fixture.View.SetResult(FeedbackStage.PianoAndViolinAndDrum, isRestored: true);
            Assert.IsTrue(HasResult(fixture), "결과가 표시되지 않았다.");

            fixture.PlayerLocation.MoveTo(Room2Node);
            fixture.EventBus.Publish(new MemoryRoomMoveCompletedEvent(Room1Node, Room2Node));

            Assert.IsFalse(HasResult(fixture), "방을 옮겼는데 지난 방의 시향 결과가 남아 있다.");
        }

        [Test]
        public void 시향_창을_닫으면_결과가_사라진다()
        {
            var fixture = MakeFixture(MakeAmpoule("ampoule-1", Room1));
            fixture.Controller.Open();
            fixture.View.SetResult(FeedbackStage.Piano, isRestored: false);
            Assert.IsTrue(HasResult(fixture));

            fixture.Controller.Close();

            Assert.IsFalse(HasResult(fixture), "닫았다가 다시 열면 지난 결과가 남아 있다.");
        }

        [Test]
        public void 다시_시향하면_이전_결과가_대체된다()
        {
            var fixture = MakeFixture(MakeAmpoule("ampoule-1", Room1));
            fixture.View.SetResult(FeedbackStage.PianoAndViolinAndDrum, isRestored: true);
            var beforeCount = fixture.Root.Q<VisualElement>("scent-test-result").childCount;

            fixture.View.SetResult(FeedbackStage.Piano, isRestored: false);
            var afterCount = fixture.Root.Q<VisualElement>("scent-test-result").childCount;

            // 복원 배너가 사라지고 결과 한 줄만 남아야 한다 — 덧붙지 않는다.
            Assert.AreEqual(2, beforeCount);
            Assert.AreEqual(1, afterCount);
        }

        [Test]
        public void 시향_화면은_처음에_닫혀_있다()
        {
            var fixture = MakeFixture();

            Assert.IsFalse(fixture.Controller.IsOpen, "항상 떠 있으면 플레이 공간을 가린다.");
        }

        [Test]
        public void 이_방에서_쓸_수_있는_앰플이_없으면_시향_가능으로_알리지_않는다()
        {
            var fixture = MakeFixture(MakeAmpoule("ampoule-other-room", Room2));

            Assert.IsFalse(fixture.Controller.IsTestPossible);
        }

        [Test]
        public void 이_방_앰플을_들고_있으면_시향_가능으로_알린다()
        {
            var fixture = MakeFixture(MakeAmpoule("ampoule-1", Room1));

            Assert.IsTrue(fixture.Controller.IsTestPossible);
        }

        [Test]
        public void 방을_옮겨_쓸_수_없게_되면_시향_불가로_바뀐다()
        {
            var fixture = MakeFixture(MakeAmpoule("ampoule-1", Room1));
            Assert.IsTrue(fixture.Controller.IsTestPossible);

            var notified = new System.Collections.Generic.List<bool>();
            fixture.Controller.AvailabilityChanged += notified.Add;

            fixture.PlayerLocation.MoveTo(Room2Node);
            fixture.EventBus.Publish(new MemoryRoomMoveCompletedEvent(Room1Node, Room2Node));

            Assert.IsFalse(fixture.Controller.IsTestPossible);
            CollectionAssert.AreEqual(new[] { false }, notified, "상태가 바뀔 때만 알려야 한다.");
        }
    }
}
