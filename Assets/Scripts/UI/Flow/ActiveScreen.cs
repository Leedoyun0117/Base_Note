namespace GameName.UI.Flow
{
    // 지금 활성화해야 할 메인 화면. None은 전부 꺼야 한다는 뜻이다(대화 중이거나
    // 기억 진입/탈출 지점에 서 있는 순간 — 그 상태의 안내는 FlowOverlay가 대신
    // 보여준다).
    public enum ActiveScreen
    {
        None,
        Perfumery,
        MemoryRoom,
        AnalysisRoom,

        // 현실로 복귀한 뒤, 방마다 최종 향을 확정하는 화면.
        FinalCrafting,

        // 의뢰를 완료(제공)한 뒤, 결과·보상·진열/업그레이드를 보여주는 화면.
        Completion
    }
}
