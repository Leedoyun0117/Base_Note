using GameName.UI.Perfumery;
using GameName.UI.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.FinalCrafting
{
    // 최종 조향 화면의 구성 루트. 다른 화면 Bootstrap과 같은 이유로 Core 객체는
    // 여기서 조립하지 않는다 — 씬에 하나뿐인 GameSessionBootstrap이 이미 만든
    // GameSession을 참조로 받아 그 위에 View/Controller만 얹는다.
    [RequireComponent(typeof(UIDocument))]
    public sealed class FinalCraftingBootstrap : MonoBehaviour
    {
        [SerializeField] private GameSessionBootstrap _gameSession;

        private FinalCraftingScreenController _screenController;

        private void OnEnable()
        {
            var session = _gameSession.Session;
            var root = GetComponent<UIDocument>().rootVisualElement;

            // 방 선택 패널은 조향실 화면의 RoomSelectionPanelView/Controller를
            // 그대로 재사용한다 — "방을 골라 요구 총량을 확인한다"는 동작 자체는
            // 조향실과 다르지 않다.
            var roomView = new RoomSelectionPanelView(root.Q<VisualElement>("room-panel"));
            var roomController = new RoomSelectionPanelController(
                roomView, session.RoomIds, session.PublicInfoRepository, session.RestorationTracker, session.EventBus);

            var craftingView = new PerfumeryCompositionPanelView(root.Q<VisualElement>("perfumery-panel"));
            var craftingController = new FinalCraftingPanelController(
                craftingView, session.CompositionValidator, session.FinalCraftingProcessor, session.FinalCraftingBoard);

            var submitView = new FinalCraftingSubmitPanelView(root.Q<VisualElement>("submit-panel"));
            var submitController = new FinalCraftingSubmitPanelController(
                submitView, session.FinalCraftingBoard, session.RoomIds, session.CommissionCompletionProcessor,
                () => session.CurrentCommissionData.RewardTable);

            _screenController = new FinalCraftingScreenController(roomController, craftingController, submitController);
        }

        private void OnDisable()
        {
            _screenController?.Dispose();
            _screenController = null;
        }
    }
}
