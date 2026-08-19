using System;
using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom
{
    // 상단 상태 표시줄에서 "열기" 버튼과 조작 안내만 담당한다. 현재 방·정신력
    // 표시는 같은 줄에 있지만 RoomNavigationPanelView의 몫이라 건드리지 않는다
    // — 그쪽은 조향실·분석실 화면과 공유하는 View이기 때문이다.
    //
    // 시향 버튼은 시향이 실제로 가능할 때만 보인다. 가진 앰플이 없거나 이 방의
    // 것이 아니면 눌러도 아무 것도 못 하는 버튼이라, 보여 주는 것 자체가
    // "여기서 뭔가 할 수 있다"는 잘못된 신호가 된다.
    public sealed class MemoryRoomHudView
    {
        private readonly Button _mapButton;
        private readonly Button _scentTestButton;
        private readonly Label _keyHintLabel;

        public event Action MapRequested;
        public event Action ScentTestRequested;

        public MemoryRoomHudView(VisualElement root)
        {
            _mapButton = root.Q<Button>("hud-map-button");
            _scentTestButton = root.Q<Button>("hud-scent-button");
            _keyHintLabel = root.Q<Label>("hud-key-hints");

            _mapButton.clicked += () => MapRequested?.Invoke();
            _scentTestButton.clicked += () => ScentTestRequested?.Invoke();

            SetScentTestAvailable(false);
        }

        public bool IsScentTestButtonVisible => _scentTestButton.style.display.value == DisplayStyle.Flex;

        public void SetKeyHints(string hints) => _keyHintLabel.text = hints ?? string.Empty;

        public void SetScentTestAvailable(bool available) =>
            _scentTestButton.style.display = available ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
