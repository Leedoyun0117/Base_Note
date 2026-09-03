using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom.Space
{
    // 신뢰가 깎이면 방이 좌우에서 안쪽으로 닫히는 연출을 그리는 자리.
    //
    // UI Toolkit 요소다 — 씬이 아니라 화면에 고정된 프레임이라, 카메라가 흔들려도
    // 마스크는 그대로 있고 방 안 콘텐츠만 떨린다.
    //
    // 게임 규칙은 하나도 계산하지 않는다 — 받은 가시 비율 하나로 좌우 판의 폭만
    // 정한다. 좁아지는 애니메이션은 이 클래스가 아니라 USS의 transition이 한다
    // (지속 시간은 MemoryRoomScreen.uss의 --mask-shrink-duration 한 곳).
    //
    // 콜라이더 비활성(가시 밖 단서를 못 집게)은 이 뷰와 무관하다 — 그 판정은
    // MemoryRoomSpaceController가 같은 비율을 받아 IClueAccessPolicy로 따로 한다.
    // 이 판은 순수하게 시각 장식이므로 화면 폭 기준의 간단한 비율이면 충분하다.
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

        // 가시 비율 v면 방 한가운데(0.5)에서 양쪽으로 v/2씩 보이고, 그 바깥
        // (1 - v) 만큼을 판이 덮는다. 화면 폭 기준으로 각 판은 (1 - v)/2 를 덮는다.
        // v가 1이면 폭 0 — 판이 사라진다.
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
