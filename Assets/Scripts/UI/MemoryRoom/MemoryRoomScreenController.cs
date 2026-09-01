using System;
using GameName.Core.Clues;
using GameName.UI.ClueZoom;
using GameName.UI.Inventory;
using GameName.UI.MemoryRoom.Space;
using GameName.UI.Overlays;

namespace GameName.UI.MemoryRoom
{
    // 기억 방 화면을 이루는 조각들을 조립하고, 조각을 넘나드는 정보만 중개한다.
    //
    // 조각이 세 종류로 나뉘어 있다:
    //   · 2D 씬(MemoryRoomSpaceController) — 방 공간과 단서 오브젝트
    //   · 상단 바(MemoryRoomHudView) — 얇게 걸치는 조작 안내와 상황 문구
    //   · 필요할 때만 뜨는 오버레이 — 인벤토리·확대 화면
    // 이들을 서로 잇는 일은 전부 여기서 한다. 각 조각은 서로를 알지 못한다.
    //
    // 규칙은 하나도 계산하지 않는다 — 단서를 담을 수 있는지, 갈 수 있는지는
    // 각 컨트롤러가 이미 Core에서 얻은 결론이고, 여기서는 "그러면 어느 조각을
    // 다시 그려야 하는가"만 정한다.
    public sealed class MemoryRoomScreenController : IDisposable
    {
        private readonly MemoryRoomHudView _hud;
        private readonly MemoryRoomSpaceController _space;
        private readonly ClueZoomScreenController _clueZoom;
        private readonly InventoryScreenController _inventory;
        private readonly OverlayPanelHost _overlayPanels;

        public MemoryRoomScreenController(
            MemoryRoomHudView hud,
            MemoryRoomSpaceController space,
            ClueZoomScreenController clueZoom,
            InventoryScreenController inventory,
            OverlayPanelHost overlayPanels)
        {
            _hud = hud ?? throw new ArgumentNullException(nameof(hud));
            _space = space ?? throw new ArgumentNullException(nameof(space));
            _clueZoom = clueZoom ?? throw new ArgumentNullException(nameof(clueZoom));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _overlayPanels = overlayPanels ?? throw new ArgumentNullException(nameof(overlayPanels));

            _space.ClueActivated += OnClueActivated;
            _space.MessageChanged += _hud.SetMessage;

            _clueZoom.CloseRequested += OnClueZoomCloseRequested;
            _clueZoom.ClueStored += OnClueStored;

            // 전체 화면 오버레이가 떠 있는 동안에는 뒤의 방을 건드릴 수 없다.
            // 확대 화면만 이 처리를 따로 하지 않고 라우터의 알림 하나로 통일한다.
            _overlayPanels.VisibleChanged += OnOverlayVisibilityChanged;

            ApplyOverlayBlocking();
        }

        // 씬에서 단서를 눌렀다 — 확대 화면을 띄운다. 씬 조작을 막는 것은
        // 오버레이 가시성 알림이 알아서 처리하므로 여기서 따로 끄지 않는다.
        private void OnClueActivated(ClueInfo clue)
        {
            _clueZoom.Open(clue);
            _overlayPanels.Show(OverlayPanel.ClueZoom);
        }

        private void OnClueZoomCloseRequested() => _overlayPanels.Hide(OverlayPanel.ClueZoom);

        // 확대 화면이 실제로 숨겨진 시점에 불린다 — 나가기 버튼으로 닫혔든,
        // 다른 오버레이에 밀려났든 똑같이 드래그 상태를 정리해야 하기 때문에
        // "닫아 달라는 요청"이 아니라 "닫혔다는 사실"에 반응한다.
        public void OnClueZoomHidden() => _clueZoom.OnHidden();

        private void OnOverlayVisibilityChanged(OverlayPanel? visible) => ApplyOverlayBlocking();

        private void ApplyOverlayBlocking() => _space.SetInteractionEnabled(!_overlayPanels.IsAnyVisible);

        private void OnClueStored()
        {
            // 담은 단서는 방에서 사라지고 인벤토리에 나타난다. 습득은 이벤트를
            // 발행하지 않으므로(그 자리에서 결과가 바로 나온다) 두 조각을
            // 여기서 직접 다시 그리게 한다.
            _space.Refresh();
            _inventory.Refresh();
        }

        public void Dispose()
        {
            _space.ClueActivated -= OnClueActivated;
            _space.MessageChanged -= _hud.SetMessage;

            _clueZoom.CloseRequested -= OnClueZoomCloseRequested;
            _clueZoom.ClueStored -= OnClueStored;

            _overlayPanels.VisibleChanged -= OnOverlayVisibilityChanged;

            _space.Dispose();
            _clueZoom.Dispose();
            _inventory.Dispose();
        }
    }
}
