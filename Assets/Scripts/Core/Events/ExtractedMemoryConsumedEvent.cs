using GameName.Core.Clues;
using GameName.Core.Memories;

namespace GameName.Core.Events
{
    // 추출한 기억 하나를 대화의 답으로 내밀어 써 버렸다는 사실.
    //
    // ClueUsedInDialogueEvent와 갈라 둔 이유는 MemoryColorRevealedEvent를
    // ClueExtractedEvent와 가른 것과 같다: 단서가 손에서 나갔다는 사실(가방
    // 격자가 볼 것)과, 그 단서에 딸려 있던 추출된 기억이 소모됐다는 사실(상단
    // 바의 색 보유 점이 볼 것)은 서로 다른 층위다. 추출하지 않은 단서를 답으로
    // 냈을 때는 소모될 기억이 없어 이 사건은 나지 않는다.
    public readonly struct ExtractedMemoryConsumedEvent
    {
        public ClueId Source { get; }
        public MemoryColor Color { get; }

        public ExtractedMemoryConsumedEvent(ClueId source, MemoryColor color)
        {
            Source = source;
            Color = color;
        }
    }
}
