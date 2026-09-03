using System;

namespace GameName.Core.Restoration
{
    // 복원도 노드 하나를 식별하는 값.
    public readonly struct RestorationNodeId : IEquatable<RestorationNodeId>
    {
        public string Value { get; }

        public RestorationNodeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("복원도 노드 식별자는 비어 있을 수 없다.", nameof(value));

            Value = value;
        }

        public bool Equals(RestorationNodeId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is RestorationNodeId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value;

        public static bool operator ==(RestorationNodeId left, RestorationNodeId right) => left.Equals(right);
        public static bool operator !=(RestorationNodeId left, RestorationNodeId right) => !left.Equals(right);
    }
}
