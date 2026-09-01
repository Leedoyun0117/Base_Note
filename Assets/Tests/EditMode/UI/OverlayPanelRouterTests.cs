using GameName.UI.Overlays;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // 오버레이 가시성이 한 곳에서만 관리된다는 것을 검증한다 — 종류가 늘어나도
    // "한 번에 하나만 보인다"는 규칙을 각 화면이 다시 구현하지 않아도 되게
    // 하는 것이 이 라우터의 존재 이유다.
    public class OverlayPanelRouterTests
    {
        private sealed class FakePanel : IOverlayPanelContent
        {
            public bool IsVisible { get; private set; }
            public void SetVisible(bool visible) => IsVisible = visible;
        }

        [Test]
        public void 등록_직후에는_모두_숨겨져_있다()
        {
            var router = new OverlayPanelRouter();
            var inventory = new FakePanel();

            router.Register(OverlayPanel.Inventory, inventory);

            Assert.IsFalse(inventory.IsVisible);
            Assert.IsNull(router.Visible);
        }

        [Test]
        public void 같은_화면을_다시_토글하면_닫힌다()
        {
            var router = new OverlayPanelRouter();
            var inventory = new FakePanel();
            router.Register(OverlayPanel.Inventory, inventory);

            router.Toggle(OverlayPanel.Inventory);
            Assert.IsTrue(inventory.IsVisible);

            router.Toggle(OverlayPanel.Inventory);
            Assert.IsFalse(inventory.IsVisible);
            Assert.IsNull(router.Visible);
        }

        [Test]
        public void 한_번에_하나만_보인다()
        {
            var router = new OverlayPanelRouter();
            var zoom = new FakePanel();
            var inventory = new FakePanel();
            router.Register(OverlayPanel.ClueZoom, zoom);
            router.Register(OverlayPanel.Inventory, inventory);

            router.Toggle(OverlayPanel.ClueZoom);
            router.Toggle(OverlayPanel.Inventory);

            Assert.IsFalse(zoom.IsVisible);
            Assert.IsTrue(inventory.IsVisible);
            Assert.AreEqual(OverlayPanel.Inventory, router.Visible);
        }

        // 확대 화면은 키가 아니라 게임 안의 행동으로 열리지만, 가시성 규칙은
        // 똑같이 라우터를 거친다.
        [Test]
        public void 확대_화면을_열면_다른_오버레이가_닫힌다()
        {
            var router = new OverlayPanelRouter();
            var inventory = new FakePanel();
            var zoom = new FakePanel();
            router.Register(OverlayPanel.Inventory, inventory);
            router.Register(OverlayPanel.ClueZoom, zoom);

            router.Toggle(OverlayPanel.Inventory);
            router.Show(OverlayPanel.ClueZoom);

            Assert.IsFalse(inventory.IsVisible);
            Assert.IsTrue(zoom.IsVisible);
        }

        [Test]
        public void 나중에_등록된_화면도_지금의_가시성_상태를_따른다()
        {
            var router = new OverlayPanelRouter();
            var inventory = new FakePanel();
            router.Register(OverlayPanel.Inventory, inventory);
            router.Toggle(OverlayPanel.Inventory);

            // 화면 전환으로 뒤늦게 확대 화면이 등록되는 상황.
            var zoom = new FakePanel();
            router.Register(OverlayPanel.ClueZoom, zoom);

            Assert.IsFalse(zoom.IsVisible);
            Assert.IsTrue(inventory.IsVisible);
        }

        [Test]
        public void 등록되지_않은_화면을_열려_해도_아무_일도_일어나지_않는다()
        {
            var router = new OverlayPanelRouter();
            var inventory = new FakePanel();
            router.Register(OverlayPanel.Inventory, inventory);
            router.Toggle(OverlayPanel.Inventory);

            router.Show(OverlayPanel.ClueZoom);

            // 아직 등록 전인 화면 때문에 이미 열려 있던 것이 닫히면 안 된다.
            Assert.IsTrue(inventory.IsVisible);
            Assert.AreEqual(OverlayPanel.Inventory, router.Visible);
        }
    }
}
