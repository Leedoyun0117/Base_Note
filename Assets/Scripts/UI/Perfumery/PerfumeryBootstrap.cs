using GameName.UI.Journal;
using GameName.UI.MemoryRoom;
using GameName.UI.Session;
using GameName.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.Perfumery
{
    // 조향실 화면 + 기록지 화면을 함께 띄우는 구성 루트.
    //
    // Core 객체는 더 이상 여기서 조립하지 않는다 — 씬에 하나만 있는
    // GameSessionBootstrap이 이미 만들어 둔 GameSession을 참조로 받아, 그 위에
    // 이 화면의 View/Controller만 얹는다. 그래야 기억 방 화면 등 다른 화면과
    // 정신력/인벤토리/위치 같은 상태를 실제로 공유한다.
    [RequireComponent(typeof(UIDocument))]
    public sealed class PerfumeryBootstrap : MonoBehaviour
    {
        [SerializeField] private GameSessionBootstrap _gameSession;

        // 기록지는 화면 전체를 덮는 별도 UIDocument(별도 GameObject)로 띄운다.
        // 씬에 그 GameObject를 만들고 JournalVisibilityController를 붙인 뒤
        // 여기에 연결해야 한다.
        [SerializeField] private JournalVisibilityController _journalVisibility;

        private PerfumeryScreenController _screenController;
        private JournalScreenController _journalScreenController;

        private void OnEnable()
        {
            var session = _gameSession.Session;
            var root = GetComponent<UIDocument>().rootVisualElement;

            // 조향실도 그래프 위의 노드 하나다 — 계단·분석실로 나가는 이동은
            // 기억 방/분석실 화면과 같은 지도 조합(MemoryMapView +
            // MemoryRoomMapNavigationController)을 그대로 재사용한다. 조향
            // 목표 방 선택은 이동과 다른 동작이라 별도로 둔다 — 최종 조향
            // 화면과 같은 RoomSelectionPanelView/Controller(목록)를 재사용한다.
            var navigationView = new RoomNavigationPanelView(root.Q<VisualElement>("room-panel"));
            var mapView = new MemoryMapView(root);
            var navigationController = new MemoryRoomMapNavigationController(
                navigationView, mapView, session.Graph, session.RestorationTracker, session.MentalityGauge,
                session.MentalityCostSettings, session.MovementProcessor, session.PlayerLocation, session.RoomIds,
                session.CommissionSession, session.MemoryExitNodeId, session.EventBus);

            var roomView = new RoomSelectionPanelView(root.Q<VisualElement>("room-panel"));
            var roomController = new RoomSelectionPanelController(
                roomView, session.RoomIds, session.PublicInfoRepository, session.RestorationTracker, session.EventBus);

            var compositionView = new PerfumeryCompositionPanelView(root.Q<VisualElement>("perfumery-panel"));
            var compositionController = new PerfumeryCompositionPanelController(
                compositionView, session.CompositionValidator, session.AmpouleStorage,
                session.AmpouleCraftingQueue, session.MentalityGauge, session.MentalityCostSettings, session.EventBus);

            var statusView = new StatusPanelView(root.Q<VisualElement>("status-panel"));
            var statusController = new StatusPanelController(
                statusView, session.MentalityGauge, session.AmpouleStorage, session.Inventory,
                session.TransferProcessor, session.EventBus);

            _screenController = new PerfumeryScreenController(
                navigationController, roomController, compositionController, statusController,
                session.CraftingProcessor);

            // ── 기록지 화면 조립 ────────────────────────────────────────
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
