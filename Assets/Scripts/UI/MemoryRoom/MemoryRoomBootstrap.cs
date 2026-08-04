using GameName.UI.Journal;
using GameName.UI.Session;
using GameName.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom
{
    // 기억 방 화면의 구성 루트. GameSessionBootstrap이 이미 조립해 둔
    // GameSession을 참조로 받아 그 위에 이 화면의 View/Controller만 얹는다 —
    // Core 객체를 여기서 새로 만들지 않는다(PerfumeryBootstrap과 상태를
    // 공유하기 위함).
    [RequireComponent(typeof(UIDocument))]
    public sealed class MemoryRoomBootstrap : MonoBehaviour
    {
        [SerializeField] private GameSessionBootstrap _gameSession;

        // 기록지는 조향실 화면과 마찬가지로 별도 UIDocument로 띄운다. 같은
        // JournalVisibilityController를 재사용한다 — 화면마다 새로 만들지 않는다.
        [SerializeField] private JournalVisibilityController _journalVisibility;

        private MemoryRoomScreenController _screenController;
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

            var clueView = new ClueCollectionPanelView(root.Q<VisualElement>("clue-panel"));
            var clueController = new ClueCollectionPanelController(
                clueView, session.PlayerLocation, session.ClueTracker, session.ClueCollector,
                session.RoomIds, session.EventBus);

            var inventoryView = new MemoryRoomInventoryPanelView(root.Q<VisualElement>("inventory-panel"));
            var inventoryController = new MemoryRoomInventoryPanelController(
                inventoryView, session.Inventory, session.ClueReturnProcessor);

            var testingView = new ScentTestingPanelView(root.Q<VisualElement>("scent-test-panel"));
            var testingController = new ScentTestingPanelController(
                testingView, session.PlayerLocation, session.Inventory, session.ScentTestingProcessor,
                session.RestorationTracker, session.RoomIds, session.EventBus);

            _screenController = new MemoryRoomScreenController(
                navigationController, clueController, inventoryController, testingController);

            if (_journalVisibility != null)
            {
                var journalView = new JournalScreenView(_journalVisibility.Document.rootVisualElement);
                _journalScreenController = new JournalScreenController(journalView, session.Journal);
                _journalVisibility.Initialize(_journalScreenController);
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
