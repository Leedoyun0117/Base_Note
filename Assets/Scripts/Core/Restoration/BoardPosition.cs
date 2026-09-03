using System;

namespace GameName.Core.Restoration
{
    // 복원도 위에서 노드가 놓인 자리.
    //
    // UnityEngine.Vector2를 쓰지 않는 것은 Core가 엔진에 의존하지 않기 때문이다 —
    // 화면 쪽은 이 값을 자기 좌표계의 Vector2로 옮겨 쓴다. Core에는 배치의
    // 의미(어느 노드가 어디에)만 있으면 되고, 픽셀·단위 해석은 화면의 몫이다.
    public readonly struct BoardPosition : IEquatable<BoardPosition>
    {
        public float X { get; }
        public float Y { get; }

        public BoardPosition(float x, float y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(BoardPosition other) =>
            X.Equals(other.X) && Y.Equals(other.Y);

        public override bool Equals(object obj) => obj is BoardPosition other && Equals(other);
        public override int GetHashCode() => (X.GetHashCode() * 397) ^ Y.GetHashCode();
        public override string ToString() => $"({X}, {Y})";

        public static bool operator ==(BoardPosition left, BoardPosition right) => left.Equals(right);
        public static bool operator !=(BoardPosition left, BoardPosition right) => !left.Equals(right);
    }
}
