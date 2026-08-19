using System.Collections.Generic;
using System.Linq;
using GameName.Core.Clues;
using GameName.Core.MemoryRooms;
using GameName.UI.MemoryRoom.Space;
using NUnit.Framework;
using UnityEngine;

namespace GameName.UI.Tests.EditMode
{
    // 계산이 아니라 "실제로 만들어진 씬 오브젝트의 상태"를 본다.
    //
    // 이 파일이 생긴 이유가 곧 이번 버그의 교훈이다. 기존 테스트는 좌표 계산
    // (CluePlacementLayout)과 단서 오브젝트 하나의 동작(ClueSceneObject)은
    // 촘촘히 검사했지만, 그 둘을 실제 오브젝트로 조립하는 구간 — 콜라이더가
    // 붙는가, 크기가 보이는 사각형과 같은가, 다시 그렸을 때 이전 것이 정말
    // 사라지는가 — 은 아무도 보지 않았다. 증상이 전부 그 구간에서 나왔다.
    //
    // 여기서는 씬을 띄우지 않고 GameObject만 만들어 확인한다. MemoryRoomSpaceView는
    // 규칙을 계산하지 않고 지시를 오브젝트로 옮기기만 하므로, 카메라도 입력도
    // 없이 그 결과를 그대로 읽을 수 있다.
    public class MemoryRoomSpaceViewObjectTests
    {
        private static readonly ClueId Poster = new ClueId("clue-poster");
        private static readonly ClueId FloorObject = new ClueId("clue-floor");

        private MemoryRoomSpaceView _view;
        private MemoryRoomLayout _layout;

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

        [SetUp]
        public void SetUp()
        {
            _layout = MakeLayout();
            _view = new GameObject("Space").AddComponent<MemoryRoomSpaceView>();
            _view.Build(_layout);
        }

        [TearDown]
        public void TearDown()
        {
            if (_view != null)
                Object.DestroyImmediate(_view.gameObject);
        }

        private ClueSceneItem ClueAt(ClueId id, ClueKind kind, float ratio) =>
            new ClueSceneItem(
                id, kind, CluePlacementLayout.PositionAt(_layout, kind, new CluePositionRatio(ratio)));

        private ClueSceneObject[] CluesInScene() =>
            _view.GetComponentsInChildren<ClueSceneObject>(includeInactive: true);

        private RoomExitSceneObject[] ExitsInScene() =>
            _view.GetComponentsInChildren<RoomExitSceneObject>(includeInactive: true);

        private static IReadOnlyList<RoomExitSceneItem> NoExits() => new RoomExitSceneItem[0];

        // 보이는 사각형이 본체(Body) 자식이고, 마우스 판정은 부모의 콜라이더다.
        // 둘이 어긋나면 "보이는데 눌리지 않는다"가 되고, 화면만 봐서는 어긋난
        // 사실 자체가 드러나지 않는다.
        [Test]
        public void 두_종류_단서_모두_콜라이더가_붙고_크기가_보이는_사각형과_같다()
        {
            _view.SetContents(
                new[] { ClueAt(Poster, ClueKind.Poster, 0.2f), ClueAt(FloorObject, ClueKind.FloorObject, 0.7f) },
                NoExits());

            var clues = CluesInScene();
            Assert.AreEqual(2, clues.Length, "단서 두 종이 모두 만들어지지 않았다.");

            foreach (var clue in clues)
            {
                var collider = clue.GetComponent<BoxCollider2D>();
                Assert.IsNotNull(collider, $"{clue.name}에 마우스 판정 콜라이더가 없다.");

                var body = clue.transform.Find("Body");
                Assert.IsNotNull(body, $"{clue.name}에 보이는 본체가 없다.");

                Assert.AreEqual(body.localScale.x, collider.size.x, 0.0001f, $"{clue.name}의 판정 폭이 다르다.");
                Assert.AreEqual(body.localScale.y, collider.size.y, 0.0001f, $"{clue.name}의 판정 높이가 다르다.");

                // 중심도 같아야 한다 — 크기만 같고 한쪽으로 밀려 있으면 사각형의
                // 절반은 여전히 눌리지 않는다.
                Assert.AreEqual(
                    body.position.x, collider.bounds.center.x, 0.0001f, $"{clue.name}의 판정 중심이 어긋났다.");
                Assert.AreEqual(
                    body.position.y, collider.bounds.center.y, 0.0001f, $"{clue.name}의 판정 중심이 어긋났다.");
            }
        }

        // 바닥 물건은 바닥에 붙어 있어 바닥·벽 콜라이더와 겹칠 여지가 가장 큰
        // 단서다. 그래도 판정 영역이 방 안(바닥 윗면 위)에 온전히 들어와야 한다.
        [Test]
        public void 바닥_물건의_판정_영역은_바닥_아래로_내려가지_않는다()
        {
            _view.SetContents(new[] { ClueAt(FloorObject, ClueKind.FloorObject, 0.5f) }, NoExits());

            var collider = CluesInScene().Single().GetComponent<BoxCollider2D>();

            Assert.AreEqual(
                RoomGeometry.FloorTopY(_layout), collider.bounds.min.y, 0.0001f,
                "바닥 물건의 아랫면이 바닥 윗면에 정확히 얹혀 있지 않다.");
        }

        // 증상 3·4의 핵심. 이전에는 Destroy가 프레임 끝에야 실제로 지워서,
        // 다시 그린 직후에는 이전 방의 단서가 콜라이더째 함께 살아 있었다.
        [Test]
        public void 방을_다시_그리면_이전_단서_오브젝트가_그_자리에서_사라진다()
        {
            _view.SetContents(
                new[] { ClueAt(Poster, ClueKind.Poster, 0.2f), ClueAt(FloorObject, ClueKind.FloorObject, 0.7f) },
                NoExits());
            Assert.AreEqual(2, CluesInScene().Length);

            var next = new ClueId("clue-next-room");
            _view.SetContents(new[] { ClueAt(next, ClueKind.Poster, 0.4f) }, NoExits());

            var clues = CluesInScene();
            Assert.AreEqual(1, clues.Length, "이전 방의 단서 오브젝트가 남아 있다.");
            Assert.AreEqual("Clue_" + next, clues[0].name, "남아 있는 것이 새 방의 단서가 아니다.");
        }

        // 계단·분석실처럼 방이 아닌 곳으로 나갔을 때. 끄기만 하면 방이 다시
        // 켜지는 순간 이전 방 단서가 새 방 단서와 함께 되살아난다.
        [Test]
        public void 방을_치우면_단서_오브젝트가_하나도_남지_않는다()
        {
            _view.SetContents(new[] { ClueAt(Poster, ClueKind.Poster, 0.2f) }, NoExits());

            _view.HideRoom();

            Assert.AreEqual(0, CluesInScene().Length, "방을 치웠는데 단서 오브젝트가 남아 있다.");
        }

        [Test]
        public void 같은_방의_단서들은_서로_다른_자리에_놓인다()
        {
            _view.SetContents(
                new[] { ClueAt(Poster, ClueKind.Poster, 0.2f), ClueAt(FloorObject, ClueKind.FloorObject, 0.7f) },
                NoExits());

            var positions = CluesInScene().Select(c => c.transform.position).ToArray();

            Assert.AreNotEqual(positions[0], positions[1], "두 단서가 같은 자리에 겹쳐 놓였다.");
        }

        // 출입구가 셋이면 예전에는 첫 번째와 세 번째가 같은 벽 같은 자리에
        // 겹쳤다. 겹치면 앞의 문이 뒤의 문을 완전히 가려 눌러도 다른 곳으로 간다.
        [Test]
        public void 출입구가_셋이어도_서로_겹치지_않는다()
        {
            var exits = new[]
            {
                MakeExit("계단", RoomExitKind.Door, 0, 3),
                MakeExit("분석실", RoomExitKind.Door, 1, 3),
                MakeExit("조향실", RoomExitKind.Door, 2, 3),
            };

            _view.SetContents(new ClueSceneItem[0], exits);

            var colliders = ExitsInScene().Select(e => e.GetComponent<BoxCollider2D>()).ToArray();
            Assert.AreEqual(3, colliders.Length);

            for (var i = 0; i < colliders.Length; i++)
            {
                for (var j = i + 1; j < colliders.Length; j++)
                {
                    Assert.IsFalse(
                        Overlaps(colliders[i], colliders[j]),
                        $"출입구 {i}번과 {j}번의 판정 영역이 겹친다.");
                }
            }
        }

        // 사다리가 둘인 방(데모의 방 2)에서 두 오브젝트가 실제로 따로 서는지
        // 본다. 계산만 맞고 오브젝트가 겹치면 화면에서는 하나로 보이고, 둘 중
        // 하나는 영영 누를 수 없다.
        [Test]
        public void 사다리가_둘이어도_판정_영역이_겹치지_않는다()
        {
            var exits = new[]
            {
                MakeExit("아래 방", RoomExitKind.Ladder, 0, 2),
                MakeExit("위 방", RoomExitKind.Ladder, 1, 2),
            };

            _view.SetContents(new ClueSceneItem[0], exits);

            var colliders = ExitsInScene().Select(e => e.GetComponent<BoxCollider2D>()).ToArray();
            Assert.AreEqual(2, colliders.Length, "사다리 두 개가 모두 만들어지지 않았다.");
            Assert.AreNotEqual(
                colliders[0].bounds.center, colliders[1].bounds.center, "두 사다리가 같은 자리에 포개졌다.");
            Assert.IsFalse(Overlaps(colliders[0], colliders[1]), "두 사다리의 판정 영역이 겹친다.");
        }

        // 마우스를 올렸을 때 켜지는 테두리는 본체보다 뒤에 있어야 한다. 앞에
        // 있으면 가리키는 순간 단서가 흰 사각형으로 덮여 무엇인지 알 수 없다.
        [Test]
        public void 강조_테두리는_본체보다_뒤에_그려진다()
        {
            _view.SetContents(new[] { ClueAt(FloorObject, ClueKind.FloorObject, 0.5f) }, NoExits());

            var clue = CluesInScene().Single();
            var outline = clue.transform.Find("Outline").GetComponent<SpriteRenderer>();
            var body = clue.transform.Find("Body").GetComponent<SpriteRenderer>();

            Assert.Less(outline.sortingOrder, body.sortingOrder, "테두리가 본체를 덮는다.");
        }

        // 나란히 붙은 두 판정 영역은 경계선을 공유한다. Bounds.Intersects는 그
        // 맞닿은 상태도 겹친 것으로 치므로, 부동소수 오차만큼 줄여 놓고 본다 —
        // 여기서 잡으려는 것은 "정확히 같은 자리에 포개진" 경우다.
        private static bool Overlaps(Collider2D left, Collider2D right)
        {
            const float touchTolerance = 0.001f;

            var shrunk = left.bounds;
            shrunk.Expand(-touchTolerance);
            return shrunk.Intersects(right.bounds);
        }

        private RoomExitSceneItem MakeExit(string label, RoomExitKind kind, int index, int count) =>
            new RoomExitSceneItem(
                MemoryGraphNodeId.OfRoom(new MemoryRoomId("room-" + index)),
                kind,
                RoomExitLayout.PositionOf(_layout, kind, index, count),
                label);
    }
}
