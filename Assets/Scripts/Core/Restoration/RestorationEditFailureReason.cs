namespace GameName.Core.Restoration
{
    // 복원도 편집 시도가 실패한 이유. 호출부(화면)가 사유별로 다른 안내를 줄 수
    // 있도록 구분한다.
    public enum RestorationEditFailureReason
    {
        // 그 색으로 아직 한 번도 추출이 없었다 — 뿌리가 없으므로 그 색 보드에
        // 노드를 붙일 수 없다.
        ColorNotStarted,

        // 그 식별자의 노드가 이 보드에 없다.
        NodeNotFound,

        // 그 식별자의 간선이 이 보드에 없다.
        EdgeNotFound,

        // 자동 생성된 노드(뿌리·단서)라 이름을 바꾸거나 지울 수 없다.
        NodeNotEditable,

        // 자동 생성된 간선이라 지울 수 없다.
        EdgeLocked,

        // 두 노드가 서로 다른 색 보드에 있다 — 간선은 같은 색 안에서만 잇는다.
        ColorMismatch,

        // 한 노드를 자기 자신에게 이으려 했다.
        SelfLoop
    }
}
