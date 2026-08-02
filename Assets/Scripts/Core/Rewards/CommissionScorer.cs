using System;
using System.Collections.Generic;
using GameName.Core.Judging;

namespace GameName.Core.Rewards
{
    // 방별 시향 판정 결과를 의뢰 하나의 점수(평균 정확도)로 합치는 순수 계산.
    // 화면이나 완료 처리기가 이 계산을 직접 하지 않도록 한 곳에 고정한다.
    public static class CommissionScorer
    {
        // 바탕 감정이 틀리면 보조 감정 배합이 우연히 정답과 비슷해도 결과적으로는
        // 전혀 다른 감정을 표현한 향이므로, 정확도를 완전히 무효(0)로 취급한다.
        // 그렇지 않으면 바탕이 틀린 향이 Accuracy 값만으로 만점에 가까운 보상을
        // 받는 모순이 생긴다.
        public static double EffectiveAccuracy(ScentJudgementResult result) =>
            result.IsBaseEmotionCorrect ? result.Accuracy : 0.0;

        public static double AverageAccuracy(IReadOnlyList<ScentJudgementResult> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            if (results.Count == 0)
                throw new ArgumentException("최소 한 개의 판정 결과가 필요하다.", nameof(results));

            var sum = 0.0;
            foreach (var result in results)
                sum += EffectiveAccuracy(result);

            return sum / results.Count;
        }
    }
}
