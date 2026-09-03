namespace GameName.Core.Restoration
{
    // 플레이어가 두 노드를 이으려는 시도의 결과. 성공하면 만들어진 간선을 함께
    // 돌려준다.
    public sealed class RestorationEdgeResult
    {
        public bool Succeeded { get; }
        public RestorationEditFailureReason? FailureReason { get; }
        public RestorationEdge Edge { get; }

        private RestorationEdgeResult(
            bool succeeded, RestorationEditFailureReason? failureReason, RestorationEdge edge)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            Edge = edge;
        }

        public static RestorationEdgeResult Success(RestorationEdge edge) =>
            new RestorationEdgeResult(true, null, edge);

        public static RestorationEdgeResult Failure(RestorationEditFailureReason reason) =>
            new RestorationEdgeResult(false, reason, null);
    }
}
