using GameName.Core.Clues;
using GameName.UI.MemoryRoom.Space;
using NUnit.Framework;
using UnityEngine;

namespace GameName.UI.Tests.EditMode
{
    // 방 공간의 좌표 규칙을 씬 없이 검증한다. 이 계산들을 MonoBehaviour에서
    // 떼어 낸 이유가 바로 이것이다 — "포스터가 벽에 붙는가", "물건이 바닥에
    // 놓이는가", "플레이어가 벽을 뚫는가"를 확인하려고 매번 씬을 띄울 수는 없다.
    public class MemoryRoomSpaceLayoutTests
    {
        // 기획이 정한 규격: 플레이어 키 1, 방 높이 4, 길이 7.
        private static MemoryRoomLayout MakeLayout(float roomLength = 7f) =>
            new MemoryRoomLayout(
                roomHeight: 4f,
                roomLength: roomLength,
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

        // 안쪽 구간에서 이 자리가 몇 퍼센트 지점인지. 방 길이가 달라져도
        // 같은 비율이 같은 상대 위치를 가리키는지 보려고 쓴다.
        private static float RelativePositionOf(MemoryRoomLayout layout, float x)
        {
            var min = RoomGeometry.MinInteriorX(layout);
            var max = RoomGeometry.MaxInteriorX(layout);
            return (x - min) / (max - min);
        }

        [Test]
        public void 포스터는_바닥에서_떨어진_벽_높이에_놓인다()
        {
            var layout = MakeLayout();

            var position = CluePlacementLayout.PositionAt(
                layout, ClueKind.Poster, new CluePositionRatio(0.5f));

            Assert.AreEqual(RoomGeometry.FloorTopY(layout) + layout.PosterMountHeight, position.y, 0.0001f);

            // 벽에 붙었다는 것은 곧 플레이어 키보다 훨씬 위라는 뜻이다 —
            // 바닥 물건과 확실히 구분되어야 한다.
            Assert.Greater(position.y, layout.PlayerHeight);
        }

        [Test]
        public void 바닥_물건은_바닥에_얹혀_놓인다()
        {
            var layout = MakeLayout();

            var position = CluePlacementLayout.PositionAt(
                layout, ClueKind.FloorObject, new CluePositionRatio(0.5f));

            // 물건의 아래쪽 면이 정확히 바닥 윗면에 닿아야 한다.
            var bottom = position.y - layout.FloorObjectSize / 2f;
            Assert.AreEqual(RoomGeometry.FloorTopY(layout), bottom, 0.0001f);
        }

        // 세로는 저작 데이터가 아니라 종류가 정한다 — 같은 자리를 줘도 종류가
        // 다르면 높이가 갈려야 한다.
        [Test]
        public void 같은_자리라도_높이는_종류가_결정한다()
        {
            var layout = MakeLayout();
            var samePosition = new CluePositionRatio(0.42f);

            var poster = CluePlacementLayout.PositionAt(layout, ClueKind.Poster, samePosition);
            var floorObject = CluePlacementLayout.PositionAt(layout, ClueKind.FloorObject, samePosition);

            Assert.AreEqual(poster.x, floorObject.x, 0.0001f, "가로는 저작 데이터가 정하므로 같아야 한다.");
            Assert.Greater(poster.y, floorObject.y, "세로는 종류가 정하므로 갈려야 한다.");
        }

        [Test]
        public void 저작_자리가_그대로_가로_위치가_된다()
        {
            var layout = MakeLayout();

            var left = CluePlacementLayout.XOf(layout, new CluePositionRatio(0f));
            var middle = CluePlacementLayout.XOf(layout, new CluePositionRatio(0.5f));
            var right = CluePlacementLayout.XOf(layout, new CluePositionRatio(1f));

            Assert.AreEqual(RoomGeometry.MinInteriorX(layout), left, 0.0001f);
            Assert.AreEqual(0f, middle, 0.0001f);
            Assert.AreEqual(RoomGeometry.MaxInteriorX(layout), right, 0.0001f);
        }

        // 비율로 두는 이유가 바로 이것이다 — 방 길이를 바꿔도 "방의 30% 지점"이
        // 그대로 유지되어야 한다.
        [Test]
        public void 방_길이를_바꿔도_비율_배치가_유지된다()
        {
            var narrow = MakeLayout(roomLength: 7f);
            var wide = MakeLayout(roomLength: 20f);
            var position = new CluePositionRatio(0.3f);

            var narrowX = CluePlacementLayout.XOf(narrow, position);
            var wideX = CluePlacementLayout.XOf(wide, position);

            Assert.AreNotEqual(narrowX, wideX, "방이 넓어지면 실제 좌표는 달라져야 한다.");
            Assert.AreEqual(
                RelativePositionOf(narrow, narrowX), RelativePositionOf(wide, wideX), 0.0001f,
                "방 길이가 달라져도 같은 비율은 같은 상대 위치를 가리켜야 한다.");
        }

        [Test]
        public void 저작_자리는_벽을_뚫지_않는다()
        {
            var layout = MakeLayout();

            // 비율 0과 1은 방의 양 끝이지만, 벽에 반쯤 파묻히지 않도록 여백
            // 안쪽 구간에 대응된다.
            var left = CluePlacementLayout.XOf(layout, new CluePositionRatio(0f));
            var right = CluePlacementLayout.XOf(layout, new CluePositionRatio(1f));

            Assert.Greater(left, RoomGeometry.LeftInnerX(layout));
            Assert.Less(right, RoomGeometry.RightInnerX(layout));
        }

        // 버린 자리를 기억할 때 좌표를 비율로 되돌린다 — 그 왕복이 어긋나면
        // 버린 단서가 조금씩 밀린다.
        [Test]
        public void 좌표와_비율_변환은_서로_왕복한다()
        {
            var layout = MakeLayout();
            var original = new CluePositionRatio(0.37f);

            var roundTripped = CluePlacementLayout.RatioOf(layout, CluePlacementLayout.XOf(layout, original));

            Assert.AreEqual(original.Value, roundTripped.Value, 0.0001f);
        }

        [Test]
        public void 방_바깥_좌표는_비율_범위_안으로_눌러_넣는다()
        {
            var layout = MakeLayout();

            Assert.AreEqual(CluePositionRatio.Maximum, CluePlacementLayout.RatioOf(layout, 100f).Value, 0.0001f);
            Assert.AreEqual(CluePositionRatio.Minimum, CluePlacementLayout.RatioOf(layout, -100f).Value, 0.0001f);
        }

        [Test]
        public void 플레이어는_방_밖으로_걸어_나갈_수_없다()
        {
            var layout = MakeLayout();

            // 아주 긴 시간 동안 오른쪽으로 걸어도 오른쪽 한계에서 멈춘다.
            var x = PlayerMovement.NextX(layout, currentX: 0f, direction: 1f, deltaTime: 100f);
            Assert.AreEqual(RoomGeometry.MaxInteriorX(layout), x, 0.0001f);

            x = PlayerMovement.NextX(layout, currentX: 0f, direction: -1f, deltaTime: 100f);
            Assert.AreEqual(RoomGeometry.MinInteriorX(layout), x, 0.0001f);
        }

        [Test]
        public void 좌우를_동시에_누르면_움직이지_않는다()
        {
            Assert.AreEqual(0f, PlayerMovement.DirectionOf(leftHeld: true, rightHeld: true));
            Assert.AreEqual(0f, PlayerMovement.DirectionOf(leftHeld: false, rightHeld: false));
            Assert.AreEqual(-1f, PlayerMovement.DirectionOf(leftHeld: true, rightHeld: false));
            Assert.AreEqual(1f, PlayerMovement.DirectionOf(leftHeld: false, rightHeld: true));
        }

        [Test]
        public void 플레이어는_발이_바닥에_닿는다()
        {
            var layout = MakeLayout();

            var position = RoomGeometry.PlayerPosition(layout, x: 0f);
            var feet = position.y - layout.PlayerHeight / 2f;

            Assert.AreEqual(RoomGeometry.FloorTopY(layout), feet, 0.0001f);
        }

        [Test]
        public void 방_크기는_주입받은_데이터를_그대로_따른다()
        {
            // 다른 규격을 주면 좌표도 그대로 따라 바뀐다 — 코드에 4와 7이
            // 박혀 있지 않다는 확인이다.
            var wide = new MemoryRoomLayout(
                roomHeight: 6f, roomLength: 12f, wallThickness: 0.2f,
                playerHeight: 1f, playerWidth: 0.4f, playerMoveSpeed: 3f, edgeMargin: 0.5f,
                posterMountHeight: 3f, posterSize: 0.8f, floorObjectSize: 0.5f,
                exitMarkerWidth: 0.5f, exitMarkerHeight: 1.6f, cameraVerticalMargin: 0.6f);

            Assert.AreEqual(-6f, RoomGeometry.LeftInnerX(wide), 0.0001f);
            Assert.AreEqual(6f, RoomGeometry.RightInnerX(wide), 0.0001f);
            Assert.AreEqual(6f, RoomGeometry.CeilingY(wide), 0.0001f);
            Assert.AreEqual(new Vector2(0f, 3f), RoomGeometry.BackWall(wide).Center);
        }
    }
}
