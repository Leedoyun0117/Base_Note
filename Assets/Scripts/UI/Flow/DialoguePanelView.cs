using System;
using GameName.Core.Journal;
using UnityEngine.UIElements;

namespace GameName.UI.Flow
{
    // 대화 패널의 화면 요소 구성과 표시 갱신만 담당한다. 지금이 마지막 줄인지,
    // 기억으로 들어갈 수 있는지는 판단하지 않는다 — 컨트롤러가 그 결론(버튼
    // 문구)만 넘겨준다.
    public sealed class DialoguePanelView
    {
        private readonly VisualElement _root;
        private readonly Label _speakerLabel;
        private readonly Label _textLabel;
        private readonly Button _advanceButton;

        public event Action AdvanceRequested;

        public DialoguePanelView(VisualElement root)
        {
            _root = root;
            _speakerLabel = root.Q<Label>("dialogue-speaker");
            _textLabel = root.Q<Label>("dialogue-text");
            _advanceButton = root.Q<Button>("dialogue-advance-button");

            _advanceButton.clicked += () => AdvanceRequested?.Invoke();
        }

        public void SetVisible(bool visible) => _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        public void SetLine(DialogueLine line)
        {
            _speakerLabel.text = line.Speaker;
            _textLabel.text = line.Text;
        }

        public void SetAdvanceButtonText(string text) => _advanceButton.text = text;
    }
}
