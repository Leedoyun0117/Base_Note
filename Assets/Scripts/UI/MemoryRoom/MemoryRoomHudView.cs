using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom
{
    // 방 위에 얇게 겹치는 상태 표시줄. 조작 안내와 한 줄짜리 상황 문구만
    // 담당한다 — 규칙은 하나도 판단하지 않고 받은 문자열을 그대로 보여준다.
    public sealed class MemoryRoomHudView
    {
        private readonly Label _keyHintLabel;
        private readonly Label _messageLabel;

        public MemoryRoomHudView(VisualElement root)
        {
            _keyHintLabel = root.Q<Label>("hud-key-hints");
            _messageLabel = root.Q<Label>("hud-message");
        }

        public void SetKeyHints(string hints) => _keyHintLabel.text = hints ?? string.Empty;

        // 이동 실패 사유나 출입구 안내처럼 플레이어에게 보여줄 한 줄. 빈 문구는
        // 요소를 아예 감춘다 — 빈 상자가 방 위에 남아 있지 않게 하기 위함이다.
        public void SetMessage(string message)
        {
            _messageLabel.text = message ?? string.Empty;
            _messageLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
