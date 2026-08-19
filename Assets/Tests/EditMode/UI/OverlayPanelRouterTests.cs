using GameName.UI.Overlays;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // 오버레이 가시성이 한 곳에서만 관리된다는 것을 검증한다.
    //
    // 지금은 기록지가 Tab, 인벤토리가 I로 따로 열리지만, 나중에 Tab 하나로
    // 둘을 오가는 탭 패널이 된다. 그때 필요한 성질("한 번에 하나만 보인다")이
    // 이미 라우터에 있다는 것을 여기서 못박아 둔다 — 통합 작업이 이 규칙을
    // 새로 만드는 일이 되지 않도록.
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
            var journal = new FakePanel();

            router.Register(OverlayPanel.Journal, journal);

            Assert.IsFalse(journal.IsVisible);
            Assert.IsNull(router.Visible);
        }

        [Test]
        public void 같은_화면을_다시_토글하면_닫힌다()
        {
            var router = new OverlayPanelRouter();
            var journal = new FakePanel();
            router.Register(OverlayPanel.Journal, journal);

            router.Toggle(OverlayPanel.Journal);
            Assert.IsTrue(journal.IsVisible);

            router.Toggle(OverlayPanel.Journal);
            Assert.IsFalse(journal.IsVisible);
            Assert.IsNull(router.Visible);
        }

        [Test]
        public void 한_번에_하나만_보인다()
        {
            var router = new OverlayPanelRouter();
            var journal = new FakePanel();
            var inventory = new FakePanel();
            router.Register(OverlayPanel.Journal, journal);
            router.Register(OverlayPanel.Inventory, inventory);

            router.Toggle(OverlayPanel.Journal);
            router.Toggle(OverlayPanel.Inventory);

            Assert.IsFalse(journal.IsVisible);
            Assert.IsTrue(inventory.IsVisible);
            Assert.AreEqual(OverlayPanel.Inventory, router.Visible);
        }

        // 확대 화면은 키가 아니라 게임 안의 행동으로 열리지만, 가시성 규칙은
        // 똑같이 라우터를 거친다.
        [Test]
        public void 확대_화면을_열면_다른_오버레이가_닫힌다()
        {
            var router = new OverlayPanelRouter();
            var journal = new FakePanel();
            var zoom = new FakePanel();
            router.Register(OverlayPanel.Journal, journal);
            router.Register(OverlayPanel.ClueZoom, zoom);

            router.Toggle(OverlayPanel.Journal);
            router.Show(OverlayPanel.ClueZoom);

            Assert.IsFalse(journal.IsVisible);
            Assert.IsTrue(zoom.IsVisible);
        }

        [Test]
        public void 나중에_등록된_화면도_지금의_가시성_상태를_따른다()
        {
            var router = new OverlayPanelRouter();
            var journal = new FakePanel();
            router.Register(OverlayPanel.Journal, journal);
            router.Toggle(OverlayPanel.Journal);

            // 화면 전환으로 뒤늦게 인벤토리가 등록되는 상황.
            var inventory = new FakePanel();
            router.Register(OverlayPanel.Inventory, inventory);

            Assert.IsFalse(inventory.IsVisible);
            Assert.IsTrue(journal.IsVisible);
        }

        [Test]
        public void 등록되지_않은_화면을_열려_해도_아무_일도_일어나지_않는다()
        {
            var router = new OverlayPanelRouter();
            var journal = new FakePanel();
            router.Register(OverlayPanel.Journal, journal);
            router.Toggle(OverlayPanel.Journal);

            router.Show(OverlayPanel.Inventory);

            // 아직 등록 전인 화면 때문에 이미 열려 있던 것이 닫히면 안 된다.
            Assert.IsTrue(journal.IsVisible);
            Assert.AreEqual(OverlayPanel.Journal, router.Visible);
        }
    }
}
