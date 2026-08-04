using System;

namespace GameName.Core.MemoryRooms
{
    // 기억 방 그래프 노드의 배치 좌표. 픽셀이 아니라 논리적 격자 칸 하나를
    // 가리키는 정수 좌표라, 화면 크기나 해상도가 바뀌어도 값 자체는 전혀
    // 달라지지 않는다 — 격자를 실제 화면 좌표로 바꾸는 계산은 UI 쪽
    // (MemoryMapLayout)의 몫이다.
    //
    // Row는 세로 위치이자 그대로 시간(과거/현재) 규칙이다 — Row가 클수록
    // 과거, 작을수록 현재다("아래로 갈수록 과거"). 이 값이 커지는 방향과
    // 사다리가 실제로 여는 방향이 어긋나면 데이터 오류이며,
    // LadderRespectsDepthOrderRule이 그 어긋남을 잡아낸다.
    //
    // Column은 가로 위치일 뿐 게임 규칙과 무관하다 — 가로 연결(문)은 조건
    // 없이 통행되므로 Column 값에는 어떤 제약도 없다.
    //
    // Column/Row 둘 다 0 이상이어야 한다 — 이 값 자체(Core)는 음수를 막지
    // 않지만, 지도를 그리는 쪽(MemoryMapView)이 컨테이너 크기를 좌표
    // 최댓값만 보고 정하므로 음수 좌표는 그 컨테이너 밖으로 잘려 그려진다.
    public readonly struct MemoryGraphCoordinate : IEquatable<MemoryGraphCoordinate>
    {
        public int Column { get; }
        public int Row { get; }

        public MemoryGraphCoordinate(int column, int row)
        {
            Column = column;
            Row = row;
        }

        public bool Equals(MemoryGraphCoordinate other) => Column == other.Column && Row == other.Row;
        public override bool Equals(object obj) => obj is MemoryGraphCoordinate other && Equals(other);
        public override int GetHashCode() => (Column * 397) ^ Row;

        public static bool operator ==(MemoryGraphCoordinate left, MemoryGraphCoordinate right) => left.Equals(right);
        public static bool operator !=(MemoryGraphCoordinate left, MemoryGraphCoordinate right) => !left.Equals(right);
    }
}
