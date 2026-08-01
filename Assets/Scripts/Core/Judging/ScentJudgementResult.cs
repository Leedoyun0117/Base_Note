namespace GameName.Core.Judging
{
    // 시향 판정 결과.
    // - Stage: 시향 중 실시간으로 들려주는 청각 피드백 단계(4단계 이산값).
    // - Accuracy: 의뢰 완료 시 보상(감정량) 산정에 쓰이는 연속값. 계산 공식은
    //   아직 미정이라 이 타입은 값을 담는 자리만 제공하며, 실제 산출은
    //   IScentJudge 구현체 책임이다.
    // 두 값은 쓰임(실시간 피드백 vs 보상 산정)이 서로 다르므로, 하나가 다른
    // 하나로부터 기계적으로 유도된다고 가정하지 않는다.
    public readonly struct ScentJudgementResult
    {
        public bool IsBaseEmotionCorrect { get; }
        public FeedbackStage Stage { get; }
        public double Accuracy { get; }

        public ScentJudgementResult(bool isBaseEmotionCorrect, FeedbackStage stage, double accuracy)
        {
            IsBaseEmotionCorrect = isBaseEmotionCorrect;
            Stage = stage;
            Accuracy = accuracy;
        }
    }
}
