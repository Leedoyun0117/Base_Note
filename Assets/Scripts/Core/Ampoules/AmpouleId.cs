using System;

namespace GameName.Core.Ampoules
{
    // 앰플 한 개(물리적 개체)를 식별하는 값.
    public readonly struct AmpouleId : IEquatable<AmpouleId>
    {
        public string Value { get; }

        public AmpouleId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("앰플 식별자는 비어 있을 수 없다.", nameof(value));

            Value = value;
        }

        public bool Equals(AmpouleId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is AmpouleId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value;

        public static bool operator ==(AmpouleId left, AmpouleId right) => left.Equals(right);
        public static bool operator !=(AmpouleId left, AmpouleId right) => !left.Equals(right);
    }
}
