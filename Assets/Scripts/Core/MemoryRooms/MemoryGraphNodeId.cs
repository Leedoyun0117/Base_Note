using System;

namespace GameName.Core.MemoryRooms
{
    // 기억 방 그래프에서 노드(기억 방 또는 허브) 하나를 가리키는 식별자.
    public readonly struct MemoryGraphNodeId : IEquatable<MemoryGraphNodeId>
    {
        public string Value { get; }

        public MemoryGraphNodeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("그래프 노드 식별자는 비어 있을 수 없다.", nameof(value));

            Value = value;
        }

        // 기억 방의 MemoryRoomId를 그대로 그래프 노드 식별자로 쓰기 위한 변환.
        // 그래프 밖(정답, 기록지 등)에서 쓰이는 방 식별자와 그래프 안에서 쓰이는
        // 식별자가 서로 다른 문자열이 되어 혼선이 생기는 일을 막기 위해, 방을
        // 가리키는 노드는 항상 이 팩토리를 거쳐 만든다.
        public static MemoryGraphNodeId OfRoom(MemoryRoomId roomId) => new MemoryGraphNodeId(roomId.Value);

        public bool Equals(MemoryGraphNodeId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is MemoryGraphNodeId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value;

        public static bool operator ==(MemoryGraphNodeId left, MemoryGraphNodeId right) => left.Equals(right);
        public static bool operator !=(MemoryGraphNodeId left, MemoryGraphNodeId right) => !left.Equals(right);
    }
}
