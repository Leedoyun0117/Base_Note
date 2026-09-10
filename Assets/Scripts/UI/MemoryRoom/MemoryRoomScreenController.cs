using System;
using GameName.Core.Clues;
using GameName.UI.ClueZoom;
using GameName.UI.MemoryRoom.Space;
using GameName.UI.Overlays;

namespace GameName.UI.MemoryRoom
{
    // 기억 방 화면을 이루는 조각들을 조립하고, 조각을 넘나드는 정보만 중개한다.
    //
    // 조각들:
    //   · 2D 씬(MemoryRoomSpaceController) — 방 공간과 단서 오브젝트
    //   · 상단 바(MemoryRoomHudController) — 턴·안정 축·활성 컴플렉스
    //   · 스토리 패널(ClueZoomScreenController) — 단서를 클릭했을 때의 서사 +
    //     해석 로그. 오버레이로 뜬다.
    // 이들을 서로 잇는 일은 전부 여기서 한다. 각 조각은 서로를 알지 못한다.
    //
    // 규칙은 하나도 계산하지 않는다.
    public sealed class MemoryRoomScreenController : IDisposable
    {
        private readonly MemoryRoomHudView _hud;
        private readonly MemoryRoomHudController _hudController;
        private readonly MemoryRoomSpaceController _space;
        private readonly ClueZoomScreenController _clueZoom;
        private readonly OverlayPanelHost _overlayPanels;

        public MemoryRoomScreenController(
            MemoryRoomHudView hud,
            MemoryRoomHudController hudController,
            MemoryRoomSpaceController space,
            ClueZoomScreenController clueZoom,
            OverlayPanelHost overlayPanels)
        {
            _hud = hud ?? throw new ArgumentNullException(nameof(hud));
            _hudController = hudController ?? throw new ArgumentNullException(nameof(hudController));
            _space = space ?? throw new ArgumentNullException(nameof(space));
            _clueZoom = clueZoom ?? throw new ArgumentNullException(nameof(clueZoom));
            _overlayPanels = overlayPanels ?? throw new ArgumentNullException(nameof(overlayPanels));

            _space.ClueActivated += OnClueActivated;
            _space.MessageChanged += _hud.SetMessage;

            _clueZoom.CloseRequested += OnClueZoomCloseRequested;
            _clueZoom.ClueRead += OnClueRead;

            _overlayPanels.VisibleChanged += OnOverlayVisibilityChanged;
            ApplyOverlayBlocking();
        }

        // 씬에서 단서를 눌렀다 — 스토리 패널을 열고 그 단서를 읽는다.
        private void OnClueActivated(ClueInfo clue)
        {
            _overlayPanels.Show(OverlayPanel.ClueZoom);
            _clueZoom.Preview(clue);
        }

        private void OnClueZoomCloseRequested() => _overlayPanels.Hide(OverlayPanel.ClueZoom);

        public void OnClueZoomHidden() => _clueZoom.OnHidden();

        // 단서를 읽어 소모했다 — 방에서 지운다.
        private void OnClueRead() => _space.Refresh();

        private void OnOverlayVisibilityChanged(OverlayPanel? visible) => ApplyOverlayBlocking();

        private void ApplyOverlayBlocking() => _space.SetInteractionEnabled(!_overlayPanels.IsAnyVisible);

        public void Dispose()
        {
            _space.ClueActivated -= OnClueActivated;
            _space.MessageChanged -= _hud.SetMessage;

            _clueZoom.CloseRequested -= OnClueZoomCloseRequested;
            _clueZoom.ClueRead -= OnClueRead;

            _overlayPanels.VisibleChanged -= OnOverlayVisibilityChanged;

            _hudController.Dispose();
            _space.Dispose();
            _clueZoom.Dispose();
        }
    }
}
