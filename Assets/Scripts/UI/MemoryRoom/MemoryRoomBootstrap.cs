using GameName.UI.ClueZoom;
using GameName.UI.Inventory;
using GameName.UI.Journal;
using GameName.UI.MemoryRoom.Space;
using GameName.UI.Overlays;
using GameName.UI.Session;
using GameName.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom
{
    // 기억 방 화면의 구성 루트. GameSessionBootstrap이 이미 조립해 둔
    // GameSession을 참조로 받아 그 위에 이 화면의 View/Controller만 얹는다 —
    // Core 객체를 여기서 새로 만들지 않는다.
    //
    // 이 화면은 이제 두 가지 표시 수단을 함께 쓴다: 방 자체는 2D 씬 오브젝트로,
    // 상태·지도·시향과 오버레이는 UI Toolkit으로 그린다. 그 둘을 잇는 배선도
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

        // 기록지·인벤토리·확대 화면의 가시성을 한 곳에서 관리하는 컴포넌트.
        [SerializeField] private OverlayPanelHost _overlayPanels;

        // 시향 결과 문구가 화면에 남아 있는 시간(초). 연출 감각의 문제라
        // 코드에 박지 않고 인스펙터에서 조정한다.
        [SerializeField] private float _scentResultDisplaySeconds = 4f;

        private MemoryRoomScreenController _screenController;
        private JournalScreenController _journalScreenController;

        private void OnEnable()
        {
            var session = _gameSession.Session;
            var root = GetComponent<UIDocument>().rootVisualElement;
            var layout = _layoutAsset.ToLayout();

            // ── HUD(씬 위에 겹치는 패널) ────────────────────────────────
            // 상태 표시(방 이름·정신력·안내)가 상단 바 여기저기로 흩어져 예전의
            // "navigation-panel" 상자가 없어졌다. 이 View는 이름으로만 요소를
            // 찾으므로 문서 루트를 그대로 넘긴다 — MemoryMapView가 미리보기와
            // 확대 지도를 함께 찾으려고 루트를 받는 것과 같은 이유다.
            var navigationView = new RoomNavigationPanelView(root);
            var mapView = new MemoryMapView(root);
            var navigationController = new MemoryRoomMapNavigationController(
                navigationView, mapView, session.Graph, session.RestorationTracker, session.MentalityGauge,
                session.MentalityCostSettings, session.MovementProcessor, session.PlayerLocation, session.RoomIds,
                session.CommissionSession, session.MemoryExitNodeId, session.EventBus);

            var hudView = new MemoryRoomHudView(root);
            hudView.SetKeyHints(OverlayKeyHint.Describe(
                _overlayPanels.KeyNameOf(OverlayPanel.Journal),
                _overlayPanels.KeyNameOf(OverlayPanel.Inventory)));

            var testingView = new ScentTestingPanelView(
                root.Q<VisualElement>("scent-test-panel"), _scentResultDisplaySeconds);
            var testingController = new ScentTestingPanelController(
                testingView, session.PlayerLocation, session.Inventory, session.ScentTestingProcessor,
                session.RestorationTracker, session.RoomIds, session.EventBus);

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
                navigationController, hudView, mapView, testingController, spaceController, zoomController,
                inventoryController, _overlayPanels);

            _overlayPanels.Bind(OverlayPanel.Inventory, inventoryController.Refresh);
            _overlayPanels.Bind(OverlayPanel.ClueZoom, onShown: null, onHidden: _screenController.OnClueZoomHidden);

            // ── 기록지 ──────────────────────────────────────────────────
            var journalRoot = _overlayPanels.RootOf(OverlayPanel.Journal);
            if (journalRoot != null)
            {
                var journalView = new JournalScreenView(journalRoot);
                _journalScreenController = new JournalScreenController(journalView, session.Journal);
                _overlayPanels.Bind(OverlayPanel.Journal, _journalScreenController.Refresh);
            }
        }

        private void OnDisable()
        {
            // 오버레이 쪽에 걸어 둔 대리자를 먼저 끊는다 — 이 화면이 꺼진 뒤에도
            // 남아 있으면 이미 정리된 컨트롤러를 가리키게 된다.
            if (_overlayPanels != null)
            {
                _overlayPanels.Bind(OverlayPanel.Inventory, null);
                _overlayPanels.Bind(OverlayPanel.ClueZoom, null);
                _overlayPanels.Bind(OverlayPanel.Journal, null);
            }

            _screenController?.Dispose();
            _screenController = null;

            _journalScreenController?.Dispose();
            _journalScreenController = null;
        }
    }
}
