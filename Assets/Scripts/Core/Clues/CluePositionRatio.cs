using System;

namespace GameName.Core.Clues
{
    // 단서가 방 안 가로로 어디쯤 놓이는지를 나타내는 비율. 0이 방의 왼쪽 끝,
    // 1이 오른쪽 끝이다.
    //
    // 월드 좌표가 아니라 비율로 두는 이유: 방 길이는 밸런싱과 아트에 따라
    // 바뀌는 값인데(MemoryRoomLayout), 저작 데이터가 "x = 2.1" 같은 절대
    // 좌표를 들고 있으면 방을 조금만 늘려도 배치가 통째로 어긋난다. 비율은
    // 방이 커지든 작아지든 "창가 근처"라는 의도를 그대로 유지한다.
    //
    // 이 값이 Core에 있는 이유도 여기 있다. 이것은 화면 좌표가 아니라 기획이
    // 적어 넣는 콘텐츠다 — 창가의 사진, 문 앞에 떨어진 반지처럼 "어디에
    // 놓여 있었는가" 자체가 기억을 읽는 실마리가 될 수 있기 때문이다. 실제
    // 월드 좌표로 바꾸는 계산은 전부 표시 계층(CluePlacementLayout)에 있고,
    // Core는 이 비율 하나만 안다.
    public readonly struct CluePositionRatio : IEquatable<CluePositionRatio>
    {
        public const float Minimum = 0f;
        public const float Maximum = 1f;

        public float Value { get; }

        public CluePositionRatio(float value)
        {
            if (value < Minimum || value > Maximum)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value), value, $"단서 가로 위치는 {Minimum}에서 {Maximum} 사이여야 한다.");
            }

            Value = value;
        }

        // 계산 결과처럼 범위를 살짝 벗어날 수 있는 값을 받아들일 때 쓴다.
        // 생성자가 예외를 던지는 쪽을 기본으로 둔 것은 저작 데이터의 오타를
        // 조용히 0이나 1로 뭉개지 않기 위해서다 — 잘라 내는 것이 옳은 자리는
        // 사람이 적어 넣은 값이 아니라 기계가 계산한 값뿐이다.
        public static CluePositionRatio Clamped(float value)
        {
            if (value < Minimum) return new CluePositionRatio(Minimum);
            if (value > Maximum) return new CluePositionRatio(Maximum);
            return new CluePositionRatio(value);
        }

        // 두 자리가 얼마나 떨어져 있는지. 겹침 검사가 쓴다.
        public float DistanceTo(CluePositionRatio other) => Math.Abs(Value - other.Value);

        public bool Equals(CluePositionRatio other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is CluePositionRatio other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString("0.##");

        public static bool operator ==(CluePositionRatio left, CluePositionRatio right) => left.Equals(right);
        public static bool operator !=(CluePositionRatio left, CluePositionRatio right) => !left.Equals(right);
    }
}
