using System;
using UnityEngine.UIElements;

namespace GameName.UI.Flow
{
    // 복귀 확인 패널의 화면 요소 구성과 표시 갱신만 담당한다.
    //
    // 되돌릴 수 없는 전환이므로 버튼 한 번으로 바로 실행하지 않는다 — "현실로
    // 복귀"를 누르면 경고와 확정/취소 버튼이 나타나고(시향 패널의 확인 절차와
    // 같은 패턴), "복귀 확정"을 눌러야 실제로 ExitConfirmed가 올라간다.
    public sealed class ReturnToRealityPanelView
    {
        private readonly VisualElement _root;
        private readonly VisualElement _confirmControls;
        private readonly Button _requestButton;
        private readonly Button _confirmButton;
        private readonly Button _cancelButton;
        private readonly Label _summaryLabel;

        public event Action ExitRequested;
        public event Action ExitConfirmed;
        public event Action ExitCancelled;

        public ReturnToRealityPanelView(VisualElement root)
        {
            _root = root;
            _confirmControls = root.Q<VisualElement>("exit-confirm-controls");
            _requestButton = root.Q<Button>("request-exit-button");
            _confirmButton = root.Q<Button>("confirm-exit-button");
            _cancelButton = root.Q<Button>("cancel-exit-button");
            _summaryLabel = root.Q<Label>("exit-summary-notice");

            _requestButton.clicked += () => ExitRequested?.Invoke();
            _confirmButton.clicked += () => ExitConfirmed?.Invoke();
            _cancelButton.clicked += () => ExitCancelled?.Invoke();

            SetConfirmVisible(false);
        }

        public void SetVisible(bool visible) => _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        public void SetConfirmVisible(bool visible) =>
            _confirmControls.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        public void SetSummary(string message)
        {
            _summaryLabel.text = message ?? string.Empty;
            _summaryLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
