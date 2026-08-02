namespace GameName.Core.Journal
{
    // 기록지 쓰기 경계.
    //
    // 분석/제작/시향/대화 기록은 여기 없다 — ClueAnalyzedEvent, AmpouleCraftedEvent,
    // ScentJudgedEvent, DialogueLineShownEvent를 통해 이미 시스템 전체에
    // 발행되고 있으므로, Journal 구현체가 그 이벤트를 직접 구독해 기록한다
    // (같은 사실을 두 경로로 남기지 않기 위함 — 자세한 근거는 설계 근거 문서
    // 참고). BeginCommission만 직접 호출로 남는다 — "의뢰가 시작됐다"는
    // 사실은 어떤 시스템이 일으킨 사건이 아니라, 기록 범위를 새로 여는 관리
    // 동작이기 때문이다.
    //
    // 의뢰 전환은 이 인터페이스로만 가능하다 — 이후의 모든 기록은 마지막으로
    // 지정된 의뢰에 귀속된다.
    public interface IJournal
    {
        void BeginCommission(CommissionId commissionId);
    }
}
