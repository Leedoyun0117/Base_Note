using System;

namespace GameName.Core.Rewards
{
    // 의뢰 완료 시 의뢰인이 주는 선물 하나. 자원 제약상 선물은 전부 같은
    // 모습으로 보여지므로(전용 스프라이트/모델을 따로 두지 않는다), 이 값
    // 타입도 시각적 구분을 위한 필드를 갖지 않는다 — 오직 감정량 숫자와
    // 반응 대사만으로 결과를 구분한다.
    public sealed class Gift
    {
        public int EmotionalValue { get; }
        public string ReactionDialogue { get; }

        public Gift(int emotionalValue, string reactionDialogue)
        {
            if (emotionalValue < 0)
                throw new ArgumentOutOfRangeException(nameof(emotionalValue), "감정량은 음수일 수 없다.");
            if (string.IsNullOrWhiteSpace(reactionDialogue))
                throw new ArgumentException("반응 대사는 비어 있을 수 없다.", nameof(reactionDialogue));

            EmotionalValue = emotionalValue;
            ReactionDialogue = reactionDialogue;
        }
    }
}
