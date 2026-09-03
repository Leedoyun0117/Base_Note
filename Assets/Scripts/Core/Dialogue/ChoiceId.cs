using System;

namespace GameName.Core.Dialogue
{
    // 선택지 하나를 식별하는 값.
    // 선택지 문구가 아니라 키인 이유는 DialogueLineId와 같다.
    public readonly struct ChoiceId : IEquatable<ChoiceId>
    {
        public string Value { get; }

        public ChoiceId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("선택지 식별자는 비어 있을 수 없다.", nameof(value));

            Value = value;
        }

        public bool Equals(ChoiceId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ChoiceId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value;

        public static bool operator ==(ChoiceId left, ChoiceId right) => left.Equals(right);
        public static bool operator !=(ChoiceId left, ChoiceId right) => !left.Equals(right);
    }
}
