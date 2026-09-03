using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom
{
    // 방 위에 얇게 겹치는 상태 표시줄. 조작 안내와 한 줄짜리 상황 문구, 그리고
    // 신뢰·남은 추출 자원·색별 기억제 보유 수를 담당한다 — 규칙은 하나도
    // 판단하지 않고 받은 값을 문구로 바꿔 보여줄 뿐이다.
    public sealed class MemoryRoomHudView
    {
        private readonly Label _keyHintLabel;
        private readonly Label _messageLabel;
        private readonly Label _trustLabel;
        private readonly Label _extractionLabel;
        private readonly Label _walletRedLabel;
        private readonly Label _walletGreenLabel;
        private readonly Label _walletBlueLabel;

        public MemoryRoomHudView(VisualElement root)
        {
            _keyHintLabel = root.Q<Label>("hud-key-hints");
            _messageLabel = root.Q<Label>("hud-message");
            _trustLabel = root.Q<Label>("hud-trust");
            _extractionLabel = root.Q<Label>("hud-extraction");
            _walletRedLabel = root.Q<Label>("hud-wallet-r");
            _walletGreenLabel = root.Q<Label>("hud-wallet-g");
            _walletBlueLabel = root.Q<Label>("hud-wallet-b");
        }

        public void SetKeyHints(string hints) => _keyHintLabel.text = hints ?? string.Empty;

        // 이동 실패 사유나 출입구 안내처럼 플레이어에게 보여줄 한 줄. 빈 문구는
        // 요소를 아예 감춘다 — 빈 상자가 방 위에 남아 있지 않게 하기 위함이다.
        public void SetMessage(string message)
        {
            _messageLabel.text = message ?? string.Empty;
            _messageLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void SetTrust(int trust) => _trustLabel.text = $"신뢰 {trust}";

        public void SetExtraction(int remaining) => _extractionLabel.text = $"추출 자원 {remaining}";

        // 세 색을 한 번에 받는다 — 지갑은 색 하나가 바뀌어도 셋을 함께 다시
        // 그리는 편이 "어느 색이 바뀌었는가"를 화면이 추적하지 않게 한다.
        public void SetWallet(int red, int green, int blue)
        {
            _walletRedLabel.text = $"R {red}";
            _walletGreenLabel.text = $"G {green}";
            _walletBlueLabel.text = $"B {blue}";
        }
    }
}
