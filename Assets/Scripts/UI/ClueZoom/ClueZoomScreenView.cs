using System;
using System.Collections.Generic;
using System.Text;
using GameName.Core.Complexes;
using UnityEngine.UIElements;

namespace GameName.UI.ClueZoom
{
    // 방에서 단서를 누르면 뜨는 스토리 패널. 먼저 그 단서의 짧은 서사만 보여
    // 주고, [사용]을 눌러야 실제로 읽는다(= 한 턴을 쓴다). 읽고 나면 원본 태그가
    // 활성 컴플렉스 체인을 통과해 최종 태그가 되기까지의 해석 로그를 덧붙인다.
    //
    // 판정은 하나도 하지 않는다 — 컨트롤러가 Core에서 받은 결과를 문구로 옮길 뿐.
    // UXML 요소 이름은 옛 단서 확대 창 것을 재활용한다(정리는 별도 작업):
    // [수집] 버튼 자리가 [사용]/[닫기]로 바뀐다.
    public sealed class ClueZoomScreenView
    {
        private readonly VisualElement _stage;
        private readonly Label _nameLabel;
        private readonly Label _storyLabel;
        private readonly Label _messageLabel;
        private readonly Button _actionButton;

        private bool _acceptsStageClose;

        // [사용] 버튼을 눌러 이 단서를 실제로 읽겠다고 했다.
        private bool _used;

        // 이미 읽은 뒤라 [사용] 버튼이 [닫기]로 바뀌었는가.
        public event Action UseRequested;

        // 나가기 버튼을 눌렀거나, 읽은 뒤 [닫기]를 눌렀거나, 카드 바깥(무대)을 눌렀다.
        public event Action ExitRequested;

        public ClueZoomScreenView(VisualElement root)
        {
            _stage = root.Q<VisualElement>("clue-zoom-stage");
            _nameLabel = root.Q<Label>("clue-zoom-clue-name");
            _storyLabel = root.Q<Label>("clue-zoom-clue-kind");
            _messageLabel = root.Q<Label>("clue-zoom-message");

            var exit = root.Q<Button>("clue-zoom-exit-button");
            if (exit != null) exit.clicked += () => ExitRequested?.Invoke();

            // 옛 [수집] 버튼 자리 = 읽기 전엔 [사용], 읽은 뒤엔 [닫기].
            _actionButton = root.Q<Button>("clue-zoom-collect-button");
            if (_actionButton != null)
                _actionButton.clicked += OnActionClicked;

            if (_stage != null)
            {
                _stage.RegisterCallback<PointerUpEvent>(_ => _acceptsStageClose = true);
                _stage.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (_acceptsStageClose && ReferenceEquals(evt.target, _stage))
                        ExitRequested?.Invoke();
                });
            }
        }

        private void OnActionClicked()
        {
            if (_used) ExitRequested?.Invoke();
            else UseRequested?.Invoke();
        }

        // 패널이 열릴 때. 서사만 보여 주고 [사용] 버튼을 대기시킨다.
        public void BeginRead(string displayName, string story)
        {
            _acceptsStageClose = false;
            _used = false;

            if (_nameLabel != null)
                _nameLabel.text = string.IsNullOrEmpty(displayName) ? "단서" : displayName;
            SetStory(story);
            SetMessage(null);

            if (_actionButton != null)
            {
                _actionButton.text = "사용";
                _actionButton.SetEnabled(true);
            }
        }

        // [사용]이 성공했다 — 버튼을 [닫기]로 바꾼다.
        public void MarkUsed()
        {
            _used = true;
            if (_actionButton != null)
                _actionButton.text = "닫기";
        }

        public void SetStory(string story)
        {
            if (_storyLabel == null) return;
            _storyLabel.text = story ?? string.Empty;
            _storyLabel.style.display = string.IsNullOrEmpty(story) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // 해석 로그: 원본 → 각 컴플렉스 단계 → 최종 태그.
        public void SetInterpretation(
            IReadOnlyList<StoryTag> sourceTags,
            IReadOnlyList<ComplexChainStep> steps,
            IReadOnlyList<StoryTag> finalTags)
        {
            var sb = new StringBuilder();
            sb.Append("원본  ").Append(Join(sourceTags));

            if (steps != null)
            {
                foreach (var step in steps)
                    sb.Append('\n').Append(step.ComplexId).Append("  ").Append(Join(step.TagsAfter));
            }

            if (steps == null || steps.Count == 0)
                sb.Append("\n(작용한 컴플렉스 없음)");

            sb.Append("\n최종  ").Append(Join(finalTags));
            SetMessage(sb.ToString());
        }

        public void SetMessage(string message)
        {
            if (_messageLabel == null) return;
            _messageLabel.text = message ?? string.Empty;
            _messageLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private static string Join(IReadOnlyList<StoryTag> tags)
        {
            if (tags == null || tags.Count == 0)
                return "—";

            var sb = new StringBuilder();
            for (var i = 0; i < tags.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(tags[i]);
            }

            return sb.ToString();
        }
    }
}
