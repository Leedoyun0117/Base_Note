using GameName.Core.Clues;
using GameName.UI.MemoryRoom.Space;
using NUnit.Framework;
using UnityEngine;

namespace GameName.UI.Tests.EditMode
{
    // 버린 단서를 어디에 내려놓는지 검증한다.
    //
    // 예전에는 이 계산이 아예 없었다 — 플레이어가 서 있던 자리를 그대로 썼다.
    // 그래서 바닥 물건은 플레이어 몸에 완전히 가려 "버렸는데 방에 나타나지
    // 않는" 것처럼 보였고, 이미 단서가 있는 자리에 그대로 얹혀 "두 개가 겹쳐
    // 보이는" 상태가 되었다. 두 증상 모두 여기서 막는다.
    public class DroppedCluePlacementTests
    {
        private static readonly CluePositionRatio[] Nothing = new CluePositionRatio[0];

        private static MemoryRoomLayout MakeLayout() =>
            new MemoryRoomLayout(
                roomHeight: 4f,
                roomLength: 7f,
                wallThickness: 0.2f,
                playerHeight: 1f,
                playerWidth: 0.4f,
                playerMoveSpeed: 3f,
                edgeMargin: 0.6f,
                posterMountHeight: 2.4f,
                posterSize: 0.8f,
                floorObjectSize: 0.5f,
                exitMarkerWidth: 0.5f,
                exitMarkerHeight: 1.6f,
                cameraVerticalMargin: 0.6f);

        [Test]
        public void 버린_단서는_플레이어_몸에_가려지지_않는_자리에_놓인다()
        {
            var layout = MakeLayout();
            const float playerX = 0f;

            var placed = DroppedCluePlacement.Resolve(layout, ClueKind.FloorObject, playerX, Nothing);

            var distance = Mathf.Abs(CluePlacementLayout.XOf(layout, placed) - playerX);
            var halfWidths = (layout.PlayerWidth + layout.FloorObjectSize) / 2f;

            Assert.GreaterOrEqual(
                distance, halfWidths - 0.0001f,
                "버린 단서가 플레이어와 겹치는 자리에 놓였다 — 화면에서는 몸에 가려 보이지 않는다.");
        }

        [Test]
        public void 이미_단서가_있는_자리에는_겹쳐_놓지_않는다()
        {
            var layout = MakeLayout();
            const float playerX = 0f;

            // 플레이어 바로 옆, 즉 원래대로라면 내려놓을 그 자리를 먼저 채운다.
            var wanted = DroppedCluePlacement.Resolve(layout, ClueKind.FloorObject, playerX, Nothing);
            var placed = DroppedCluePlacement.Resolve(
                layout, ClueKind.FloorObject, playerX, new[] { wanted });

            var separation = CluePlacementLayout.MinimumSeparationRatio(layout, ClueKind.FloorObject);

            Assert.GreaterOrEqual(
                placed.DistanceTo(wanted), separation - 0.0001f,
                "이미 단서가 있는 자리에 그대로 얹혔다.");
        }

        [Test]
        public void 자리가_여러_개_찼어도_빈자리를_찾아낸다()
        {
            var layout = MakeLayout();
            var separation = CluePlacementLayout.MinimumSeparationRatio(layout, ClueKind.FloorObject);

            var occupied = new[]
            {
                new CluePositionRatio(0.5f),
                new CluePositionRatio(0.5f + separation),
                new CluePositionRatio(0.5f - separation),
            };

            var placed = DroppedCluePlacement.Resolve(layout, ClueKind.FloorObject, 0f, occupied);

            foreach (var taken in occupied)
            {
                Assert.GreaterOrEqual(
                    placed.DistanceTo(taken), separation - 0.0001f,
                    $"이미 찬 자리({taken})와 겹쳤다.");
            }
        }

        // 오른쪽 벽에 붙어 서서 버리면 오른쪽으로는 놓을 곳이 없다. 벽을 뚫고
        // 나가는 대신 반대편에 놓아야 한다.
        [Test]
        public void 방_끝에서_버려도_방_안에_놓인다()
        {
            var layout = MakeLayout();

            var placed = DroppedCluePlacement.Resolve(
                layout, ClueKind.FloorObject, RoomGeometry.MaxInteriorX(layout), Nothing);

            var x = CluePlacementLayout.XOf(layout, placed);

            Assert.GreaterOrEqual(x, RoomGeometry.MinInteriorX(layout) - 0.0001f);
            Assert.LessOrEqual(x, RoomGeometry.MaxInteriorX(layout) + 0.0001f);
        }

        // 종류가 다르면 포스터는 벽, 물건은 바닥이라 가로가 같아도 겹치지 않는다.
        // 그래서 컨트롤러는 같은 종류의 자리만 넘기고, 이 계산도 넘어온 목록만
        // 본다 — "창문 아래 떨어진 반지" 같은 배치를 막지 않기 위해서다.
        [Test]
        public void 비교_대상이_없으면_원하는_자리를_그대로_쓴다()
        {
            var layout = MakeLayout();
            const float playerX = 1.2f;

            var first = DroppedCluePlacement.Resolve(layout, ClueKind.FloorObject, playerX, Nothing);
            var second = DroppedCluePlacement.Resolve(layout, ClueKind.FloorObject, playerX, Nothing);

            Assert.AreEqual(first, second, "같은 상황에서 자리가 달라지면 배치를 예측할 수 없다.");
        }
    }
}
