using System.Linq;
using GameName.Core.Clues;
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
    // 사라지는가 — 은 아무도 보지 않았다.
    //
    // 여기서는 씬을 띄우지 않고 GameObject만 만들어 확인한다.
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

        private ClueSceneItem ClueAt(ClueId id, ClueKind kind, float ratio, bool accessible = true) =>
            new ClueSceneItem(
                id, kind, CluePlacementLayout.PositionAt(_layout, kind, new CluePositionRatio(ratio)), accessible);

        private ClueSceneObject[] CluesInScene() =>
            _view.GetComponentsInChildren<ClueSceneObject>(includeInactive: true);

        // 보이는 사각형이 본체(Body) 자식이고, 마우스 판정은 부모의 콜라이더다.
        // 둘이 어긋나면 "보이는데 눌리지 않는다"가 되고, 화면만 봐서는 어긋난
        // 사실 자체가 드러나지 않는다.
        [Test]
        public void 두_종류_단서_모두_콜라이더가_붙고_크기가_보이는_사각형과_같다()
        {
            _view.SetContents(
                new[] { ClueAt(Poster, ClueKind.Poster, 0.2f), ClueAt(FloorObject, ClueKind.FloorObject, 0.7f) });

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

                Assert.AreEqual(
                    body.position.x, collider.bounds.center.x, 0.0001f, $"{clue.name}의 판정 중심이 어긋났다.");
                Assert.AreEqual(
                    body.position.y, collider.bounds.center.y, 0.0001f, $"{clue.name}의 판정 중심이 어긋났다.");
            }
        }

        [Test]
        public void 바닥_물건의_판정_영역은_바닥_아래로_내려가지_않는다()
        {
            _view.SetContents(new[] { ClueAt(FloorObject, ClueKind.FloorObject, 0.5f) });

            var collider = CluesInScene().Single().GetComponent<BoxCollider2D>();

            Assert.AreEqual(
                RoomGeometry.FloorTopY(_layout), collider.bounds.min.y, 0.0001f,
                "바닥 물건의 아랫면이 바닥 윗면에 정확히 얹혀 있지 않다.");
        }

        // 이전에는 Destroy가 프레임 끝에야 실제로 지워서, 다시 그린 직후에는
        // 이전 방의 단서가 콜라이더째 함께 살아 있었다.
        [Test]
        public void 방을_다시_그리면_이전_단서_오브젝트가_그_자리에서_사라진다()
        {
            _view.SetContents(
                new[] { ClueAt(Poster, ClueKind.Poster, 0.2f), ClueAt(FloorObject, ClueKind.FloorObject, 0.7f) });
            Assert.AreEqual(2, CluesInScene().Length);

            var next = new ClueId("clue-next-room");
            _view.SetContents(new[] { ClueAt(next, ClueKind.Poster, 0.4f) });

            var clues = CluesInScene();
            Assert.AreEqual(1, clues.Length, "이전 방의 단서 오브젝트가 남아 있다.");
            Assert.AreEqual("Clue_" + next, clues[0].name, "남아 있는 것이 새 방의 단서가 아니다.");
        }

        // 접근 불가로 지시받은 단서는 콜라이더가 꺼진 채로 만들어진다. (3차
        // 개편에서 접근 정책은 사라졌지만 ClueSceneItem.Accessible 표시 경로
        // 자체는 남아 있어 계속 확인한다.)
        [Test]
        public void 접근_불가로_지시된_단서는_콜라이더가_꺼진_채_만들어진다()
        {
            _view.SetContents(new[] { ClueAt(Poster, ClueKind.Poster, 0.05f, accessible: false) });

            var clue = CluesInScene().Single();
            Assert.IsFalse(clue.GetComponent<BoxCollider2D>().enabled, "가시 밖 단서의 콜라이더가 살아 있다.");
            Assert.IsFalse(clue.IsAccessible);
        }

        [Test]
        public void 방을_치우면_단서_오브젝트가_하나도_남지_않는다()
        {
            _view.SetContents(new[] { ClueAt(Poster, ClueKind.Poster, 0.2f) });

            _view.HideRoom();

            Assert.AreEqual(0, CluesInScene().Length, "방을 치웠는데 단서 오브젝트가 남아 있다.");
        }

        [Test]
        public void 같은_방의_단서들은_서로_다른_자리에_놓인다()
        {
            _view.SetContents(
                new[] { ClueAt(Poster, ClueKind.Poster, 0.2f), ClueAt(FloorObject, ClueKind.FloorObject, 0.7f) });

            var positions = CluesInScene().Select(c => c.transform.position).ToArray();

            Assert.AreNotEqual(positions[0], positions[1], "두 단서가 같은 자리에 겹쳐 놓였다.");
        }

        // 마우스를 올렸을 때 켜지는 테두리는 본체보다 뒤에 있어야 한다. 앞에
        // 있으면 가리키는 순간 단서가 흰 사각형으로 덮여 무엇인지 알 수 없다.
        [Test]
        public void 강조_테두리는_본체보다_뒤에_그려진다()
        {
            _view.SetContents(new[] { ClueAt(FloorObject, ClueKind.FloorObject, 0.5f) });

            var clue = CluesInScene().Single();
            var outline = clue.transform.Find("Outline").GetComponent<SpriteRenderer>();
            var body = clue.transform.Find("Body").GetComponent<SpriteRenderer>();

            Assert.Less(outline.sortingOrder, body.sortingOrder, "테두리가 본체를 덮는다.");
        }
    }
}
