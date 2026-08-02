using GameName.Core.Analysis;

namespace GameName.Core.Clues
{
    // 단서 분석 시도의 결과. 성공한 경우에만 EmotionAnalysisResult를 담는다.
    public sealed class ClueAnalysisResult
    {
        public bool Succeeded { get; }
        public ClueAnalysisFailureReason? FailureReason { get; }
        public EmotionAnalysisResult AnalysisResult { get; }

        private ClueAnalysisResult(
            bool succeeded, ClueAnalysisFailureReason? failureReason, EmotionAnalysisResult analysisResult)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            AnalysisResult = analysisResult;
        }

        public static ClueAnalysisResult Success(EmotionAnalysisResult analysisResult) =>
            new ClueAnalysisResult(true, null, analysisResult);

        public static ClueAnalysisResult Failure(ClueAnalysisFailureReason reason) =>
            new ClueAnalysisResult(false, reason, null);
    }
}
