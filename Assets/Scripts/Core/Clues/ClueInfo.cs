using System;

namespace GameName.Core.Clues
{
    // 단서에 대해 플레이어(씬 렌더링 등)에게 공개해도 되는 정보.
    // ClueDefinition.ToInfo()를 통해서만 만들어지는 단방향 변환이다.
    //
    // 서사(Story)와 태그(Tags)는 담지 않는다 — 서사는 클릭했을 때 사건으로
    // 전달되고, 태그는 해석의 여지를 남기려 감춘다. Kind·DisplayName·자리는
    // 방에 들어서면 그냥 보이는 사실이라 담는다.
    public sealed class ClueInfo : IEquatable<ClueInfo>
    {
        public ClueId Id { get; }
        public ClueKind Kind { get; }
        public string DisplayName { get; }
        public CluePositionRatio AuthoredPosition { get; }

        public ClueInfo(ClueId id, ClueKind kind, string displayName, CluePositionRatio authoredPosition)
        {
            Id = id;
            Kind = kind;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            AuthoredPosition = authoredPosition;
        }

        public bool Equals(ClueInfo other) => other != null && Id.Equals(other.Id);
        public override bool Equals(object obj) => Equals(obj as ClueInfo);
        public override int GetHashCode() => Id.GetHashCode();
    }
}
