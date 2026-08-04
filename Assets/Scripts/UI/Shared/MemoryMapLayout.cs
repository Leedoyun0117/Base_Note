using GameName.Core.MemoryRooms;
using UnityEngine;

namespace GameName.UI.Shared
{
    // 격자 좌표(MemoryGraphCoordinate) -> 지도 컨테이너 안에서의 로컬 픽셀 좌표
    // 변환만 담당한다. MemoryMapView가 이 계산을 직접 하지 않게 분리했다 —
    // 화면 크기나 해상도는 여기 어디에도 들어오지 않는다(생성자로 받는
    // CellSize/원점은 "지도 한 칸이 몇 픽셀인가"라는 지도 자체의 축척일 뿐,
    // 화면 크기에서 유도되는 값이 아니다). 같은 좌표는 언제나 같은 로컬
    // 위치로 변환된다.
    public sealed class MemoryMapLayout
    {
        private readonly float _cellSize;
        private readonly float _originX;
        private readonly float _originY;

        public MemoryMapLayout(float cellSize, float originX = 0f, float originY = 0f)
        {
            _cellSize = cellSize;
            _originX = originX;
            _originY = originY;
        }

        public Vector2 ToLocalPosition(MemoryGraphCoordinate coordinate) =>
            new Vector2(_originX + coordinate.Column * _cellSize, _originY + coordinate.Row * _cellSize);
    }
}
