using GameName.Core.Clues;
using GameName.Core.Memories;
using GameName.Core.Restoration;
using GameName.UI.Restoration;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // 복원도 캔버스 뷰의 순수 UI 규칙:
    //   · 플레이어 메모에는 항상 보이는 × 삭제 버튼이 있고, 낮은 확대율에서도
    //     트리에 남아 클릭 가능한 크기를 유지한다(반대 배율).
    //   · × → 확인 → PlayerNodeDeleteRequested. 우클릭 없이도 삭제가 된다.
    //   · FocusNode는 확대율을 유지한 채 그 노드를 창 중앙으로 옮긴다.
    //
    // 패널에 붙일 수 없는 EditMode라 Button.clicked·레이아웃은 못 쓴다 — 뷰가
    // 연 internal 시드로 같은 코드 경로를 탄다.
    public class RestorationCanvasViewTests
    {
        private static VisualElement MakeRoot()
        {
            var root = new VisualElement();

            var viewport = new VisualElement { name = "restoration-viewport" };
            var pan = new VisualElement { name = "restoration-pan" };
            var canvas = new VisualElement { name = "restoration-canvas" };
            canvas.Add(new VisualElement { name = "restoration-edges" });
            canvas.Add(new VisualElement { name = "restoration-nodes" });
            pan.Add(canvas);
            viewport.Add(pan);
            root.Add(viewport);

            root.Add(new VisualElement { name = "restoration-addbar" });
            root.Add(new Button { name = "restoration-connect-toggle" });
            root.Add(new Label { name = "restoration-notice" });
            root.Add(new Button { name = "restoration-zoom-in" });
            root.Add(new Button { name = "restoration-zoom-out" });
            root.Add(new Button { name = "restoration-zoom-reset" });

            return root;
        }

        private static RestorationCanvasView MakeView(VisualElement root = null) =>
            new RestorationCanvasView(root ?? MakeRoot(), c => c.ToString());

        private static RestorationNode PlayerNode(string id, float x = 300f, float y = 200f) =>
            new RestorationNode(
                new RestorationNodeId(id), MemoryColor.Red, RestorationNodeKind.PlayerAuthored,
                "유키의 메모", new BoardPosition(x, y), null);

        private static RestorationNode RootNode(string id) =>
            new RestorationNode(
                new RestorationNodeId(id), MemoryColor.Blue, RestorationNodeKind.ColorRoot,
                string.Empty, new BoardPosition(10f, 10f), null);

        private static RestorationNode ClueNode(string id) =>
            new RestorationNode(
                new RestorationNodeId(id), MemoryColor.Blue, RestorationNodeKind.ClueLinked,
                "낡은 모포", new BoardPosition(20f, 20f), new ClueId("clue-x"));

        [Test]
        public void 플레이어_메모에는_삭제_버튼이_있고_잠긴_노드에는_없다()
        {
            var view = MakeView();

            view.AddNode(PlayerNode("p1"));
            view.AddNode(RootNode("root-blue"));
            view.AddNode(ClueNode("clue-blanket"));

            Assert.IsNotNull(view.NodeElementFor(new RestorationNodeId("p1")).DeleteButton,
                "플레이어 메모에 × 버튼이 없다.");
            Assert.IsNull(view.NodeElementFor(new RestorationNodeId("root-blue")).DeleteButton,
                "색 루트에 삭제 버튼이 생겼다.");
            Assert.IsNull(view.NodeElementFor(new RestorationNodeId("clue-blanket")).DeleteButton,
                "단서 노드에 삭제 버튼이 생겼다.");
        }

        [Test]
        public void 삭제_버튼은_낮은_확대율에서도_트리에_남고_반대_배율을_받는다()
        {
            var view = MakeView();
            view.AddNode(PlayerNode("p1"));
            var element = view.NodeElementFor(new RestorationNodeId("p1"));

            view.SetZoomForTest(0.1f); // 크게 축소

            Assert.AreSame(element.Root, element.DeleteButton.parent, "× 버튼이 노드에서 떨어져 나갔다.");
            Assert.AreEqual(10f, element.DeleteButton.style.scale.value.value.x, 0.001f,
                "축소된 만큼 반대 배율(1/zoom)이 걸리지 않았다 — 화면상 크기가 유지되지 않는다.");
        }

        [Test]
        public void 삭제_버튼_클릭_후_확인하면_PlayerNodeDeleteRequested가_발행된다()
        {
            var view = MakeView();
            view.AddNode(PlayerNode("p1"));
            var element = view.NodeElementFor(new RestorationNodeId("p1"));

            RestorationNodeId? requested = null;
            view.PlayerNodeDeleteRequested += id => requested = id;

            element.ClickDeleteButtonForTest();
            Assert.IsTrue(element.HasDeleteConfirmOpen, "× 클릭에 확인 팝업이 뜨지 않았다.");
            Assert.IsNull(requested, "확인 전에 삭제가 발행됐다.");

            element.ConfirmDeleteForTest();
            Assert.AreEqual(new RestorationNodeId("p1"), requested);
            Assert.IsFalse(element.HasDeleteConfirmOpen, "확인 후 팝업이 남아 있다.");
        }

        [Test]
        public void CenteringPan은_노드_중심을_창_중심에_두는_팬을_낸다()
        {
            // 레이아웃이 없는 EditMode에선 실제 뷰포트를 못 재므로 순수 계산을
            // 직접 검증한다. FocusNode는 이 값을 그대로 팬에 쓴다.
            var vp = new UnityEngine.Rect(0f, 0f, 868f, 408f);
            var center = new UnityEngine.Vector2(700f, 500f);
            const float zoom = 0.35f;

            var pan = RestorationCanvasView.CenteringPan(vp, center, zoom);

            // 노드 중심이 창 중심에 오는가: center*zoom + pan == 창 중심
            Assert.AreEqual(vp.width * 0.5f, center.x * zoom + pan.x, 0.001f);
            Assert.AreEqual(vp.height * 0.5f, center.y * zoom + pan.y, 0.001f);
        }

        [Test]
        public void 없는_노드에_FocusNode하면_아무_일도_없다()
        {
            var view = MakeView();
            var panBefore = view.PanForTest;

            view.FocusNode(new RestorationNodeId("nope"));

            Assert.AreEqual(panBefore.x, view.PanForTest.x, 0.0001f);
            Assert.AreEqual(panBefore.y, view.PanForTest.y, 0.0001f);
        }

        [Test]
        public void 창이_측정되기_전에_FocusNode하면_팬을_NaN으로_만들지_않는다()
        {
            var view = MakeView();
            view.AddNode(PlayerNode("p1", x: 640f, y: 480f));

            view.FocusNode(new RestorationNodeId("p1"));

            var pan = view.PanForTest;
            Assert.IsFalse(float.IsNaN(pan.x) || float.IsNaN(pan.y), "팬이 NaN이 됐다 — 캔버스가 사라진다.");
        }
    }
}
