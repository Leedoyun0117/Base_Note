namespace GameName.Core.Restoration
{
    // 플레이어 노드를 새로 놓으려는 시도의 결과. 성공하면 만들어진 노드를 함께
    // 돌려준다 — 화면이 그 식별자로 곧바로 편집·연결을 이어 갈 수 있도록.
    public sealed class RestorationNodeResult
    {
        public bool Succeeded { get; }
        public RestorationEditFailureReason? FailureReason { get; }
        public RestorationNode Node { get; }

        private RestorationNodeResult(
            bool succeeded, RestorationEditFailureReason? failureReason, RestorationNode node)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            Node = node;
        }

        public static RestorationNodeResult Success(RestorationNode node) =>
            new RestorationNodeResult(true, null, node);

        public static RestorationNodeResult Failure(RestorationEditFailureReason reason) =>
            new RestorationNodeResult(false, reason, null);
    }
}
