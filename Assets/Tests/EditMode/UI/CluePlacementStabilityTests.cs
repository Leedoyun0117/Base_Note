using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using GameName.Core.Trust;
using GameName.UI.MemoryRoom.Space;
using NUnit.Framework;
using UnityEngine;

namespace GameName.UI.Tests.EditMode
{
    // 방에 놓인 단서는 스스로 움직이지 않는다 — 저작 자리에 놓이고, 옆 단서를
    // 집어도 제자리를 지킨다.
    //
    // 예전에는 남은 단서 개수로 자리를 균등 분배했기 때문에, 단서 하나를 집는
    // 순간 나머지가 옆으로 미끄러졌다. 계산식만 보고는 그 증상을 알 수 없어서
    // 실제로 컨트롤러와 뷰를 세워 오브젝트 좌표를 재는 방식으로 확인한다.
    public class CluePlacementStabilityTests
    {
        private static readonly MemoryRoomId Room = new MemoryRoomId("room-1");

        private static readonly Dictionary<int, float> FullVisibility = new Dictionary<int, float>
        {
            { 3, 1.0f }, { 2, 1.0f }, { 1, 1.0f }, { 0, 1.0f },
        };

        private sealed class Fixture : IDisposable
        {
            public MemoryRoomSpaceView View;
            public MemoryRoomSpaceController Controller;
            public ClueCollectionProcessor Collector;
            public MemoryRoomLayout Layout;
            public GameObject SpaceObject;

            public void Dispose()
            {
                Controller?.Dispose();
                if (SpaceObject != null)
                    UnityEngine.Object.DestroyImmediate(SpaceObject);
            }
        }

        private static MemoryRoomLayout MakeLayout(float roomLength = 7f) =>
            new MemoryRoomLayout(
                roomHeight: 4f, roomLength: roomLength, wallThickness: 0.2f,
                playerHeight: 1f, playerWidth: 0.4f, playerMoveSpeed: 3f, edgeMargin: 0.6f,
                posterMountHeight: 2.4f, posterSize: 0.8f, floorObjectSize: 0.5f,
                exitMarkerWidth: 0.5f, exitMarkerHeight: 1.6f, cameraVerticalMargin: 0.6f);

        private static ClueDefinition MakeClue(string id, float position, ClueKind kind = ClueKind.FloorObject) =>
            new ClueDefinition(new ClueId(id), kind, id, new CluePositionRatio(position), MemoryColor.Red);

        private static Fixture MakeFixture(float roomLength = 7f)
        {
            var layout = MakeLayout(roomLength);
            var eventBus = new EventBus(new NoOpEventExceptionHandler());

            var clues = new[]
            {
                MakeClue("clue-left", 0.2f),
                MakeClue("clue-middle", 0.5f),
                MakeClue("clue-right", 0.9f),
            };
            var placements = clues.Select(c => new CluePlacement(Room, c)).ToArray();
            var tracker = new MemoryRoomClueTracker(placements);
            var room = new RoomDefinition(Room, clues, null, Array.Empty<DialogueLineDefinition>());
            var clueState = new ClueStateStore(new[] { room }, eventBus);

            var spaceObject = new GameObject("Space");
            var view = spaceObject.AddComponent<MemoryRoomSpaceView>();

            var accessPolicy = new CenteredClueAccessPolicy();
            var controller = new MemoryRoomSpaceController(
                view, layout, tracker, clueState, accessPolicy, Room, eventBus);

            var inventory = new PlayerInventory(new InventorySettings(16), new SharedSlotInventoryPolicy());
            var collector = new ClueCollectionProcessor(
                clueState, accessPolicy, new TrustGauge(3, eventBus),
                new StepVisibilityPolicy(FullVisibility), tracker, inventory, eventBus);

            // 방 진입 — ClueState 시드 + 활성 방 설정 + 컨트롤러 Refresh.
            eventBus.Publish(new RoomStartedEvent(Room, 0));

            return new Fixture
            {
                View = view,
                Controller = controller,
                Collector = collector,
                Layout = layout,
                SpaceObject = spaceObject,
            };
        }

        private static ClueId ReadClueId(ClueSceneObject sceneObject) =>
            (ClueId)typeof(ClueSceneObject)
                .GetField("_clueId", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(sceneObject);

        private static Dictionary<string, Vector2> PlacedPositions(MemoryRoomSpaceView view) =>
            view.GetComponentsInChildren<ClueSceneObject>(includeInactive: true)
                .ToDictionary(
                    clue => ReadClueId(clue).Value,
                    clue => (Vector2)clue.transform.localPosition);

        [Test]
        public void 단서는_저작_자리에_놓인다()
        {
            using (var fixture = MakeFixture())
            {
                var placed = PlacedPositions(fixture.View);

                Assert.AreEqual(
                    CluePlacementLayout.XOf(fixture.Layout, new CluePositionRatio(0.2f)),
                    placed["clue-left"].x, 0.0001f);
                Assert.AreEqual(
                    CluePlacementLayout.XOf(fixture.Layout, new CluePositionRatio(0.5f)),
                    placed["clue-middle"].x, 0.0001f);
                Assert.AreEqual(
                    CluePlacementLayout.XOf(fixture.Layout, new CluePositionRatio(0.9f)),
                    placed["clue-right"].x, 0.0001f);
            }
        }

        [Test]
        public void 단서를_집어도_남은_단서의_자리는_변하지_않는다()
        {
            using (var fixture = MakeFixture())
            {
                var before = PlacedPositions(fixture.View);

                Assert.IsTrue(fixture.Collector.Collect(new ClueId("clue-middle")).Succeeded);

                var after = PlacedPositions(fixture.View);

                Assert.AreEqual(2, after.Count, "집은 단서는 방에서 사라져야 한다.");
                Assert.AreEqual(before["clue-left"], after["clue-left"], "옆 단서가 움직였다.");
                Assert.AreEqual(before["clue-right"], after["clue-right"], "옆 단서가 움직였다.");
            }
        }
    }
}
