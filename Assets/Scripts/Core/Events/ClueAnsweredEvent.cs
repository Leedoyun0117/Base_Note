using GameName.Core.Clues;

namespace GameName.Core.Events
{
    // ClueSelection 줄에 단서로 답했다(또는 답하지 않고 넘어갔다)는 사실.
    //
    // Grade를 싣는 이유: 오답이든 정답이든 신뢰는 안정 축 이탈로만 깎이므로
    // TrustChangedEvent만 봐서는 "이 갈림길에서 얼마나 맞는 답을 골랐는가"를
    // 알 수 없다. 이 사건이 유일한 흔적이고, 피드백 대사·안정 축 이동([5])이
    // 다섯 단계를 그대로 쓴다. 넘어가기(SkipClueSelection)는 MatchGrade.None이다.
    public readonly struct ClueAnsweredEvent
    {
        public MatchGrade Grade { get; }

        public ClueAnsweredEvent(MatchGrade grade)
        {
            Grade = grade;
        }
    }
}
