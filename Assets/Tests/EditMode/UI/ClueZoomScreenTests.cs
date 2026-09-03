using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using GameName.Core.Trust;
using GameName.UI.ClueZoom;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // 단서 설명 창의 규칙:
    //   · [수집]을 누르면 Core 수집 처리기를 거쳐 습득하고 화면을 닫아 달라고 알린다.
    //   · 신뢰도가 낮아 손이 닿지 않으면 습득하지 않고 사유가 뜬다.
    //   · 인벤토리 칸은 이 화면에서 그리지 않는다(습득 시 가방이 열리지 않는다).
    public class ClueZoomScreenTests
    {
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");

        private static readonly Dictionary<int, float> VisibilityTable = new Dictionary<int, float>
        {
            { 3, 1.0f }, { 2, 0.75f }, { 1, 0.5f }, { 0, 0.5f },
        };

        private static VisualElement MakeZoomRoot()
        {
            var root = new VisualElement();

            var stage = new VisualElement { name = "clue-zoom-stage" };
            stage.Add(new Button { name = "clue-zoom-exit-button" });
            stage.Add(new Button { name = "clue-zoom-collect-button" });

            var card = new VisualElement { name = "clue-zoom-card" };
            card.Add(new Label { name = "clue-zoom-clue-name" });
            card.Add(new Label { name = "clue-zoom-clue-kind" });
            card.Add(new Label { name = "clue-zoom-message" });
            stage.Add(card);

            root.Add(stage);
            return root;
        }

        private static ClueDefinition MakeDefinition(string id, float position = 0.5f) =>
            new ClueDefinition(new ClueId(id), ClueKind.Poster, id, new CluePositionRatio(position), MemoryColor.Red);

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly ClueZoomScreenController Controller;
            public readonly VisualElement Root;
            public readonly PlayerInventory Inventory;
            public readonly TrustGauge Trust;

            public Fixture(string clueId, float cluePosition)
            {
                var def = MakeDefinition(clueId, cluePosition);
                var placements = new List<CluePlacement> { new CluePlacement(Room1, def) };
                var tracker = new MemoryRoomClueTracker(placements);
                var room = new RoomDefinition(Room1, new[] { def }, null, Array.Empty<DialogueLineDefinition>());
                var clueState = new ClueStateStore(new[] { room }, Bus);

                Inventory = new PlayerInventory(new InventorySettings(4), new SharedSlotInventoryPolicy());
                Trust = new TrustGauge(3, Bus);

                var collector = new ClueCollectionProcessor(
                    clueState, new CenteredClueAccessPolicy(), Trust,
                    new StepVisibilityPolicy(VisibilityTable), tracker, Inventory, Bus);
                _ = new InventoryProjection(Inventory, tracker, Bus);

                Bus.Publish(new RoomStartedEvent(Room1, 0));

                Root = MakeZoomRoot();
                Controller = new ClueZoomScreenController(new ClueZoomScreenView(Root), collector);
                Controller.Open(def.ToInfo());
            }

            public Label Message => Root.Q<Label>("clue-zoom-message");
        }

        [Test]
        public void 수집을_누르면_인벤토리에_들어가고_화면을_닫아_달라고_알린다()
        {
            var fx = new Fixture("clue-1", 0.5f);

            var closeRequested = false;
            var stored = false;
            fx.Controller.CloseRequested += () => closeRequested = true;
            fx.Controller.ClueStored += () => stored = true;

            fx.Controller.TryCollectOpenClue();

            Assert.AreEqual(1, fx.Inventory.Items.Count);
            Assert.IsTrue(stored);
            Assert.IsTrue(closeRequested);
            Assert.AreEqual(string.Empty, fx.Message.text);
        }

        [Test]
        public void 신뢰도가_낮아_손이_닿지_않으면_습득하지_않고_사유가_뜬다()
        {
            // 방 끝쪽(0.1)에 놓인 단서. 신뢰 1이면 창은 [0.25, 0.75]라 닿지 않는다.
            var fx = new Fixture("clue-1", 0.1f);
            fx.Trust.Decrease(2); // 3 → 1

            fx.Controller.TryCollectOpenClue();

            Assert.AreEqual(0, fx.Inventory.Items.Count);
            StringAssert.Contains("손이 닿지", fx.Message.text);
        }
    }
}
