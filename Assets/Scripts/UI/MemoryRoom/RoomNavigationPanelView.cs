using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom
{
    // 이동 패널의 화면 요소 구성과 표시 갱신만 담당한다. 현재 위치/정신력
    // 표시만 그린다 — 어디로 갈 수 있는지는 이제 지도(MemoryMapView)가 전부
    // 보여주므로, 이 View는 목록을 그리지 않는다.
    public sealed class RoomNavigationPanelView
    {
        private readonly Label _currentRoomLabel;
        private readonly Label _restoredBadge;
        private readonly VisualElement _mentalityBarFill;
        private readonly Label _mentalityValueLabel;
        private readonly Label _mentalityNoticeLabel;
        private readonly Label _availableActionsLabel;
        private readonly Label _moveFailureLabel;

        public RoomNavigationPanelView(VisualElement root)
        {
            _currentRoomLabel = root.Q<Label>("current-room-label");
            _restoredBadge = root.Q<Label>("current-room-restored-badge");
            _mentalityBarFill = root.Q<VisualElement>("mentality-bar-fill");
            _mentalityValueLabel = root.Q<Label>("mentality-value");
            _mentalityNoticeLabel = root.Q<Label>("mentality-notice");
            _availableActionsLabel = root.Q<Label>("mentality-affordance-notice");
            _moveFailureLabel = root.Q<Label>("move-failure-message");
        }

        public void SetCurrentRoom(string label, bool isRestored)
        {
            _currentRoomLabel.text = label;
            _restoredBadge.style.display = isRestored ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetMentality(int current, int max)
        {
            _mentalityValueLabel.text = $"{current} / {max}";

            var ratio = max <= 0 ? 0f : (float)current / max;
            _mentalityBarFill.style.width = Length.Percent(Mathf.Clamp01(ratio) * 100f);
        }

        public void SetMentalityNotice(string message)
        {
            _mentalityNoticeLabel.text = message ?? string.Empty;
            _mentalityNoticeLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // 잔량으로 지금 무엇을 할 수 있는지 미리 알려준다 — 숫자만 보고 매번
        // 계산하지 않아도 되게 한다. 문구는 컨트롤러가 이미 Core(Mentality
        // AffordabilityCalculator)로 얻은 결론을 그대로 옮긴 것이다.
        public void SetAvailableActions(string message)
        {
            if (_availableActionsLabel == null)
                return;

            _availableActionsLabel.text = message ?? string.Empty;
        }

        public void SetMoveFailureMessage(string message)
        {
            _moveFailureLabel.text = message ?? string.Empty;
            _moveFailureLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
