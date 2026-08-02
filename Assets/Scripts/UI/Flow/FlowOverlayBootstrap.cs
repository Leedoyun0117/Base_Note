using GameName.UI.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.Flow
{
    // 대화/복귀 확인/복귀 후 안내 오버레이의 구성 루트. 기록지와 마찬가지로
    // 화면 전환 대상이 아니라 항상 활성 상태인 별도 UIDocument다 — 전환기
    // (SceneScreenSwitcher)는 이 오버레이를 켜고 끄지 않는다.
    [RequireComponent(typeof(UIDocument))]
    public sealed class FlowOverlayBootstrap : MonoBehaviour
    {
        [SerializeField] private GameSessionBootstrap _gameSession;
        [SerializeField] private DialogueAdvanceInput _dialogueInput;

        private FlowOverlayController _controller;

        private void OnEnable()
        {
            var session = _gameSession.Session;
            var root = GetComponent<UIDocument>().rootVisualElement;

            // UIDocument가 자동으로 만드는 이 root 자체는 화면 전체를 덮는
            // 컨테이너지만 UXML 쪽이 아니라서 FlowOverlay.uss의
            // picking-mode: Ignore가 적용되지 않는다. 이걸 그대로 두면 대화/
            // 복귀 확인 패널이 전부 숨겨져 있어도 이 보이지 않는 root가 화면
            // 전체의 클릭을 계속 가로채, 아래 조향실/기억 방 화면의 버튼이
            // 전혀 눌리지 않게 된다. 실제로 보이는 하위 패널(대화/복귀 확인)은
            // 각자 기본 피킹 모드라 이 설정과 무관하게 정상적으로 클릭을 받는다.
            root.pickingMode = PickingMode.Ignore;

            _controller = new FlowOverlayController(
                root, session.CommissionSession, session.DialogueProgressor, session.MentalityGauge,
                session.PlayerLocation, session.MemoryEntryNodeId, session.EventBus);

            if (_dialogueInput != null)
                _dialogueInput.Initialize(_controller.DialogueController);
        }

        private void OnDisable()
        {
            if (_dialogueInput != null)
                _dialogueInput.Initialize(null);

            _controller?.Dispose();
            _controller = null;
        }
    }
}
