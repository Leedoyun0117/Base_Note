using System;

namespace GameName.Core.Clues
{
    // 단서 하나를 식별하는 값.
    public readonly struct ClueId : IEquatable<ClueId>
    {
        public string Value { get; }

        public ClueId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("단서 식별자는 비어 있을 수 없다.", nameof(value));

            Value = value;
        }

        public bool Equals(ClueId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ClueId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value;

        public static bool operator ==(ClueId left, ClueId right) => left.Equals(right);
        public static bool operator !=(ClueId left, ClueId right) => !left.Equals(right);
    }
}
