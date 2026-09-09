using System;
using UnityEngine.UIElements;

namespace GameName.UI.Inventory
{
    // 가방 단서 패널의 요소 구성. 버튼이 눌릴 수 있는지, 왜 막혔는지는 하나도
    // 판단하지 않는다 — 컨트롤러가 정한 결과를 받아 그리고, 버튼이 눌렸다는
    // 사실만 알린다.
    //
    // 인벤토리 UIDocument 안의 하위 요소라 오버레이 라우터를 거치지 않는다 —
    // 가방을 연 채로 그 위에 겹쳐 뜬다.
    public sealed class ClueUsePanelView : IClueUsePanelView
    {
        private readonly VisualElement _panel;
        private readonly Label _title;
        private readonly Button _discardButton;
        private readonly Label _discardReason;
        private readonly Label _result;

        public event Action Discard;
        public event Action Closed;

        public ClueUsePanelView(VisualElement inventoryRoot)
        {
            _panel = inventoryRoot.Q<VisualElement>("clue-use-panel");
            _title = inventoryRoot.Q<Label>("clue-use-title");
            _discardButton = inventoryRoot.Q<Button>("clue-use-discard-button");
            _discardReason = inventoryRoot.Q<Label>("clue-use-discard-reason");
            _result = inventoryRoot.Q<Label>("clue-use-result");

            _discardButton.clicked += () => Discard?.Invoke();
            inventoryRoot.Q<Button>("clue-use-close-button").clicked += () => Closed?.Invoke();

            Close();
        }

        public bool IsOpen => _panel.style.display == DisplayStyle.Flex;

        public void Open(string title)
        {
            _title.text = title ?? string.Empty;
            SetResult(null);
            _panel.style.display = DisplayStyle.Flex;
        }

        public void Close() => _panel.style.display = DisplayStyle.None;

        public void SetDiscardAction(bool discardEnabled, string discardReason)
        {
            _discardButton.SetEnabled(discardEnabled);
            SetReason(_discardReason, discardEnabled ? null : discardReason);
        }

        public void SetResult(string message)
        {
            _result.text = message ?? string.Empty;
            _result.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private static void SetReason(Label label, string message)
        {
            label.text = message ?? string.Empty;
            label.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
