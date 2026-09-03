namespace GameName.Core.Restoration
{
    // 복원도 편집 시도(이동·이름 변경·삭제)의 결과. 부분 성공은 없다 — 실패면
    // 보드는 하나도 바뀌지 않는다.
    public sealed class RestorationEditResult
    {
        public bool Succeeded { get; }
        public RestorationEditFailureReason? FailureReason { get; }

        private RestorationEditResult(bool succeeded, RestorationEditFailureReason? failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static RestorationEditResult Success() => new RestorationEditResult(true, null);

        public static RestorationEditResult Failure(RestorationEditFailureReason reason) =>
            new RestorationEditResult(false, reason);
    }
}
