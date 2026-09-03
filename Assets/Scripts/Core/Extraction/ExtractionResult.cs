namespace GameName.Core.Extraction
{
    // 단서 추출 시도의 결과. 부분 성공은 없다 — 실패면 자원도, 단서 단계도,
    // 지갑도 전혀 바뀌지 않는다.
    public sealed class ExtractionResult
    {
        public bool Succeeded { get; }
        public ExtractionFailureReason? FailureReason { get; }

        private ExtractionResult(bool succeeded, ExtractionFailureReason? failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static ExtractionResult Success() => new ExtractionResult(true, null);

        public static ExtractionResult Failure(ExtractionFailureReason reason) =>
            new ExtractionResult(false, reason);
    }
}
