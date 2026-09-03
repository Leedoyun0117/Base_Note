using GameName.Core.Memories;

namespace GameName.Core.Dialogue
{
    // 검열 해금 시도의 결과.
    //
    // 이미 풀려 있던 키를 다시 풀려 한 경우도 성공이다(멱등). 그때는 자원이
    // 쓰이지 않았음을 SpentColor가 null인 것으로 구분할 수 있다.
    public sealed class CensorUnlockResult
    {
        public bool Succeeded { get; }
        public CensorUnlockFailureReason? FailureReason { get; }

        // 이번 호출로 실제로 소모한 색. 멱등 성공(이미 풀려 있었음)이면 null이다.
        public MemoryColor? SpentColor { get; }

        private CensorUnlockResult(
            bool succeeded, CensorUnlockFailureReason? failureReason, MemoryColor? spentColor)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            SpentColor = spentColor;
        }

        public static CensorUnlockResult Spent(MemoryColor color) =>
            new CensorUnlockResult(true, null, color);

        public static CensorUnlockResult AlreadyRevealed() =>
            new CensorUnlockResult(true, null, null);

        public static CensorUnlockResult Failure(CensorUnlockFailureReason reason) =>
            new CensorUnlockResult(false, reason, null);
    }
}
