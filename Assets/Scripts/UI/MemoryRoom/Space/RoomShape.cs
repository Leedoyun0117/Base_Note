using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 단색 사각형 하나의 중심과 크기. 벽/바닥/천장처럼 "네모 하나"로 그려지는
    // 것들을 계산 코드(RoomGeometry)와 표시 코드(MemoryRoomSpaceView) 사이에서
    // 주고받기 위한 값이다. 색이나 정렬 순서는 담지 않는다 — 그건 표시 쪽의
    // 결정이고, 이 타입은 어디에 얼마만 한 것이 놓이는지만 말한다.
    public readonly struct RoomShape
    {
        public Vector2 Center { get; }
        public Vector2 Size { get; }

        public RoomShape(Vector2 center, Vector2 size)
        {
            Center = center;
            Size = size;
        }
    }
}
