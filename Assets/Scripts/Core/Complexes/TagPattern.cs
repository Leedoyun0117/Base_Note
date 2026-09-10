using System;

namespace GameName.Core.Complexes
{
    // 컴플렉스 규칙(TagTransformRule)이 "어떤 태그에 반응하는가"를 적는 조건.
    //
    // 판정만 담고 결과는 담지 않는다 — 무엇을 할지(변환/삭제/…)는 규칙의 몫이다.
    //
    // Value가 비어 있으면 그 축의 모든 값에 걸리는 와일드카드다("Emotion 축
    // 태그면 무엇이든"). 특정 값만 노릴 때는 값을 채운다("Emotion:그리움").
    // 축은 반드시 지정한다 — 축 없는 태그는 없기 때문이다.
    public readonly struct TagPattern : IEquatable<TagPattern>
    {
        public StoryTagAxis Axis { get; }

        // null 또는 빈 문자열이면 축 전체 와일드카드.
        public string Value { get; }

        public bool IsWildcard => string.IsNullOrEmpty(Value);

        public TagPattern(StoryTagAxis axis, string value = null)
        {
            Axis = axis;
            Value = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public static TagPattern AnyOf(StoryTagAxis axis) => new TagPattern(axis);
        public static TagPattern Exact(StoryTag tag) => new TagPattern(tag.Axis, tag.Value);

        public bool Matches(StoryTag tag)
        {
            if (tag.Axis != Axis)
                return false;

            return IsWildcard || tag.Value == Value;
        }

        public bool Equals(TagPattern other) => Axis == other.Axis && Value == other.Value;
        public override bool Equals(object obj) => obj is TagPattern other && Equals(other);
        public override int GetHashCode() => unchecked(((int)Axis * 397) ^ (Value?.GetHashCode() ?? 0));
        public override string ToString() => IsWildcard ? $"{Axis}:*" : $"{Axis}:{Value}";

        public static bool operator ==(TagPattern left, TagPattern right) => left.Equals(right);
        public static bool operator !=(TagPattern left, TagPattern right) => !left.Equals(right);
    }
}
