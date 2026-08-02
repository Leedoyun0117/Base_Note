using System;
using UnityEngine.UIElements;

namespace GameName.UI.FinalCrafting
{
    // 제출 패널의 화면 요소 구성과 표시 갱신만 담당한다. "모든 방이 채워졌는지"
    // 자체는 판단하지 않는다 — 컨트롤러가 넘겨준 상태를 그대로 그릴 뿐이다.
    public sealed class FinalCraftingSubmitPanelView
    {
        private readonly Label _statusLabel;
        private readonly Button _provideButton;
        private readonly Label _resultLabel;

        public event Action ProvideRequested;

        public FinalCraftingSubmitPanelView(VisualElement root)
        {
            _statusLabel = root.Q<Label>("completion-status-label");
            _provideButton = root.Q<Button>("provide-button");
            _resultLabel = root.Q<Label>("provide-result-message");

            _provideButton.clicked += () => ProvideRequested?.Invoke();
        }

        public void SetStatus(int filledCount, int totalCount) =>
            _statusLabel.text = $"{filledCount} / {totalCount} 방 완료";

        public void SetProvideButtonEnabled(bool enabled) => _provideButton.SetEnabled(enabled);

        public void SetResultMessage(string message)
        {
            _resultLabel.text = message ?? string.Empty;
            _resultLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
