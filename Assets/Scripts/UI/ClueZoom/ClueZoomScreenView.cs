using System;
using GameName.Core.Clues;
using UnityEngine.UIElements;

namespace GameName.UI.ClueZoom
{
    // 단서 설명 창의 요소 구성. 담을 수 있는지, 왜 실패했는지는 하나도
    // 판단하지 않는다 — 컨트롤러가 Core에게 물어 얻은 결론이고, 이 View는
    // 버튼이 눌렸다는 사실만 알린다.
    //
    // 인벤토리 칸을 그리지 않는다. 습득 시 가방은 열리지 않고, 습득한 단서
    // 확인은 대화 도중 가방(인벤토리 오버레이)에서 한다.
    public sealed class ClueZoomScreenView
    {
        private readonly VisualElement _stage;
        private readonly Label _nameLabel;
        private readonly Label _kindLabel;
        private readonly Label _messageLabel;

        // 이 화면을 연 누름이 그대로 닫기로 이어지지 않게 막는 빗장.
        private bool _acceptsStageClose;

        // 나가기 버튼을 눌렀거나 카드 바깥(무대)을 눌렀다.
        public event Action ExitRequested;

        // [수집] 버튼을 눌렀다.
        public event Action CollectRequested;

        public ClueZoomScreenView(VisualElement root)
        {
            _stage = root.Q<VisualElement>("clue-zoom-stage");
            _nameLabel = root.Q<Label>("clue-zoom-clue-name");
            _kindLabel = root.Q<Label>("clue-zoom-clue-kind");
            _messageLabel = root.Q<Label>("clue-zoom-message");

            root.Q<Button>("clue-zoom-exit-button").clicked += () => ExitRequested?.Invoke();
            root.Q<Button>("clue-zoom-collect-button").clicked += () => CollectRequested?.Invoke();

            // 빈 공간(무대 자신)을 눌렀을 때만 닫는다. 다만 이 화면을 연 그 누름으로는
            // 닫지 않는다 — 방에서 단서를 누르는 순간 이 화면이 떠오르는데, 같은
            // 프레임에 UI 쪽으로도 그 누름이 전달되면 열리자마자 닫힌다. 손을 한 번
            // 뗀 뒤부터 닫기를 받는다.
            _stage.RegisterCallback<PointerUpEvent>(_ => _acceptsStageClose = true);
            _stage.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (_acceptsStageClose && ReferenceEquals(evt.target, _stage))
                    ExitRequested?.Invoke();
            });
        }

        public void SetClue(ClueInfo clue)
        {
            // 열릴 때마다 빗장을 다시 건다 — 이번 화면을 연 누름은 아직 끝나지 않았다.
            _acceptsStageClose = false;

            var kindText = clue.Kind == ClueKind.Poster ? "벽에 붙은 포스터" : "바닥에 떨어진 물건";

            // 이름이 비어 있으면(저작 누락) 종류를 이름 자리에 대신 보여준다.
            var hasName = !string.IsNullOrEmpty(clue.DisplayName);
            _nameLabel.text = hasName ? clue.DisplayName : kindText;
            _kindLabel.text = hasName ? kindText : string.Empty;
            _kindLabel.style.display = hasName ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetMessage(string message)
        {
            _messageLabel.text = message ?? string.Empty;
            _messageLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
