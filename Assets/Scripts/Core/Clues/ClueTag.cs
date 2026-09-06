using System;

namespace GameName.Core.Clues
{
    // 단서 하나에 붙는 저작 태그(예: "Yuki.toy", "Yuki.letter").
    //
    // 정답 판정의 단위는 색이 아니라 이 태그다. 색은 추출했을 때 드러나는
    // 힌트일 뿐이고("이 방향이겠구나" 정도의 단서), 실제로 무엇을 묻는 질문에
    // 맞는 답인지는 태그가 정한다 — 같은 색이어도 태그가 다르면 답이 아니다.
    //
    // 문자열 그대로 두지 않고 타입으로 감싼 이유는 CensorKey·ClueId와 같다:
    // 오타 하나가 "영영 맞힐 수 없는 질문"을 만들 수 있는 값이라, 어디서나
    // 같은 비교 규칙(대소문자, 트림 등 없이 정확히 일치)이 강제되어야 한다.
    public readonly struct ClueTag : IEquatable<ClueTag>
    {
        public string Value { get; }

        public ClueTag(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("단서 태그는 비어 있을 수 없다.", nameof(value));

            Value = value;
        }

        public bool Equals(ClueTag other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ClueTag other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value;

        public static bool operator ==(ClueTag left, ClueTag right) => left.Equals(right);
        public static bool operator !=(ClueTag left, ClueTag right) => !left.Equals(right);
    }
}
