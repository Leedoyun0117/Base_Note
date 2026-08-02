using UnityEngine;
using UnityEngine.InputSystem;

namespace GameName.UI.Flow
{
    // 스페이스 키로 대화를 넘긴다. JournalVisibilityController와 같은 이유로
    // 레거시 UnityEngine.Input이 아니라 새 Input System(Keyboard.current)을
    // 쓴다 — 이 프로젝트는 Active Input Handling이 "Input System Package"
    // 단독이라 레거시 Input 클래스를 호출하면 예외가 난다.
    //
    // 버튼 클릭과 똑같은 동작(DialoguePanelController.RequestAdvance)을
    // 그대로 호출한다 — 입력 수단이 늘어난다고 판단 로직이 늘어나지 않는다.
    public sealed class DialogueAdvanceInput : MonoBehaviour
    {
        private DialoguePanelController _controller;

        public void Initialize(DialoguePanelController controller) => _controller = controller;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                _controller?.RequestAdvance();
        }
    }
}
