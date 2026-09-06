using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom.Space
{
    // 신뢰가 깎이면 방이 좌우에서 안쪽으로 닫히는 연출.
    //
    // 이 판 자체는 이제 아무것도 칠하지 않는다 — 덮인 영역의 시각 처리(도트
    // 열화 + 종이 찢은 경계선)는 URP 풀스크린 셰이더(DotTexture)가 담당한다.
    // 이 클래스가 하는 일은 좌우 판의 폭을 USS transition으로 애니메이션하고,
    // 그 애니메이션 중인 실제 폭을 CoverFraction으로 노출하는 것뿐이다.
    // MemoryRoomBootstrap이 매 프레임 CoverFraction을 셰이더 전역값으로 흘려,
    // 셰이더 경계도 UI와 똑같은 속도로 열리고 닫힌다.
    //
    // UI Toolkit 요소라 화면에 고정된다 — 방이 흔들려도 이 폭 계산은 그대로다.
    //
    // 콜라이더 비활성(가시 밖 단서를 못 집게)은 이 뷰와 무관하다 — 그 판정은
    // MemoryRoomSpaceController가 같은 비율을 받아 IClueAccessPolicy로 따로 한다.
    public sealed class MemoryRoomMaskView
    {
        private readonly VisualElement _leftPanel;
        private readonly VisualElement _rightPanel;

        // 마지막으로 적용된(=애니메이션의 목표) 가시 비율. 애니메이션 도중 값이
        // 아니라 목표값이다 — 씬 없이 검증하는 쪽이 이 값을 본다.
        public float LastAppliedRatio { get; private set; } = 1f;

        public MemoryRoomMaskView(VisualElement root)
        {
            _leftPanel = root.Q<VisualElement>("mask-left");
            _rightPanel = root.Q<VisualElement>("mask-right");
        }

        // 지금 화면 좌우가 합쳐서 덮고 있는 비율(0..1). 레이아웃이 잡힌 뒤에는
        // USS transition이 애니메이션 중인 실제 판 폭을 읽고, 아직 잡히기 전이면
        // 목표값(1 - LastAppliedRatio)으로 되돌아간다.
        public float CoverFraction
        {
            get
            {
                var target = Mathf.Clamp01(1f - LastAppliedRatio);
                if (_leftPanel == null)
                    return target;

                var width = _leftPanel.resolvedStyle.width;
                var parentWidth = _leftPanel.parent?.resolvedStyle.width ?? 0f;
                if (float.IsNaN(width) || parentWidth <= 0f)
                    return target;

                // 한쪽 판 폭은 화면 폭의 (1 - v)/2 → 두 배가 좌우 합친 덮임 비율.
                return Mathf.Clamp01(2f * width / parentWidth);
            }
        }

        // 가시 비율 v면 방 한가운데(0.5)에서 양쪽으로 v/2씩 보이고, 그 바깥
        // (1 - v) 만큼을 판이 덮는다. 화면 폭 기준으로 각 판은 (1 - v)/2 를 덮는다.
        // v가 1이면 폭 0.
        public void SetVisibleRatio(float visibleRatio)
        {
            LastAppliedRatio = visibleRatio;

            if (_leftPanel == null || _rightPanel == null)
                return;

            var v = visibleRatio < 0f ? 0f : (visibleRatio > 1f ? 1f : visibleRatio);
            var coverPercent = (1f - v) * 50f;

            _leftPanel.style.width = Length.Percent(coverPercent);
            _rightPanel.style.width = Length.Percent(coverPercent);
        }
    }
}
