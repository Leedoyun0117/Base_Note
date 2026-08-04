namespace GameName.Core.Clues
{
    // 단서 분석 실패 사유.
    public enum ClueAnalysisFailureReason
    {
        NotInAnalysisRoom,

        // 인벤토리에도 분석실 보관대에도 없는 단서는 분석할 수 없다 — 습득하지
        // 않았거나 이미 다른 용도로 소모된 경우가 여기 해당한다.
        ClueNotAccessible,
        InsufficientMentality,

        // 같은 깊이(또는 더 얕은 깊이)로 이미 분석한 단서를 다시 분석하려는 경우.
        // 더 깊은 분석으로 "올라가는" 시도는 이 사유에 해당하지 않는다.
        AlreadyAnalyzedAtSameOrDeeperDepth
    }
}
