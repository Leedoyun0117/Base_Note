using GameName.UI.ClueZoom;
using GameName.UI.Inventory;
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
    // 상태 표시줄과 오버레이는 UI Toolkit으로 그린다. 그 둘을 잇는 배선도
    // 전부 여기 한 곳에서 이뤄지고, 씬 오브젝트는 어디서도 Core 처리기를
    // 직접 참조하지 않는다.
    [RequireComponent(typeof(UIDocument))]
    public sealed class MemoryRoomBootstrap : MonoBehaviour
    {
        [SerializeField] private GameSessionBootstrap _gameSession;

        // 씬에 그려지는 방. HUD와 같은 GameObject에 둘 이유가 없어 따로 받는다.
        [SerializeField] private MemoryRoomSpaceView _spaceView;

        // 방 치수는 씬이 아니라 에셋에서 온다 — 코드에도 씬에도 숫자를 박지
        // 않기 위함이다.
        [SerializeField] private MemoryRoomLayoutAsset _layoutAsset;

        // 인벤토리·확대 화면의 가시성을 한 곳에서 관리하는 컴포넌트.
        [SerializeField] private OverlayPanelHost _overlayPanels;

        private MemoryRoomScreenController _screenController;

        private void OnEnable()
        {
            var session = _gameSession.Session;
            var root = GetComponent<UIDocument>().rootVisualElement;
            var layout = _layoutAsset.ToLayout();

            // ── HUD(씬 위에 겹치는 상태 표시줄) ─────────────────────────
            var hudView = new MemoryRoomHudView(root);
            hudView.SetKeyHints(OverlayKeyHint.Describe(_overlayPanels.KeyNameOf(OverlayPanel.Inventory)));

            // ── 2D 씬 ───────────────────────────────────────────────────
            var spaceController = new MemoryRoomSpaceController(
                _spaceView, layout, session.PlayerLocation, session.Graph, session.ClueTracker,
                session.RoomIds, session.MovementProcessor, session.DroppedCluePositions, session.EventBus);

            // ── 오버레이 화면 ────────────────────────────────────────────
            var zoomView = new ClueZoomScreenView(_overlayPanels.RootOf(OverlayPanel.ClueZoom));
            var zoomController = new ClueZoomScreenController(zoomView, session.Inventory, session.ClueCollector);

            var inventoryView = new InventoryScreenView(_overlayPanels.RootOf(OverlayPanel.Inventory));
            var inventoryController = new InventoryScreenController(
                inventoryView, session.Inventory, session.ClueDropProcessor);

            _screenController = new MemoryRoomScreenController(
                hudView, spaceController, zoomController, inventoryController, _overlayPanels);

            _overlayPanels.Bind(OverlayPanel.Inventory, inventoryController.Refresh);
            _overlayPanels.Bind(OverlayPanel.ClueZoom, onShown: null, onHidden: _screenController.OnClueZoomHidden);
        }

        private void OnDisable()
        {
            // 오버레이 쪽에 걸어 둔 대리자를 먼저 끊는다 — 이 화면이 꺼진 뒤에도
            // 남아 있으면 이미 정리된 컨트롤러를 가리키게 된다.
            if (_overlayPanels != null)
            {
                _overlayPanels.Bind(OverlayPanel.Inventory, null);
                _overlayPanels.Bind(OverlayPanel.ClueZoom, null);
            }

            _screenController?.Dispose();
            _screenController = null;
        }
    }
}
