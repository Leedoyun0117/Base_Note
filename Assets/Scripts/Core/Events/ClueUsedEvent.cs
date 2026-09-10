using GameName.Core.Clues;

namespace GameName.Core.Events
{
    // 단서 하나를 클릭해 읽었다는 사실. 그 서사를 함께 싣는다 — 스토리 패널이
    // 이 사건 하나로 무엇을 그릴지 안다(ClueInfo에는 서사가 없다).
    //
    // 해석 결과(태그 체인)는 별도 사건(ClueInterpretedEvent)으로 나간다 — 서사
    // 표시와 감정 반응은 서로 다른 소비자의 몫이다.
    public readonly struct ClueUsedEvent
    {
        public ClueId ClueId { get; }
        public string DisplayName { get; }
        public string Story { get; }

        public ClueUsedEvent(ClueId clueId, string displayName, string story)
        {
            ClueId = clueId;
            DisplayName = displayName;
            Story = story;
        }
    }
}
