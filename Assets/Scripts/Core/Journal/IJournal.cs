namespace GameName.Core.Journal
{
    // 기록지 쓰기 경계.
    //
    // 분석/제작/시향 기록은 여기 없다 — ClueAnalyzedEvent, AmpouleCraftedEvent,
    // ScentJudgedEvent를 통해 이미 시스템 전체에 발행되고 있으므로, Journal
    // 구현체가 그 이벤트를 직접 구독해 기록한다(같은 사실을 두 경로로 남기지
    // 않기 위함 — 자세한 근거는 설계 근거 문서 참고). 대화는 그런 이벤트가
    // 없으므로(대화 시스템 자체가 아직 없다) 유일하게 직접 호출로 남는다.
    //
    // 의뢰 전환은 이 인터페이스로만 가능하다 — 이후의 모든 기록은 마지막으로
    // 지정된 의뢰에 귀속된다.
    public interface IJournal
    {
        void BeginCommission(CommissionId commissionId);
        void RecordDialogue(DialogueLine line);
    }
}
