namespace GameName.Core.Events
{
    // ClueSelection 줄에 단서로 답했다(또는 답하지 않고 넘어갔다)는 사실.
    //
    // WasCorrect를 싣는 이유: 오답 단서는 신뢰를 깎지 않으므로(잘못 말한 것과
    // 잘못 답한 것을 같은 무게로 두지 않는다) TrustChangedEvent만 봐서는 "이
    // 갈림길에서 최선을 골랐는가"를 알 수 없다. 이 사건이 유일한 흔적이다.
    // 넘어가기(SkipClueSelection)도 정답이 아니므로 WasCorrect = false다.
    public readonly struct ClueAnsweredEvent
    {
        public bool WasCorrect { get; }

        public ClueAnsweredEvent(bool wasCorrect)
        {
            WasCorrect = wasCorrect;
        }
    }
}
