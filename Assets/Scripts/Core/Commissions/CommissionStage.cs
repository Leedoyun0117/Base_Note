namespace GameName.Core.Commissions
{
    // 의뢰 하나가 진행되는 동안 거치는 단계.
    // 값 순서가 실제 진행 순서와 같다 — CommissionSession의 전환 규칙이 이
    // 순서를 전제로 한다(뒤로는 못 간다).
    public enum CommissionStage
    {
        PreConversation,
        InMemory,
        ReturnedToReality,
        Completed
    }
}
