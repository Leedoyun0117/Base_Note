using System;

namespace GameName.Core.Dialogue
{
    // 검열된 구간 하나를 식별하는 값.
    //
    // 구간의 위치(몇 번째 글자부터)가 아니라 키인 이유: 같은 사실이 여러 대사에
    // 걸쳐 가려져 있을 수 있고, 그 사실 하나가 풀리면 전부 함께 드러나야 한다.
    // 즉 검열이 걸린 단위는 "문장 속 자리"가 아니라 "감춰진 사실"이다.
    //
    // 색이 아니라 키로 푸는 이유도 여기 있다. 색으로 풀면 파란 단서 하나를
    // 추출한 순간 그 방의 파란 검열이 전부 함께 열려, 무엇을 추출할지 고르는
    // 선택이 "색을 모으는 일"로 뭉개진다. 키가 단위이면 추출 하나가 여는 것은
    // 사실 하나다.
    public readonly struct CensorKey : IEquatable<CensorKey>
    {
        public string Value { get; }

        public CensorKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("검열 식별자는 비어 있을 수 없다.", nameof(value));

            Value = value;
        }

        public bool Equals(CensorKey other) => Value == other.Value;
        public override bool Equals(object obj) => obj is CensorKey other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value;

        public static bool operator ==(CensorKey left, CensorKey right) => left.Equals(right);
        public static bool operator !=(CensorKey left, CensorKey right) => !left.Equals(right);
    }
}
