using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom.Dialogue
{
    // 대화 패널의 요소 구성과 표시 갱신. 어느 선택지가 옳은지 판단하지 않는다 —
    // 화자·원문을 그대로 그리고, 선택지/단서 버튼을 만든다.
    //
    // 선택지 자리에는 두 종류가 온다: 텍스트 선택지 버튼, 그리고 ClueSelection
    // 줄의 단서 답변 버튼. 둘 다 같은 컨테이너(dialogue-choices)에 그리고,
    // 클래스로만 구분한다.
    public sealed class DialoguePanelView : IDialoguePanelView
    {
        private readonly Label _speakerLabel;
        private readonly VisualElement _body;
        private readonly VisualElement _choices;
        private readonly Label _noticeLabel;

        public event Action<ChoiceId> ChoiceClicked;
        public event Action<ClueId> ClueAnswerClicked;
        public event Action<ClueId> ExtractClueClicked;
        public event Action SkipClueAnswerClicked;

        public DialoguePanelView(VisualElement root)
        {
            _speakerLabel = root.Q<Label>("dialogue-speaker");
            _body = root.Q<VisualElement>("dialogue-body");
            _choices = root.Q<VisualElement>("dialogue-choices");
            _noticeLabel = root.Q<Label>("dialogue-notice");

            SetNotice(null);
        }

        public void SetLine(string speaker, string authoredText)
        {
            _speakerLabel.text = speaker ?? string.Empty;
            _speakerLabel.style.display =
                string.IsNullOrEmpty(speaker) ? DisplayStyle.None : DisplayStyle.Flex;

            _body.Clear();
            _body.Add(PlainSegment(authoredText ?? string.Empty));
        }

        public void SetChoices(IReadOnlyList<KeyValuePair<ChoiceId, string>> choices)
        {
            _choices.Clear();

            foreach (var choice in choices)
            {
                var id = choice.Key;
                var button = new Button(() => ChoiceClicked?.Invoke(id))
                {
                    text = string.IsNullOrEmpty(choice.Value) ? "(선택)" : choice.Value,
                };
                button.AddToClassList("dialogue-choice");
                _choices.Add(button);
            }
        }

        public void SetClueSelection(IReadOnlyList<SelectableClue> clues)
        {
            _choices.Clear();

            var header = new Label("가진 단서로 답하세요.");
            header.AddToClassList("dialogue-clue-prompt");
            _choices.Add(header);

            if (clues.Count == 0)
            {
                var none = new Label("지금 들고 있는 단서가 없습니다.");
                none.AddToClassList("dialogue-clue-prompt");
                _choices.Add(none);
            }

            foreach (var clue in clues)
            {
                var id = clue.Id;
                var name = string.IsNullOrEmpty(clue.DisplayName) ? id.Value : clue.DisplayName;

                var row = new VisualElement();
                row.AddToClassList("dialogue-clue-row");

                var answer = new Button(() => ClueAnswerClicked?.Invoke(id)) { text = name };
                answer.AddToClassList("dialogue-choice");
                answer.AddToClassList("dialogue-choice--clue-answer");
                row.Add(answer);

                // 아직 추출하지 않은 단서에는 그 자리에서 기억을 추출하는 버튼을
                // 붙인다 — 히로민을 쓰고, 성공하면 목록이 다시 그려지며 사라진다.
                if (!clue.MemoryExtracted)
                {
                    var extract = new Button(() => ExtractClueClicked?.Invoke(id)) { text = "기억 추출" };
                    extract.AddToClassList("dialogue-choice");
                    extract.AddToClassList("dialogue-choice--extract");
                    row.Add(extract);
                }

                _choices.Add(row);
            }

            // 하드 블록이 되지 않게, 단서로 답하지 않고 넘어가는 길을 항상 둔다.
            var skip = new Button(() => SkipClueAnswerClicked?.Invoke()) { text = "잘 기억나지 않는다" };
            skip.AddToClassList("dialogue-choice");
            skip.AddToClassList("dialogue-choice--skip");
            _choices.Add(skip);
        }

        public void SetNotice(string message)
        {
            _noticeLabel.text = message ?? string.Empty;
            _noticeLabel.style.display =
                string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private static Label PlainSegment(string text)
        {
            var label = new Label(text);
            label.AddToClassList("dialogue-segment");
            label.pickingMode = PickingMode.Ignore;
            return label;
        }
    }
}
