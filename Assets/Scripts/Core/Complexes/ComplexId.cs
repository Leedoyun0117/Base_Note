using System;

namespace GameName.Core.Complexes
{
    // 컴플렉스 하나를 식별하는 값. ClueId·MemoryRoomId와 같은 문자열 기반
    // 식별자라 저작 데이터 키, 해석 로그의 단계 표시 등 어디에나 그대로 얹는다.
    public readonly struct ComplexId : IEquatable<ComplexId>
    {
        public string Value { get; }

        public ComplexId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("컴플렉스 식별자는 비어 있을 수 없다.", nameof(value));

            Value = value;
        }

        public bool Equals(ComplexId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ComplexId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value;

        public static bool operator ==(ComplexId left, ComplexId right) => left.Equals(right);
        public static bool operator !=(ComplexId left, ComplexId right) => !left.Equals(right);
    }
}
