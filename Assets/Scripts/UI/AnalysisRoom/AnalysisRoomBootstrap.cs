using GameName.UI.Journal;
using GameName.UI.Overlays;
using GameName.UI.MemoryRoom;
using GameName.UI.Session;
using GameName.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.AnalysisRoom
{
    // 분석실 화면의 구성 루트. GameSessionBootstrap이 이미 조립해 둔 GameSession을
    // 참조로 받아 그 위에 이 화면의 View/Controller만 얹는다 — Core 객체를
    // 여기서 새로 만들지 않는다.
    //
    // 이동은 기억 방 화면에서 이미 만든 MemoryMapView/MemoryRoomMapNavigationController를
    // 그대로 재사용한다 — 그래프 위 어느 노드에서 시작하든 동작하도록 이미
    // 일반화되어 있어서, 분석실 전용으로 새로 만들 이유가 없다.
    [RequireComponent(typeof(UIDocument))]
    public sealed class AnalysisRoomBootstrap : MonoBehaviour
    {
        [SerializeField] private GameSessionBootstrap _gameSession;

        // 기록지는 화면 전체를 덮는 별도 UIDocument(별도 GameObject)로 띄운다.
        // 그 가시성은 이 화면이 아니라 씬에 하나뿐인 OverlayPanelHost가 관리한다
        // — 화면마다 자기 오버레이를 따로 여닫으면 인벤토리 오버레이가 생긴
        // 지금 두 화면이 동시에 뜨는 것을 막을 곳이 없어지기 때문이다.
        [SerializeField] private OverlayPanelHost _overlayPanels;

        private AnalysisRoomScreenController _screenController;
        private JournalScreenController _journalScreenController;

        private void OnEnable()
        {
            var session = _gameSession.Session;
            var root = GetComponent<UIDocument>().rootVisualElement;

            var navigationView = new RoomNavigationPanelView(root.Q<VisualElement>("navigation-panel"));
            var mapView = new MemoryMapView(root);
            var navigationController = new MemoryRoomMapNavigationController(
                navigationView, mapView, session.Graph, session.RestorationTracker, session.MentalityGauge,
                session.MentalityCostSettings, session.MovementProcessor, session.PlayerLocation, session.RoomIds,
                session.CommissionSession, session.MemoryExitNodeId, session.EventBus);

            var analysisView = new AnalysisPanelView(root.Q<VisualElement>("analysis-panel"));
            var analysisController = new AnalysisPanelController(
                analysisView, session.Inventory, session.AnalysisProgress, session.ClueAnalyzer,
                session.MentalityGauge, session.MentalityCostSettings, session.EventBus);

            var storageView = new ClueStoragePanelView(root.Q<VisualElement>("clue-storage-panel"));
            var storageController = new ClueStoragePanelController(
                storageView, session.Inventory, session.ClueStorage, session.AnalysisProgress,
                session.ClueTransferProcessor);

            _screenController = new AnalysisRoomScreenController(navigationController, analysisController, storageController);

            var journalRoot = _overlayPanels == null ? null : _overlayPanels.RootOf(OverlayPanel.Journal);
            if (journalRoot != null)
            {
                var journalView = new JournalScreenView(journalRoot);
                _journalScreenController = new JournalScreenController(journalView, session.Journal);
                _overlayPanels.Bind(OverlayPanel.Journal, _journalScreenController.Refresh);
            }
        }

        private void OnDisable()
        {
            _screenController?.Dispose();
            _screenController = null;

            _journalScreenController?.Dispose();
            _journalScreenController = null;
        }
    }
}
