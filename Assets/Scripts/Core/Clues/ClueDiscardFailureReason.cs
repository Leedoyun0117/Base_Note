namespace GameName.Core.Clues
{
    // 단서 버리기 시도가 실패한 이유.
    public enum ClueDiscardFailureReason
    {
        // 지금 손에 든(Collected) 단서가 아니다 — 아직 못 집었거나, 이미 대화에
        // 쓰거나 추출했거나 버려서 손에 없다.
        NotCollected
    }
}
