using GameName.Core.Clues;

namespace GameName.Core.Events
{
    // 단서 하나를 이 런에서 완전히 버렸다는 사실.
    //
    // 추출과 달리 아무것도 남기지 않는다 — 색도, 대화에 쓸 기회도 없이 그냥
    // 사라진다. 회수할 방법은 이 런 안에 없다("원래 자리로 돌아간다"는 것은
    // 다음 런에서 처음 위치에 다시 존재한다는 뜻일 뿐, 이번 런에서는 아니다).
    public readonly struct ClueDiscardedEvent
    {
        public ClueId ClueId { get; }

        public ClueDiscardedEvent(ClueId clueId)
        {
            ClueId = clueId;
        }
    }
}
