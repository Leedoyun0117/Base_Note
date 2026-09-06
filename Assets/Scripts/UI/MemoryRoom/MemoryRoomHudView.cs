using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom
{
    // 방 위에 얇게 겹치는 상태 표시줄. 조작 안내와 한 줄짜리 상황 문구, 그리고
    // 신뢰·히로민 게이지·기회·색별 추출한 기억 수를 담당한다 — 규칙은 하나도
    // 판단하지 않고 받은 값을 문구·막대로 바꿔 보여줄 뿐이다.
    //
    // "다음 기억으로" 조작 자체는 여기 없다 — 가방 화면의 레버(MemoryMoveLeverController)가
    // 맡는다. 이 게이지는 그 레버를 당길지 판단하는 데 참고하는 정보 표시일 뿐이다.
    public sealed class MemoryRoomHudView
    {
        private const string BelowThresholdClass = "hud-hiromi-fill--below-threshold";

        private readonly Label _keyHintLabel;
        private readonly Label _messageLabel;
        private readonly Label _trustLabel;
        private readonly Label _hiromiValueLabel;
        private readonly VisualElement _hiromiFill;
        private readonly VisualElement _hiromiMarker;
        private readonly Label _chanceLabel;
        private readonly Label _walletRedLabel;
        private readonly Label _walletGreenLabel;
        private readonly Label _walletBlueLabel;

        public MemoryRoomHudView(VisualElement root)
        {
            _keyHintLabel = root.Q<Label>("hud-key-hints");
            _messageLabel = root.Q<Label>("hud-message");
            _trustLabel = root.Q<Label>("hud-trust");
            _hiromiValueLabel = root.Q<Label>("hud-hiromi-value");
            _hiromiFill = root.Q<VisualElement>("hud-hiromi-fill");
            _hiromiMarker = root.Q<VisualElement>("hud-hiromi-marker");
            _chanceLabel = root.Q<Label>("hud-chance");
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

        // current: 지금 보유한 히로민. threshold: 이동 문턱(RunDefinition.MoveHiromiCost) —
        // 막대 위 세로선 위치와, 채움이 그 밑으로 내려갔을 때의 색 전환 기준
        // 둘 다 이 값 하나로 정해진다.
        //
        // 막대는 "문턱의 두 배"를 꽉 찬 것으로 본다 — 히로민에 정해진 상한이
        // 없어 100%에 대응할 절대값이 필요한데, 문턱의 두 배로 잡으면 문턱이
        // 막대의 정확히 절반에 그려져 "지금 위인가 아래인가"가 항상 한눈에
        // 들어온다. 그 이상은 막대가 꽉 찬 채로 더 늘지 않지만, 숫자는 옆
        // 라벨이 그대로 보여 준다.
        public void SetHiromi(int current, int threshold)
        {
            _hiromiValueLabel.text = $"히로민 {current}";

            var displayCeiling = threshold > 0 ? threshold * 2 : 1;
            var fillPercent = Clamp01((float)current / displayCeiling) * 100f;
            var markerPercent = Clamp01((float)threshold / displayCeiling) * 100f;

            _hiromiFill.style.width = new StyleLength(new Length(fillPercent, LengthUnit.Percent));
            _hiromiMarker.style.left = new StyleLength(new Length(markerPercent, LengthUnit.Percent));

            if (current < threshold)
                _hiromiFill.AddToClassList(BelowThresholdClass);
            else
                _hiromiFill.RemoveFromClassList(BelowThresholdClass);
        }

        public void SetChance(int remaining) => _chanceLabel.text = $"기회 {remaining}";

        // 세 색을 한 번에 받는다 — 손에 든 기억은 하나가 바뀌어도 셋을 함께
        // 다시 그리는 편이 "어느 색이 바뀌었는가"를 화면이 추적하지 않게 한다.
        public void SetWallet(int red, int green, int blue)
        {
            _walletRedLabel.text = $"R {red}";
            _walletGreenLabel.text = $"G {green}";
            _walletBlueLabel.text = $"B {blue}";
        }

        private static float Clamp01(float value) => value < 0f ? 0f : (value > 1f ? 1f : value);
    }
}
