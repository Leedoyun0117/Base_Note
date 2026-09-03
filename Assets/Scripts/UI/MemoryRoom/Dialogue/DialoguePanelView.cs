using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Memories;
using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom.Dialogue
{
    // 대화 패널의 요소 구성과 표시 갱신. 검열이 풀렸는지도, 어느 선택지가
    // 옳은지도 판단하지 않는다 — 파서로 조각을 자르고, 조각마다 지금 풀렸는지만
    // (ICensorResolver) 물어 원문 또는 대체 표기를 그린다.
    //
    // 선택지 자리에는 두 종류가 온다: 텍스트 선택지 버튼, 그리고 ClueSelection
    // 줄의 단서 답변 버튼. 둘 다 같은 컨테이너(dialogue-choices)에 그리고,
    // 클래스로만 구분한다.
    public sealed class DialoguePanelView : IDialoguePanelView
    {
        private readonly ICensoredTextParser _parser;
        private readonly ICensorResolver _resolver;
        private readonly ICensorMaskFormatter _maskFormatter;

        private readonly Label _speakerLabel;
        private readonly VisualElement _body;
        private readonly VisualElement _choices;
        private readonly Label _noticeLabel;

        private readonly VisualElement _unlockPrompt;
        private readonly Label _unlockText;

        public event Action<CensorKey> MaskClicked;
        public event Action<ChoiceId> ChoiceClicked;
        public event Action<ClueId> ClueAnswerClicked;
        public event Action SkipClueAnswerClicked;
        public event Action UnlockConfirmed;
        public event Action UnlockCancelled;

        public DialoguePanelView(
            VisualElement root,
            ICensoredTextParser parser,
            ICensorResolver resolver,
            ICensorMaskFormatter maskFormatter)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _maskFormatter = maskFormatter ?? throw new ArgumentNullException(nameof(maskFormatter));

            _speakerLabel = root.Q<Label>("dialogue-speaker");
            _body = root.Q<VisualElement>("dialogue-body");
            _choices = root.Q<VisualElement>("dialogue-choices");
            _noticeLabel = root.Q<Label>("dialogue-notice");

            _unlockPrompt = root.Q<VisualElement>("dialogue-unlock-prompt");
            _unlockText = root.Q<Label>("dialogue-unlock-text");

            root.Q<Button>("dialogue-unlock-confirm").clicked += () => UnlockConfirmed?.Invoke();
            root.Q<Button>("dialogue-unlock-cancel").clicked += () => UnlockCancelled?.Invoke();

            HideUnlockPrompt();
            SetNotice(null);
        }

        public void SetLine(string speaker, string authoredText)
        {
            _speakerLabel.text = speaker ?? string.Empty;
            _speakerLabel.style.display =
                string.IsNullOrEmpty(speaker) ? DisplayStyle.None : DisplayStyle.Flex;

            _body.Clear();
            foreach (var segment in _parser.Parse(authoredText ?? string.Empty).Segments)
            {
                var revealed = !segment.Key.HasValue || _resolver.IsRevealed(segment.Key.Value);
                _body.Add(revealed ? PlainSegment(segment.Text) : MaskSegment(segment));
            }
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

        public void SetClueSelection(IReadOnlyList<KeyValuePair<ClueId, string>> clues)
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
                var id = clue.Key;
                var button = new Button(() => ClueAnswerClicked?.Invoke(id))
                {
                    text = string.IsNullOrEmpty(clue.Value) ? id.Value : clue.Value,
                };
                button.AddToClassList("dialogue-choice");
                button.AddToClassList("dialogue-choice--clue-answer");
                _choices.Add(button);
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

        public void ShowUnlockPrompt(string message)
        {
            _unlockText.text = message ?? string.Empty;
            _unlockPrompt.style.display = DisplayStyle.Flex;
        }

        public void HideUnlockPrompt() => _unlockPrompt.style.display = DisplayStyle.None;

        private static Label PlainSegment(string text)
        {
            var label = new Label(text);
            label.AddToClassList("dialogue-segment");
            label.pickingMode = PickingMode.Ignore;
            return label;
        }

        private Label MaskSegment(CensoredTextSegment segment)
        {
            var key = segment.Key.Value;
            var label = new Label(_maskFormatter.FormatMask(segment.Color.Value));
            label.AddToClassList("dialogue-segment");
            label.AddToClassList("dialogue-segment--mask");
            label.AddToClassList(MaskColorClass(segment.Color.Value));
            label.RegisterCallback<ClickEvent>(_ => MaskClicked?.Invoke(key));
            return label;
        }

        private static string MaskColorClass(MemoryColor color)
        {
            switch (color)
            {
                case MemoryColor.Red: return "dialogue-segment--mask-r";
                case MemoryColor.Green: return "dialogue-segment--mask-g";
                default: return "dialogue-segment--mask-b";
            }
        }
    }
}
