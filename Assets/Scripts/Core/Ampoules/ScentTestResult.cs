using GameName.Core.Judging;

namespace GameName.Core.Ampoules
{
    // 시향 시도의 결과.
    public sealed class ScentTestResult
    {
        public bool Succeeded { get; }
        public ScentTestFailureReason? FailureReason { get; }
        public ScentJudgementResult Judgement { get; }

        private ScentTestResult(
            bool succeeded, ScentTestFailureReason? failureReason, ScentJudgementResult judgement)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            Judgement = judgement;
        }

        public static ScentTestResult Success(ScentJudgementResult judgement) =>
            new ScentTestResult(true, null, judgement);

        public static ScentTestResult Failure(ScentTestFailureReason reason) =>
            new ScentTestResult(false, reason, default);
    }
}
