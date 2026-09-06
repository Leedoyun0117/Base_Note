using GameName.Core.Clues;

namespace GameName.Core.Events
{
    // 단서 하나를 추출에 써 버렸다는 사실.
    //
    // 추출로 히로민이 얼마나 줄었는지는 여기 없다 — 그것은 HiromiChangedEvent다.
    // 자원 표시와 단서 소비 표시를 갈라 두면, 화면마다 필요한 사건만 구독하면
    // 된다(가방 격자는 이 사건만, 상단 바는 HiromiChangedEvent만 본다).
    //
    // 어떤 색이 나왔는지도 여기 없다 — 그것은 MemoryColorRevealedEvent다.
    // 추출이 항상 색을 남기는 것은 아니기 때문이다.
    public readonly struct ClueExtractedEvent
    {
        public ClueId ClueId { get; }

        public ClueExtractedEvent(ClueId clueId)
        {
            ClueId = clueId;
        }
    }
}
