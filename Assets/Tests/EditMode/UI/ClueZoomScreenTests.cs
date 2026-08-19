using GameName.Core.Clues;
using GameName.Core.Emotions;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;
using GameName.UI.ClueZoom;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // 확대 화면의 규칙 두 가지를 화면 없이 검증한다.
    //   · 인벤토리 칸 개수는 Core 설정값을 따른다(화면에 4가 박혀 있지 않다).
    //   · 담기에 실패하면 단서가 제자리로 돌아가고 사유가 뜬다.
    public class ClueZoomScreenTests
    {
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");
        private static readonly ClueId ClueId1 = new ClueId("clue-1");

        // UXML과 같은 이름의 요소들만 손으로 세운다 — 스타일은 검증 대상이
        // 아니므로 필요 없다.
        private static VisualElement MakeZoomRoot()
        {
            var root = new VisualElement();

            var stage = new VisualElement { name = "clue-zoom-stage" };
            stage.Add(new Button { name = "clue-zoom-exit-button" });

            var clue = new VisualElement { name = "clue-zoom-clue" };
            clue.Add(new VisualElement { name = "clue-zoom-clue-swatches" });
            clue.Add(new Label { name = "clue-zoom-clue-kind" });
            clue.Add(new Label { name = "clue-zoom-clue-summary" });
            stage.Add(clue);

            stage.Add(new Label { name = "clue-zoom-message" });
            root.Add(stage);

            var sidebar = new VisualElement { name = "clue-zoom-sidebar" };
            sidebar.Add(new VisualElement { name = "clue-zoom-sidebar-header" });
            sidebar.Add(new VisualElement { name = "clue-zoom-inventory-grid" });
            root.Add(sidebar);

            return root;
        }

        private static ClueDefinition MakeDefinition(string id, ClueKind kind = ClueKind.Poster) =>
            new ClueDefinition(
                new ClueId(id), kind, new CluePositionRatio(0.5f),
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }),
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));

        private static (ClueZoomScreenController Controller, VisualElement Root, PlayerInventory Inventory)
            MakeFixture(int capacity, params string[] extraClueIdsInRoom)
        {
            var placements = new System.Collections.Generic.List<CluePlacement>
            {
                new CluePlacement(Room1, MakeDefinition(ClueId1.Value)),
            };
            foreach (var id in extraClueIdsInRoom)
                placements.Add(new CluePlacement(Room1, MakeDefinition(id, ClueKind.FloorObject)));

            var tracker = new MemoryRoomClueTracker(placements);
            var inventory = new PlayerInventory(new InventorySettings(capacity), new SharedSlotInventoryPolicy());
            var location = new PlayerLocation(MemoryGraphNodeId.OfRoom(Room1));
            var collector = new ClueCollector(location, inventory, tracker);

            var root = MakeZoomRoot();
            var view = new ClueZoomScreenView(root);
            var controller = new ClueZoomScreenController(view, inventory, collector);

            tracker.TryGetPlacedClue(ClueId1, out var definition, out _);
            controller.Open(definition.ToInfo());

            return (controller, root, inventory);
        }

        [Test]
        public void 인벤토리_칸_개수는_Core_설정값을_따른다()
        {
            var grid = MakeFixture(capacity: 4).Root.Q<VisualElement>("clue-zoom-inventory-grid");

            Assert.AreEqual(4, grid.childCount);
        }

        [Test]
        public void 용량이_바뀌면_그린_칸_개수도_함께_바뀐다()
        {
            // 4를 전제로 그리는 것이 아니라 설정값을 읽어 그린다는 증거다 —
            // 업그레이드로 칸이 늘어나는 상황이 이미 규칙에 있다.
            var grid = MakeFixture(capacity: 6).Root.Q<VisualElement>("clue-zoom-inventory-grid");

            Assert.AreEqual(6, grid.childCount);
        }

        [Test]
        public void 인벤토리가_가득_차면_제자리로_돌아가고_사유가_뜬다()
        {
            // 칸이 하나뿐인 인벤토리를 다른 단서로 먼저 채워 둔다.
            var (controller, root, inventory) = MakeFixture(capacity: 1, "clue-filler");
            inventory.TryStore(MakeDefinition("clue-filler", ClueKind.FloorObject).ToInfo());

            controller.TryStoreOpenClue();

            var message = root.Q<Label>("clue-zoom-message");
            Assert.AreEqual("인벤토리에 자리가 없습니다.", message.text);
            Assert.AreEqual(DisplayStyle.Flex, message.style.display.value);

            // 단서는 옮겨지지 않았고(칸은 그대로 하나), 화면에서도 제자리다.
            Assert.AreEqual(1, inventory.Items.Count);

            var clueElement = root.Q<VisualElement>("clue-zoom-clue");
            Assert.IsFalse(clueElement.ClassListContains("clue-zoom-clue--dragging"));
        }

        [Test]
        public void 담기에_성공하면_인벤토리에_들어가고_화면을_닫아_달라고_알린다()
        {
            var (controller, root, inventory) = MakeFixture(capacity: 4);

            var closeRequested = false;
            var stored = false;
            controller.CloseRequested += () => closeRequested = true;
            controller.ClueStored += () => stored = true;

            controller.TryStoreOpenClue();

            Assert.AreEqual(1, inventory.Items.Count);
            Assert.IsTrue(stored);
            Assert.IsTrue(closeRequested);
            Assert.AreEqual(string.Empty, root.Q<Label>("clue-zoom-message").text);
        }
    }
}
