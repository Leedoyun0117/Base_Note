using System;

namespace GameName.Core.Rewards
{
    // 보상 등급 하나 — "평균 정확도가 이 값 이상이면 이 선물을 받는다"는
    // 규칙 하나를 표현하는 불변 값. 등급 경계/보상 수치를 코드에 흩어 상수로
    // 박아두지 않기 위해 RewardTable 생성자로만 주입된다.
    public readonly struct RewardTier
    {
        public double MinimumAverageAccuracy { get; }
        public int EmotionalValue { get; }
        public string ReactionDialogue { get; }

        public RewardTier(double minimumAverageAccuracy, int emotionalValue, string reactionDialogue)
        {
            if (minimumAverageAccuracy < 0.0 || minimumAverageAccuracy > 1.0)
                throw new ArgumentOutOfRangeException(nameof(minimumAverageAccuracy), "정확도 하한은 0~1 사이여야 한다.");
            if (emotionalValue < 0)
                throw new ArgumentOutOfRangeException(nameof(emotionalValue), "감정량은 음수일 수 없다.");
            if (string.IsNullOrWhiteSpace(reactionDialogue))
                throw new ArgumentException("반응 대사는 비어 있을 수 없다.", nameof(reactionDialogue));

            MinimumAverageAccuracy = minimumAverageAccuracy;
            EmotionalValue = emotionalValue;
            ReactionDialogue = reactionDialogue;
        }
    }
}
