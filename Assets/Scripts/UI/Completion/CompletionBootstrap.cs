using GameName.UI.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.Completion
{
    // 완료 화면의 구성 루트. 다른 화면과 같은 이유로 Core 객체는 여기서 조립하지
    // 않는다 — GameSessionBootstrap이 이미 만든 GameSession 위에 View/Controller만
    // 얹는다.
    [RequireComponent(typeof(UIDocument))]
    public sealed class CompletionBootstrap : MonoBehaviour
    {
        [SerializeField] private GameSessionBootstrap _gameSession;

        private CompletionScreenController _screenController;

        private void OnEnable()
        {
            var session = _gameSession.Session;
            var root = GetComponent<UIDocument>().rootVisualElement;

            var view = new CompletionScreenView(root);
            _screenController = new CompletionScreenController(
                view,
                session.DisplayCollection,
                session.UpgradeShop,
                () => session.LastCompletionResult,
                () => session.CurrentCommissionData,
                session.AllCommissions,
                session.LoadCommission);
        }

        private void OnDisable()
        {
            _screenController?.Dispose();
            _screenController = null;
        }
    }
}
