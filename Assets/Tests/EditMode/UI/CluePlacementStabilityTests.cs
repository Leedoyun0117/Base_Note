using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;
using GameName.UI.MemoryRoom.Space;
using NUnit.Framework;
using UnityEngine;

namespace GameName.UI.Tests.EditMode
{
    // 이번 변경의 핵심 회귀 테스트: 방에 놓인 단서는 스스로 움직이지 않는다.
    //
    // 예전에는 남은 단서 개수로 자리를 균등 분배했기 때문에, 단서 하나를 집는
    // 순간 나머지가 옆으로 미끄러졌다. 계산식만 보고는 그 증상을 알 수 없어서
    // (식 자체는 옳았다) 실제로 컨트롤러와 뷰를 세워 오브젝트 좌표를 재는
    // 방식으로 확인한다.
    public class CluePlacementStabilityTests
    {
        private static readonly MemoryRoomId Room = new MemoryRoomId("room-1");
        private static readonly MemoryGraphNodeId RoomNode = MemoryGraphNodeId.OfRoom(Room);
        private static readonly MemoryGraphNodeId StaircaseNode = new MemoryGraphNodeId("staircase");

        private sealed class Fixture : IDisposable
        {
            public MemoryRoomSpaceView View;
            public MemoryRoomSpaceController Controller;
            public ClueCollector Collector;
            public ClueDropProcessor DropProcessor;
            public MemoryRoomClueTracker Tracker;
            public PlayerLocation PlayerLocation;
            public MemoryRoomMovementProcessor MovementProcessor;
            public PlayerInventory Inventory;
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
            new ClueDefinition(
                new ClueId(id), kind, new CluePositionRatio(position));

        // 방 하나와 계단만 있는 최소 구성. 단서는 저작 자리를 서로 다르게 준다.
        private static Fixture MakeFixture(float roomLength = 7f)
        {
            var layout = MakeLayout(roomLength);
            var eventBus = new EventBus(new NoOpEventExceptionHandler());

            var graph = new MemoryRoomGraph(
                new[]
                {
                    new MemoryGraphNode(RoomNode, MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(0, 1)),
                    new MemoryGraphNode(StaircaseNode, MemoryGraphNodeType.Staircase, new MemoryGraphCoordinate(0, 0)),
                },
                new[] { new OpenConnection(StaircaseNode, RoomNode) },
                Array.Empty<LadderConnection>());

            var tracker = new MemoryRoomClueTracker(new[]
            {
                new CluePlacement(Room, MakeClue("clue-left", 0.2f)),
                new CluePlacement(Room, MakeClue("clue-middle", 0.5f)),
                new CluePlacement(Room, MakeClue("clue-right", 0.9f)),
            });

            var playerLocation = new PlayerLocation(RoomNode);
            var inventory = new PlayerInventory(new InventorySettings(5), new SharedSlotInventoryPolicy());
            var movementProcessor = new MemoryRoomMovementProcessor(graph, playerLocation, eventBus);

            var spaceObject = new GameObject("Space");
            var view = spaceObject.AddComponent<MemoryRoomSpaceView>();

            var controller = new MemoryRoomSpaceController(
                view, layout, playerLocation, graph, tracker, new[] { Room }, movementProcessor,
                new DroppedCluePositions(), eventBus);

            return new Fixture
            {
                View = view,
                Controller = controller,
                Collector = new ClueCollector(playerLocation, inventory, tracker),
                DropProcessor = new ClueDropProcessor(playerLocation, graph, inventory, tracker, eventBus),
                Tracker = tracker,
                PlayerLocation = playerLocation,
                MovementProcessor = movementProcessor,
                Inventory = inventory,
                Layout = layout,
                SpaceObject = spaceObject,
            };
        }

        // 씬 오브젝트는 식별자를 밖으로 내주지 않는다(그게 설계 경계다).
        // 테스트만 비공개 필드를 읽는다 — 검증하려고 런타임 표면을 넓히지 않기
        // 위해서다.
        private static ClueId ReadClueId(ClueSceneObject sceneObject) =>
            (ClueId)typeof(ClueSceneObject)
                .GetField("_clueId", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(sceneObject);

        private static Dictionary<string, Vector2> PlacedPositions(MemoryRoomSpaceView view) =>
            view.GetComponentsInChildren<ClueSceneObject>(includeInactive: true)
                .ToDictionary(
                    clue => ReadClueId(clue).Value,
                    clue => (Vector2)clue.transform.localPosition);

        private static PlayerCharacter PlayerOf(MemoryRoomSpaceView view) =>
            view.GetComponentInChildren<PlayerCharacter>(includeInactive: true);

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
                fixture.Controller.Refresh();

                var after = PlacedPositions(fixture.View);

                Assert.AreEqual(2, after.Count, "집은 단서는 방에서 사라져야 한다.");
                Assert.AreEqual(before["clue-left"], after["clue-left"], "옆 단서가 움직였다.");
                Assert.AreEqual(before["clue-right"], after["clue-right"], "옆 단서가 움직였다.");
            }
        }

        [Test]
        public void 단서를_버려도_남은_단서의_자리는_변하지_않는다()
        {
            using (var fixture = MakeFixture())
            {
                fixture.Collector.Collect(new ClueId("clue-middle"));
                fixture.Controller.Refresh();
                var before = PlacedPositions(fixture.View);

                var carried = fixture.Inventory.Items.OfType<ClueInfo>().First();
                Assert.IsTrue(fixture.DropProcessor.Drop(carried).Succeeded);

                var after = PlacedPositions(fixture.View);

                Assert.AreEqual(3, after.Count, "버린 단서가 방에 다시 나타나야 한다.");
                Assert.AreEqual(before["clue-left"], after["clue-left"], "옆 단서가 움직였다.");
                Assert.AreEqual(before["clue-right"], after["clue-right"], "옆 단서가 움직였다.");
            }
        }

        // 예전에는 이 테스트가 "플레이어가 서 있던 자리에 정확히 놓인다"를
        // 요구했다. 그 요구 자체가 증상이었다 — 플레이어 스프라이트는 단서보다
        // 앞에 그려지므로, 발밑에 놓인 바닥 물건은 몸에 완전히 가려 방에 아무것도
        // 나타나지 않은 것처럼 보인다. 그래서 "서 있던 자리"가 아니라 "서 있던
        // 자리 곁"이 옳은 요구다.
        [Test]
        public void 버린_단서는_플레이어_곁에_놓여_몸에_가려지지_않는다()
        {
            using (var fixture = MakeFixture())
            {
                fixture.Collector.Collect(new ClueId("clue-middle"));
                fixture.Controller.Refresh();

                // 저작 자리(0.5)와 확실히 다른 곳으로 걸어가서 버린다.
                var dropX = CluePlacementLayout.XOf(fixture.Layout, new CluePositionRatio(0.35f));
                PlayerOf(fixture.View).MoveTo(dropX);

                var carried = fixture.Inventory.Items.OfType<ClueInfo>().First();
                fixture.DropProcessor.Drop(carried);

                var placedX = PlacedPositions(fixture.View)["clue-middle"].x;

                Assert.AreEqual(
                    ExpectedDropX(fixture.Layout, dropX), placedX, 0.0001f,
                    "버린 단서가 플레이어 곁이 아닌 곳에 놓였다.");
                Assert.AreNotEqual(
                    CluePlacementLayout.XOf(fixture.Layout, new CluePositionRatio(0.5f)), placedX,
                    "버린 단서가 저작 자리로 되돌아갔다.");
            }
        }

        // 플레이어와 단서가 서로 딱 닿는 최소 거리만큼 옆으로 비켜 놓는다.
        // 기대값을 DroppedCluePlacement에게 다시 묻지 않고 여기서 직접 세우는
        // 이유는, 그러면 계산이 바뀌어도 테스트가 함께 따라가 버려 아무것도
        // 검증하지 못하기 때문이다.
        private static float ExpectedDropX(MemoryRoomLayout layout, float playerX) =>
            playerX + (layout.PlayerWidth + layout.FloorObjectSize) / 2f;

        // 버린 자리는 표시용 기록이지만 세션 하나만큼은 살아 있어야 한다 —
        // 방을 나갔다 돌아왔다고 제자리로 튀어 돌아가면 안 된다.
        [Test]
        public void 버린_자리는_방을_나갔다_와도_유지된다()
        {
            using (var fixture = MakeFixture())
            {
                fixture.Collector.Collect(new ClueId("clue-middle"));
                fixture.Controller.Refresh();

                var dropX = CluePlacementLayout.XOf(fixture.Layout, new CluePositionRatio(0.35f));
                PlayerOf(fixture.View).MoveTo(dropX);
                fixture.DropProcessor.Drop(fixture.Inventory.Items.OfType<ClueInfo>().First());

                fixture.MovementProcessor.Move(StaircaseNode);
                fixture.MovementProcessor.Move(RoomNode);

                Assert.AreEqual(
                    ExpectedDropX(fixture.Layout, dropX),
                    PlacedPositions(fixture.View)["clue-middle"].x, 0.0001f);
            }
        }
    }
}
