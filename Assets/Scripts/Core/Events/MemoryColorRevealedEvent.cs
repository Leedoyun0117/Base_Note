using GameName.Core.Clues;
using GameName.Core.Memories;

namespace GameName.Core.Events
{
    // 단서에서 색이 드러났다는 사실.
    // Source를 싣는 이유는 "어느 물건에서 이 색이 나왔는가"가 연출과 기록
    // 양쪽에서 필요한 정보이기 때문이다.
    public readonly struct MemoryColorRevealedEvent
    {
        public MemoryColor Color { get; }
        public ClueId Source { get; }

        public MemoryColorRevealedEvent(MemoryColor color, ClueId source)
        {
            Color = color;
            Source = source;
        }
    }
}
