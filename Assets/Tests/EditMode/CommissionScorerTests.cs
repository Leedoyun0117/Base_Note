using GameName.Core.Judging;
using GameName.Core.Rewards;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 섹션 2 검증: 바탕 감정이 틀린 향은 보조 감정 배합이 정답과 완전히
    // 같아 ScentJudge가 Accuracy 1.0을 내더라도, 점수 산정에서는 0으로
    // 취급되어야 한다. 그렇지 않으면 바탕이 틀린 향이 만점 보상을 받는다.
    public class CommissionScorerTests
    {
        [Test]
        public void 바탕_감정이_틀리면_정확도가_1이어도_유효_정확도는_0이다()
        {
            var result = new ScentJudgementResult(isBaseEmotionCorrect: false, stage: FeedbackStage.Silence, accuracy: 1.0);

            var effective = CommissionScorer.EffectiveAccuracy(result);

            Assert.AreEqual(0.0, effective);
        }

        [Test]
        public void 바탕_감정이_맞으면_정확도를_그대로_쓴다()
        {
            var result = new ScentJudgementResult(isBaseEmotionCorrect: true, stage: FeedbackStage.Piano, accuracy: 0.7);

            var effective = CommissionScorer.EffectiveAccuracy(result);

            Assert.AreEqual(0.7, effective);
        }

        [Test]
        public void 평균_정확도는_바탕이_틀린_방을_0으로_넣어_계산한다()
        {
            var results = new[]
            {
                new ScentJudgementResult(isBaseEmotionCorrect: true, stage: FeedbackStage.PianoAndViolin, accuracy: 1.0),
                new ScentJudgementResult(isBaseEmotionCorrect: false, stage: FeedbackStage.Silence, accuracy: 1.0),
            };

            var average = CommissionScorer.AverageAccuracy(results);

            // (1.0 + 0.0) / 2 = 0.5 — 바탕이 틀린 방의 raw Accuracy(1.0)를 그대로
            // 썼다면 평균이 1.0이 되어 만점 보상을 받았을 것이다.
            Assert.AreEqual(0.5, average);
        }
    }
}
