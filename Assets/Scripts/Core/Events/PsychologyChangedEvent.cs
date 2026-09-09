using GameName.Core.Mind;

namespace GameName.Core.Events
{
    // 심리 상태가 바뀌었다는 사실.
    //
    // 이전 상태를 함께 싣는 이유는 TrustChangedEvent와 같다: 듣는 쪽이 관심
    // 있는 것은 대개 "무엇에서 무엇으로" 바뀌었는가다(연출도 그 전이에 붙는다).
    public readonly struct PsychologyChangedEvent
    {
        public PsychologyState Previous { get; }
        public PsychologyState Current { get; }

        public PsychologyChangedEvent(PsychologyState previous, PsychologyState current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
