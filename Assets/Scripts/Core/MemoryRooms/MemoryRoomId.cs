using System;

namespace GameName.Core.MemoryRooms
{
    // 기억 방 하나를 식별하는 값. 문자열 기반이라 씬 이름, 데이터 테이블 키 등
    // 어떤 식별 체계를 쓰든 그대로 얹을 수 있다.
    public readonly struct MemoryRoomId : IEquatable<MemoryRoomId>
    {
        public string Value { get; }

        public MemoryRoomId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("방 식별자는 비어 있을 수 없다.", nameof(value));

            Value = value;
        }

        public bool Equals(MemoryRoomId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is MemoryRoomId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value;

        public static bool operator ==(MemoryRoomId left, MemoryRoomId right) => left.Equals(right);
        public static bool operator !=(MemoryRoomId left, MemoryRoomId right) => !left.Equals(right);
    }
}
