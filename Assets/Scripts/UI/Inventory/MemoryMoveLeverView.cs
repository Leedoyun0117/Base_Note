using System;
using UnityEngine.UIElements;

namespace GameName.UI.Inventory
{
    // "다음 기억으로" 레버의 요소 구성. 히로민이 충분한지, 강제 이동이 맞는
    // 선택인지는 하나도 판단하지 않는다 — 컨트롤러가 정한 결과를 받아 그리고,
    // 버튼이 눌렸다는 사실만 알린다.
    public sealed class MemoryMoveLeverView : IMemoryMoveLeverView
    {
        private readonly Button _moveButton;
        private readonly VisualElement _confirm;
        private readonly Label _confirmText;

        public event Action MoveClicked;
        public event Action ForceConfirmed;
        public event Action ForceCancelled;

        public MemoryMoveLeverView(VisualElement inventoryRoot)
        {
            _moveButton = inventoryRoot.Q<Button>("memory-move-button");
            _confirm = inventoryRoot.Q<VisualElement>("memory-move-confirm");
            _confirmText = inventoryRoot.Q<Label>("memory-move-confirm-text");

            _moveButton.clicked += () => MoveClicked?.Invoke();
            inventoryRoot.Q<Button>("memory-move-confirm-force").clicked += () => ForceConfirmed?.Invoke();
            inventoryRoot.Q<Button>("memory-move-confirm-cancel").clicked += () => ForceCancelled?.Invoke();

            HideForceConfirm();
        }

        public void SetMoveEnabled(bool enabled) => _moveButton.SetEnabled(enabled);

        public void ShowForceConfirm(string message)
        {
            _confirmText.text = message ?? string.Empty;
            _confirm.style.display = DisplayStyle.Flex;
        }

        public void HideForceConfirm() => _confirm.style.display = DisplayStyle.None;
    }
}
