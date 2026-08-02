using GameName.UI.Journal;
using GameName.UI.MemoryRoom;
using GameName.UI.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.AnalysisRoom
{
    // 분석실 화면의 구성 루트. GameSessionBootstrap이 이미 조립해 둔 GameSession을
    // 참조로 받아 그 위에 이 화면의 View/Controller만 얹는다 — Core 객체를
    // 여기서 새로 만들지 않는다.
    //
    // 이동 + 정신력 표시는 기억 방 화면에서 이미 만든 RoomNavigationPanelView/
    // Controller를 그대로 재사용한다 — 그래프 위 어느 노드에서 시작하든
    // 동작하도록 이미 일반화되어 있어서, 분석실 전용으로 새로 만들 이유가 없다.
    [RequireComponent(typeof(UIDocument))]
    public sealed class AnalysisRoomBootstrap : MonoBehaviour
    {
        [SerializeField] private GameSessionBootstrap _gameSession;

        // 기록지는 다른 화면과 마찬가지로 별도 UIDocument로 띄운다. 같은
        // JournalVisibilityController를 재사용한다 — 화면마다 새로 만들지 않는다.
        [SerializeField] private JournalVisibilityController _journalVisibility;

        private RoomNavigationPanelController _navigationController;
        private AnalysisPanelController _analysisController;
        private JournalScreenController _journalScreenController;

        private void OnEnable()
        {
            var session = _gameSession.Session;
            var root = GetComponent<UIDocument>().rootVisualElement;

            var navigationView = new RoomNavigationPanelView(root.Q<VisualElement>("navigation-panel"));
            _navigationController = new RoomNavigationPanelController(
                navigationView, session.Graph, session.RestorationTracker, session.MentalityGauge,
                session.MovementProcessor, session.PlayerLocation, session.RoomIds, session.EventBus);

            var analysisView = new AnalysisPanelView(root.Q<VisualElement>("analysis-panel"));
            _analysisController = new AnalysisPanelController(
                analysisView, session.Inventory, session.AnalysisProgress, session.ClueAnalyzer,
                session.MentalityCostSettings);

            if (_journalVisibility != null)
            {
                var journalView = new JournalScreenView(_journalVisibility.Document.rootVisualElement);
                _journalScreenController = new JournalScreenController(journalView, session.Journal);
                _journalVisibility.Initialize(_journalScreenController);
            }
        }

        private void OnDisable()
        {
            _navigationController?.Dispose();
            _navigationController = null;

            _analysisController?.Dispose();
            _analysisController = null;

            _journalScreenController?.Dispose();
            _journalScreenController = null;
        }
    }
}
