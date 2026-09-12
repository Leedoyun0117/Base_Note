using System.Collections.Generic;
using System.Text;
using DG.Tweening;
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
    //
    // 게이지는 세 겹이다: 트랙(고정) + 중앙 기준선(고정, 0=안정 자리) + 채움
    // (기준선에서 값 자리까지 색으로 벌어지는 막대, 얼마나 벌어졌는지 한눈에
    // 보이라고) + 그 끝에 얹히는 포인터 마커(정확한 값 자리를 콕 집어 준다).
    // 채움·마커 자리·색은 값이 바뀔 때 DOTween으로 함께 부드럽게 보간한다
    // (플레이 중에만 — 에디터·테스트에서는 즉시 반영). AnimateStabilityGauge 참고.
    public sealed class MemoryRoomHudView
    {
        private readonly Label _keyHintLabel;
        private readonly Label _messageLabel;
        private readonly Label _turnLabel;
        private readonly Label _stabilityLabel;
        private readonly VisualElement _stabilityFill;
        private readonly VisualElement _stabilityMarker;
        private readonly VisualElement _stabilityBaseline;
        private readonly Label _complexLabel;

        // 채움·마커가 뚝 끊기지 않게 목표치까지 보간하는 데 걸리는 시간.
        private const float StabilityTweenSeconds = 0.35f;

        // 방향 색의 극단값 — MemoryRoomScreen.uss의 --memory-blue / --memory-red와
        // 같은 값. 침체(왼쪽·음수)는 항상 파랑, 흥분(오른쪽·양수)은 항상 빨강으로
        // 고정이고, 중앙(0)에 가까울수록 옅어지다가(흰색 쪽) 극단으로 갈수록
        // 이 색으로 진해진다 — 두 색이 서로 섞이지는 않는다.
        private static readonly Color StabilityNegativeColor = new Color32(0x5B, 0x8B, 0xC7, 0xFF);
        private static readonly Color StabilityPositiveColor = new Color32(0xC7, 0x5B, 0x4A, 0xFF);

        // 지금 그려져 있는 채움의 자리·폭(%), 마커의 자리(%), 공통 색.
        // 트윈이 이 넷을 목표치로 민다.
        private float _stabilityFillLeftPercent = 50f;
        private float _stabilityFillWidthPercent;
        private float _stabilityMarkerLeftPercent = 50f;
        private Color _stabilityColor = StabilityNegativeColor;
        private Tween _stabilityTween;

        public MemoryRoomHudView(VisualElement root)
        {
            _keyHintLabel = root.Q<Label>("hud-key-hints");
            _messageLabel = root.Q<Label>("hud-message");
            _turnLabel = root.Q<Label>("hud-trust");
            _stabilityLabel = root.Q<Label>("hud-stability-value");
            _stabilityFill = root.Q<VisualElement>("hud-stability-fill");
            _stabilityMarker = root.Q<VisualElement>("hud-stability-marker");
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

        // 안정 축을 수치 + 채움 막대 + 포인터 마커로 그린다. 중앙 기준선(0=안정)
        // 은 고정이고, 음수면 채움이 그 왼쪽으로, 양수면 오른쪽으로 벌어지며
        // 그 끝에 마커가 값 자리를 콕 집어 준다. 범위는 축이 데이터로 들고
        // 있는 값 그대로 받는다.
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

            if (_stabilityBaseline != null)
                _stabilityBaseline.style.left = Length.Percent(zeroPct);

            var targetFillLeft = Mathf.Min(zeroPct, posPct);
            var targetFillWidth = Mathf.Abs(posPct - zeroPct);
            var targetColor = DirectionColor(position, min, max);

            AnimateStabilityGauge(targetFillLeft, targetFillWidth, posPct, targetColor);
        }

        // 음수(침체)는 항상 파랑, 양수(흥분)는 항상 빨강 — 두 색은 서로 섞이지
        // 않는다. 대신 그 극(min/max)에 얼마나 가까운지를 세기(0=옅음·흰색
        // ~ 1=그 색 그대로)로 삼아, 중앙 근처는 옅고 극단으로 갈수록 진해진다.
        private static Color DirectionColor(int position, int min, int max)
        {
            if (position > 0)
            {
                var intensity = max > 0 ? Mathf.Clamp01(position / (float)max) : 0f;
                return Color.LerpUnclamped(Color.white, StabilityPositiveColor, intensity);
            }

            if (position < 0)
            {
                var intensity = min < 0 ? Mathf.Clamp01(position / (float)min) : 0f;
                return Color.LerpUnclamped(Color.white, StabilityNegativeColor, intensity);
            }

            return Color.white;
        }

        // 채움·마커를 목표 자리·색으로 옮긴다. 플레이 중에는 DOTween으로
        // 0.35초에 걸쳐 보간하고(값이 연속으로 바뀌면 이전 트윈을 죽이고 지금
        // 자리에서 다시 시작한다), 에디터·테스트에서는 즉시 반영한다.
        //
        // DOTween의 UI Toolkit 모듈은 Assembly-CSharp에만 컴파일돼 이 어셈블리
        // (GameName.UI)에서 못 쓴다 — 그래서 style을 직접 트윈하는 대신 0~1
        // 진행값만 DOVirtual로 굴리고 매 콜백에서 style을 다시 쓴다.
        private void AnimateStabilityGauge(
            float targetFillLeft, float targetFillWidth, float targetMarkerLeft, Color targetColor)
        {
            if (_stabilityFill == null && _stabilityMarker == null) return;

            _stabilityTween?.Kill();
            _stabilityTween = null;

            if (!Application.isPlaying)
            {
                _stabilityFillLeftPercent = targetFillLeft;
                _stabilityFillWidthPercent = targetFillWidth;
                _stabilityMarkerLeftPercent = targetMarkerLeft;
                _stabilityColor = targetColor;
                ApplyStabilityGauge();
                return;
            }

            var fromFillLeft = _stabilityFillLeftPercent;
            var fromFillWidth = _stabilityFillWidthPercent;
            var fromMarkerLeft = _stabilityMarkerLeftPercent;
            var fromColor = _stabilityColor;

            _stabilityTween = DOVirtual
                .Float(0f, 1f, StabilityTweenSeconds, t =>
                {
                    _stabilityFillLeftPercent = Mathf.LerpUnclamped(fromFillLeft, targetFillLeft, t);
                    _stabilityFillWidthPercent = Mathf.LerpUnclamped(fromFillWidth, targetFillWidth, t);
                    _stabilityMarkerLeftPercent = Mathf.LerpUnclamped(fromMarkerLeft, targetMarkerLeft, t);
                    _stabilityColor = Color.LerpUnclamped(fromColor, targetColor, t);
                    ApplyStabilityGauge();
                })
                .SetEase(Ease.OutCubic)
                .OnKill(() => _stabilityTween = null);
        }

        private void ApplyStabilityGauge()
        {
            if (_stabilityFill != null)
            {
                _stabilityFill.style.left = Length.Percent(_stabilityFillLeftPercent);
                _stabilityFill.style.width = Length.Percent(_stabilityFillWidthPercent);
                _stabilityFill.style.backgroundColor = _stabilityColor;
            }

            if (_stabilityMarker != null)
            {
                _stabilityMarker.style.left = Length.Percent(_stabilityMarkerLeftPercent);
                _stabilityMarker.style.backgroundColor = _stabilityColor;
            }
        }

        // 화면을 내릴 때 돌던 트윈을 정리한다.
        public void CancelStabilityAnimation()
        {
            _stabilityTween?.Kill();
            _stabilityTween = null;
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
