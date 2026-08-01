using System;
using GameName.Core.Emotions;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Judging
{
    // IScentJudge 기본 구현.
    //
    // 정확도 공식: 기본 감정 5종 각각에 대해 |정답 세기 - 후보 세기|를 모두 더한
    // 값을 (정답 총량 + 후보 총량)으로 나눈 오차를 1에서 뺀 값. 두 총량이 모두
    // 0이면(둘 다 빈 배분) 나눌 오차 자체가 없으므로 정확도를 1로 취급한다.
    public sealed class ScentJudge : IScentJudge
    {
        private static readonly EmotionType[] AllEmotionTypes =
            (EmotionType[])Enum.GetValues(typeof(EmotionType));

        private readonly IScentJudgementSettings _settings;

        public ScentJudge(IScentJudgementSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public ScentJudgementResult Judge(Scent candidate, MemoryRoomAnswer answer)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            if (answer == null) throw new ArgumentNullException(nameof(answer));

            var correct = answer.CorrectScent;
            var accuracy = CalculateAccuracy(candidate.SupportingBlend, correct.SupportingBlend);
            var isBaseEmotionCorrect = candidate.BaseEmotion == correct.BaseEmotion;
            var stage = DetermineStage(candidate, correct, isBaseEmotionCorrect, accuracy);

            return new ScentJudgementResult(isBaseEmotionCorrect, stage, accuracy);
        }

        private FeedbackStage DetermineStage(
            Scent candidate, Scent correct, bool isBaseEmotionCorrect, double accuracy)
        {
            if (!isBaseEmotionCorrect)
                return FeedbackStage.Silence;

            // 최상위 단계는 실수 오차가 아니라 배분 값 자체의 동등 비교로만 열린다.
            // 이 단계가 기억 방 완전 복원의 게이트 조건으로 쓰이므로, 부동소수점
            // 오차가 게이트에 끼어들 여지를 원천적으로 차단한다.
            if (candidate.SupportingBlend.Equals(correct.SupportingBlend))
                return FeedbackStage.PianoAndViolinAndDrum;

            return accuracy >= _settings.HighAccuracyThreshold
                ? FeedbackStage.PianoAndViolin
                : FeedbackStage.Piano;
        }

        private static double CalculateAccuracy(EmotionBlend candidateBlend, EmotionBlend correctBlend)
        {
            var differenceSum = 0;
            foreach (var emotion in AllEmotionTypes)
            {
                var correctIntensity = correctBlend.IntensityOf(emotion);
                var candidateIntensity = candidateBlend.IntensityOf(emotion);
                differenceSum += Math.Abs(correctIntensity - candidateIntensity);
            }

            var denominator = correctBlend.Total + candidateBlend.Total;
            if (denominator == 0)
                return 1d;

            var error = differenceSum / (double)denominator;
            var accuracy = 1d - error;

            // 공식만으로도 이론상 0~1을 벗어날 수 없지만, 부동소수점 오차로 경계를
            // 살짝 넘는 것까지 방지해 "0 이상 1 이하" 계약을 확실히 지킨다.
            if (accuracy < 0d) return 0d;
            if (accuracy > 1d) return 1d;
            return accuracy;
        }
    }
}
