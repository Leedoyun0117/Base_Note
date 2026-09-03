using GameName.Core.Clues;
using GameName.Core.Memories;

namespace GameName.Core.Restoration
{
    // 복원도 위의 노드 한 개. 불변이다 — 위치나 이름이 바뀌면 보드가 이 인스턴스를
    // 새 인스턴스로 갈아 끼운다. 그래서 사건에 그대로 실려도 나중에 값이 조용히
    // 바뀌지 않는다.
    //
    // SourceClue는 ClueLinked 노드에만 있다(어느 추출에서 나왔는지). 나머지
    // 종류에서는 null이다.
    public sealed class RestorationNode
    {
        public RestorationNodeId Id { get; }
        public MemoryColor Color { get; }
        public RestorationNodeKind Kind { get; }
        public string Label { get; }
        public BoardPosition Position { get; }
        public ClueId? SourceClue { get; }

        public RestorationNode(
            RestorationNodeId id,
            MemoryColor color,
            RestorationNodeKind kind,
            string label,
            BoardPosition position,
            ClueId? sourceClue)
        {
            Id = id;
            Color = color;
            Kind = kind;
            Label = label ?? string.Empty;
            Position = position;
            SourceClue = sourceClue;
        }

        // 플레이어가 직접 적어 넣은 노드만 이름을 바꿀 수 있다. 보드가 이 규칙을
        // 강제하지만, 편집 가능 여부를 노드 자신에게 물을 수 있으면 화면 쪽도
        // 버튼 활성화를 같은 기준으로 정할 수 있다.
        public bool IsEditable => Kind == RestorationNodeKind.PlayerAuthored;

        public RestorationNode WithLabel(string label) =>
            new RestorationNode(Id, Color, Kind, label, Position, SourceClue);

        public RestorationNode WithPosition(BoardPosition position) =>
            new RestorationNode(Id, Color, Kind, Label, position, SourceClue);
    }
}
