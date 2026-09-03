using System;

namespace GameName.Core.Restoration
{
    // 복원도 간선 하나를 식별하는 값.
    public readonly struct RestorationEdgeId : IEquatable<RestorationEdgeId>
    {
        public string Value { get; }

        public RestorationEdgeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("복원도 간선 식별자는 비어 있을 수 없다.", nameof(value));

            Value = value;
        }

        public bool Equals(RestorationEdgeId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is RestorationEdgeId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value;

        public static bool operator ==(RestorationEdgeId left, RestorationEdgeId right) => left.Equals(right);
        public static bool operator !=(RestorationEdgeId left, RestorationEdgeId right) => !left.Equals(right);
    }
}
