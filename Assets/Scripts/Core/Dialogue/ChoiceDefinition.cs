using System;

namespace GameName.Core.Dialogue
{
    // 선택지 하나의 저작 데이터.
    public sealed class ChoiceDefinition
    {
        public ChoiceId Id { get; }
        public string AuthoredText { get; }

        // 이 선택지가 대화를 옳은 방향으로 미는가. 옳고 그름을 문구가 아니라
        // 데이터로 두는 이유는 판정이 문구 해석에 의존하면 번역할 때 게임이
        // 함께 바뀌어 버리기 때문이다.
        public bool IsCorrect { get; }

        // 다음 대사. 대화를 여기서 끝내는 선택지면 null이다.
        public DialogueLineId? Next { get; }

        public ChoiceCondition Condition { get; }

        public ChoiceDefinition(
            ChoiceId id,
            string authoredText,
            bool isCorrect,
            DialogueLineId? next,
            ChoiceCondition condition)
        {
            Id = id;
            AuthoredText = authoredText ?? throw new ArgumentNullException(nameof(authoredText));
            IsCorrect = isCorrect;
            Next = next;
            Condition = condition;
        }
    }
}
