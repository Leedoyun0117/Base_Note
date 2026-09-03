using GameName.Core.Clues;

namespace GameName.Core.Events
{
    // 단서 하나를 추출에 써 버렸다는 사실.
    //
    // 남은 횟수를 함께 싣는 이유: 이 사실을 듣는 쪽(횟수 표시 등)이 다시
    // IExtractionBudget을 조회하게 하면, 그 사이 다른 구독자가 횟수를 바꿨을 때
    // 구독자마다 다른 숫자를 보게 된다. 사건이 일어난 시점의 값을 그대로 싣는다.
    //
    // 어떤 색이 나왔는지는 여기 없다 — 그것은 MemoryColorRevealedEvent다.
    // 추출이 항상 색을 남기는 것은 아니기 때문이다.
    public readonly struct ClueExtractedEvent
    {
        public ClueId ClueId { get; }
        public int RemainingExtractions { get; }

        public ClueExtractedEvent(ClueId clueId, int remainingExtractions)
        {
            ClueId = clueId;
            RemainingExtractions = remainingExtractions;
        }
    }
}
