using GameName.UI.ClueZoom;
using GameName.UI.MemoryRoom.Space;
using GameName.UI.Overlays;
using GameName.UI.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom
{
    // 기억 방 화면의 구성 루트. GameSessionBootstrap이 이미 조립해 둔
    // GameSession을 참조로 받아 그 위에 이 화면의 View/Controller만 얹는다 —
    // Core 객체를 여기서 새로 만들지 않는다.
    //
    // 이 화면은 두 가지 표시 수단을 함께 쓴다: 방 자체는 2D 씬 오브젝트로,
    // 상단 바·스토리 패널은 UI Toolkit으로 그린다. 그 둘을 잇는 배선도 전부
    // 여기 한 곳에서 이뤄진다.
    //
    // 3차 개편: 신뢰 마스크·카메라 흔들림·대화 패널·가방·복원도는 전부 빠졌다.
    // 라운드 전환 계기는 "지정 턴 수를 버텼다"(RoundSurvivedEvent → RunProgressor)다.
    [RequireComponent(typeof(UIDocument))]
    public sealed class MemoryRoomBootstrap : MonoBehaviour
    {
        [SerializeField] private GameSessionBootstrap _gameSession;

        // 씬에 그려지는 방.
        [SerializeField] private MemoryRoomSpaceView _spaceView;

        // 방 치수는 씬이 아니라 에셋에서 온다.
        [SerializeField] private MemoryRoomLayoutAsset _layoutAsset;

        // 스토리 패널의 가시성을 관리하는 컴포넌트.
        [SerializeField] private OverlayPanelHost _overlayPanels;

        private MemoryRoomScreenController _screenController;

        private void OnEnable()
        {
            var session = _gameSession.Session;
            var root = GetComponent<UIDocument>().rootVisualElement;
            var layout = _layoutAsset.ToLayout();

            // ── HUD(상단 상태 표시줄) ───────────────────────────────────
            var hudView = new MemoryRoomHudView(root);
            hudView.SetKeyHints(OverlayKeyHint.Describe(_overlayPanels.KeyNameOf(OverlayPanel.Inventory)));
            var hudController = new MemoryRoomHudController(
                hudView, session.Turns, session.Stability, session.ActiveComplexes, session.EventBus);

            // ── 2D 씬 ─────────────────────────────────────────────────
            var spaceController = new MemoryRoomSpaceController(
                _spaceView, layout, session.ClueTracker, session.ClueState,
                session.CurrentRoomId, session.EventBus);

            // ── 오버레이: 스토리 패널 ──────────────────────────────────
            var zoomView = new ClueZoomScreenView(_overlayPanels.RootOf(OverlayPanel.ClueZoom));
            var zoomController = new ClueZoomScreenController(zoomView, session.ClueUse, session.EventBus);

            _screenController = new MemoryRoomScreenController(
                hudView, hudController, spaceController, zoomController, _overlayPanels);

            _overlayPanels.Bind(OverlayPanel.ClueZoom, onShown: null, onHidden: _screenController.OnClueZoomHidden);
        }

        private void OnDisable()
        {
            if (_overlayPanels != null)
                _overlayPanels.Bind(OverlayPanel.ClueZoom, null);

            _screenController?.Dispose();
            _screenController = null;
        }
    }
}
