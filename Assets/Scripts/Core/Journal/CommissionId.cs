using System;

namespace GameName.Core.Journal
{
    // 의뢰(누구의 기억인가) 하나를 식별하는 값. 기록지는 이 값으로 기록을
    // 서로 다른 의뢰끼리 섞이지 않게 나눈다. 표시용 이름을 따로 두지 않고
    // Value 자체를 화면에 보여줄 수 있는 문자열로 취급한다 — 의뢰인 이름
    // registry 같은 별도 개념을 아직 도입할 필요가 없기 때문이다.
    public readonly struct CommissionId : IEquatable<CommissionId>
    {
        public string Value { get; }

        public CommissionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("의뢰 식별자는 비어 있을 수 없다.", nameof(value));

            Value = value;
        }

        public bool Equals(CommissionId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is CommissionId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value;

        public static bool operator ==(CommissionId left, CommissionId right) => left.Equals(right);
        public static bool operator !=(CommissionId left, CommissionId right) => !left.Equals(right);
    }
}
