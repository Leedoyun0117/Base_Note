using System.Collections.Generic;
using System.Text;
using GameName.Core.Complexes;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom
{
    // 방 위에 얇게 겹치는 상태 표시줄. 조작 안내와 한 줄짜리 상황 문구, 그리고
    // 턴 진행·안정 축·활성 컴플렉스 수를 담당한다 — 규칙은 하나도 판단하지
    // 않고 받은 값을 문구로 바꿔 보여줄 뿐이다.
    //
    // 3차 개편으로 표시 내용이 바뀌었지만 일부 UXML 요소 이름은 그대로
    // 재활용한다 (hud-trust → 턴, hud-chance → 컴플렉스). 안정 축 게이지만
    // 옛 히로민 게이지 자리를 hud-stability-*로 새로 잡았다. 요소가 없어도
    // 죽지 않도록 조회는 전부 null 허용으로 둔다.
    public sealed class MemoryRoomHudView
    {
        private readonly Label _keyHintLabel;
        private readonly Label _messageLabel;
        private readonly Label _turnLabel;
        private readonly Label _stabilityLabel;
        private readonly VisualElement _stabilityFill;
        private readonly VisualElement _stabilityBaseline;
        private readonly Label _complexLabel;

        public MemoryRoomHudView(VisualElement root)
        {
            _keyHintLabel = root.Q<Label>("hud-key-hints");
            _messageLabel = root.Q<Label>("hud-message");
            _turnLabel = root.Q<Label>("hud-trust");
            _stabilityLabel = root.Q<Label>("hud-stability-value");
            _stabilityFill = root.Q<VisualElement>("hud-stability-fill");
            _stabilityBaseline = root.Q<VisualElement>("hud-stability-baseline");
            _complexLabel = root.Q<Label>("hud-chance");
        }

        public void SetKeyHints(string hints)
        {
            if (_keyHintLabel != null) _keyHintLabel.text = hints ?? string.Empty;
        }

        public void SetMessage(string message)
        {
            if (_messageLabel == null) return;
            _messageLabel.text = message ?? string.Empty;
            _messageLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void SetTurn(int current, int target)
        {
            if (_turnLabel != null) _turnLabel.text = $"턴 {current} / {target}";
        }

        // 안정 축을 수치 + 양방향 게이지로 그린다. 0(안정)은 축 범위 안에서의
        // 자리(보통 중앙)에 기준선이 오고, 음수면 채움이 그 왼쪽으로, 양수면
        // 오른쪽으로 벌어진다. 범위는 축이 데이터로 들고 있는 값 그대로 받는다.
        public void SetStability(int position, int min, int max)
        {
            if (_stabilityLabel != null)
            {
                var sign = position > 0 ? "+" : string.Empty;
                _stabilityLabel.text = $"안정 {sign}{position}";
            }

            var span = max - min;
            if (span <= 0) return;

            var zeroPct = (0f - min) / span * 100f;
            var posPct = Mathf.Clamp((position - min) / (float)span * 100f, 0f, 100f);
            var left = Mathf.Min(zeroPct, posPct);
            var width = Mathf.Abs(posPct - zeroPct);

            if (_stabilityBaseline != null)
                _stabilityBaseline.style.left = Length.Percent(zeroPct);

            if (_stabilityFill != null)
            {
                _stabilityFill.style.left = Length.Percent(left);
                _stabilityFill.style.width = Length.Percent(width);
            }
        }

        // 활성 컴플렉스를 "이름 (남은 턴) — 설명" 한 줄씩 늘어놓는다.
        // 폴리싱 없이 라벨 하나에 개행으로만 쌓는다.
        public void SetComplexes(IReadOnlyList<ActiveComplex> active)
        {
            if (_complexLabel == null) return;

            if (active == null || active.Count == 0)
            {
                _complexLabel.text = "컴플렉스 없음";
                return;
            }

            var sb = new StringBuilder("컴플렉스");
            foreach (var entry in active)
            {
                sb.Append("\n· ").Append(entry.Definition.DisplayName)
                  .Append(" (").Append(entry.RemainingTurns).Append("턴)");

                var description = entry.Definition.Description;
                if (!string.IsNullOrEmpty(description))
                    sb.Append(" — ").Append(description);
            }

            _complexLabel.text = sb.ToString();
        }
    }
}
