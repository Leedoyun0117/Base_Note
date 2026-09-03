using System;
using GameName.Core.Clues;
using GameName.UI.ClueZoom;
using GameName.UI.Inventory;
using GameName.UI.MemoryRoom.Dialogue;
using GameName.UI.MemoryRoom.Space;
using GameName.UI.Overlays;
using GameName.UI.Restoration;

namespace GameName.UI.MemoryRoom
{
    // 기억 방 화면을 이루는 조각들을 조립하고, 조각을 넘나드는 정보만 중개한다.
    //
    // 조각들:
    //   · 2D 씬(MemoryRoomSpaceController) — 방 공간과 단서 오브젝트
    //   · 마스크(MemoryRoomMaskController) — 신뢰에 따라 방을 좌우에서 닫는다
    //   · 상단 바(MemoryRoomHudController) — 신뢰·추출 자원·기억제 보유 수
    //   · 하단 대화 패널(DialoguePanelController) — 검열 렌더링된 대사와 선택지
    //   · 필요할 때만 뜨는 오버레이 — 가방(인벤토리)·단서 설명 창
    //   · 가방 안의 사물 용도 선택(ClueUsePanelController)
    //   · 가방 오버레이의 두 번째 탭 — 복원도(RestorationScreenController),
    //     탭 전환은 OverlayTabController
    // 이들을 서로 잇는 일은 전부 여기서 한다. 각 조각은 서로를 알지 못한다.
    //
    // 규칙은 하나도 계산하지 않는다 — 각 컨트롤러가 이미 Core에서 얻은 결론을
    // 받아 "그러면 어느 조각을 다시 그려야 하는가"만 정한다.
    public sealed class MemoryRoomScreenController : IDisposable
    {
        private readonly MemoryRoomHudView _hud;
        private readonly MemoryRoomHudController _hudController;
        private readonly MemoryRoomSpaceController _space;
        private readonly MemoryRoomMaskController _mask;
        private readonly MemoryRoomCameraShakeController _cameraShake;
        private readonly DialoguePanelController _dialoguePanel;
        private readonly ClueZoomScreenController _clueZoom;
        private readonly InventoryScreenController _inventory;
        private readonly ClueUsePanelController _clueUsePanel;
        private readonly RestorationScreenController _restoration;
        private readonly OverlayTabController _overlayTabs;
        private readonly OverlayPanelHost _overlayPanels;

        public MemoryRoomScreenController(
            MemoryRoomHudView hud,
            MemoryRoomHudController hudController,
            MemoryRoomSpaceController space,
            MemoryRoomMaskController mask,
            MemoryRoomCameraShakeController cameraShake,
            DialoguePanelController dialoguePanel,
            ClueZoomScreenController clueZoom,
            InventoryScreenController inventory,
            ClueUsePanelController clueUsePanel,
            RestorationScreenController restoration,
            OverlayTabController overlayTabs,
            OverlayPanelHost overlayPanels)
        {
            _hud = hud ?? throw new ArgumentNullException(nameof(hud));
            _hudController = hudController ?? throw new ArgumentNullException(nameof(hudController));
            _space = space ?? throw new ArgumentNullException(nameof(space));
            _mask = mask ?? throw new ArgumentNullException(nameof(mask));
            _cameraShake = cameraShake ?? throw new ArgumentNullException(nameof(cameraShake));
            _dialoguePanel = dialoguePanel ?? throw new ArgumentNullException(nameof(dialoguePanel));
            _clueZoom = clueZoom ?? throw new ArgumentNullException(nameof(clueZoom));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _clueUsePanel = clueUsePanel ?? throw new ArgumentNullException(nameof(clueUsePanel));
            _restoration = restoration ?? throw new ArgumentNullException(nameof(restoration));
            _overlayTabs = overlayTabs ?? throw new ArgumentNullException(nameof(overlayTabs));
            _overlayPanels = overlayPanels ?? throw new ArgumentNullException(nameof(overlayPanels));

            _space.ClueActivated += OnClueActivated;
            _space.MessageChanged += _hud.SetMessage;

            // 마스크가 계산한 가시 비율로 방 단서 접근성을 함께 움직인다.
            _mask.VisibleRatioChanged += _space.SetVisibleRatio;
            _space.SetVisibleRatio(_mask.CurrentRatio);

            _clueZoom.CloseRequested += OnClueZoomCloseRequested;
            _clueZoom.ClueStored += OnClueStored;

            // 가방에서 단서를 고르면 용도 선택 패널을 연다. 소비되면 격자를 다시 그린다.
            _inventory.ClueUseRequested += _clueUsePanel.Open;
            _clueUsePanel.ClueConsumed += _inventory.Refresh;

            // 전체 화면 오버레이가 떠 있는 동안에는 뒤의 방을 건드릴 수 없다.
            _overlayPanels.VisibleChanged += OnOverlayVisibilityChanged;

            ApplyOverlayBlocking();
        }

        // 씬에서 단서를 눌렀다 — 설명 창을 띄운다.
        private void OnClueActivated(ClueInfo clue)
        {
            _clueZoom.Open(clue);
            _overlayPanels.Show(OverlayPanel.ClueZoom);
        }

        private void OnClueZoomCloseRequested() => _overlayPanels.Hide(OverlayPanel.ClueZoom);

        // 설명 창이 실제로 숨겨진 시점에 불린다.
        public void OnClueZoomHidden() => _clueZoom.OnHidden();

        private void OnOverlayVisibilityChanged(OverlayPanel? visible) => ApplyOverlayBlocking();

        private void ApplyOverlayBlocking() => _space.SetInteractionEnabled(!_overlayPanels.IsAnyVisible);

        private void OnClueStored()
        {
            // 습득한 단서는 방에서 사라지고 가방에 나타난다. 습득은 이벤트를
            // 발행하지 않으므로(그 자리에서 결과가 바로 나온다) 두 조각을
            // 여기서 직접 다시 그리게 한다.
            _space.Refresh();
            _inventory.Refresh();
        }

        public void Dispose()
        {
            _space.ClueActivated -= OnClueActivated;
            _space.MessageChanged -= _hud.SetMessage;

            _mask.VisibleRatioChanged -= _space.SetVisibleRatio;

            _clueZoom.CloseRequested -= OnClueZoomCloseRequested;
            _clueZoom.ClueStored -= OnClueStored;

            _inventory.ClueUseRequested -= _clueUsePanel.Open;
            _clueUsePanel.ClueConsumed -= _inventory.Refresh;

            _overlayPanels.VisibleChanged -= OnOverlayVisibilityChanged;

            _hudController.Dispose();
            _mask.Dispose();
            _cameraShake.Dispose();
            _dialoguePanel.Dispose();
            _space.Dispose();
            _clueZoom.Dispose();
            _inventory.Dispose();
            _clueUsePanel.Dispose();
            _restoration.Dispose();
            _overlayTabs.Dispose();
        }
    }
}
