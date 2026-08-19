using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 방 치수 하나에서 실제 좌표를 뽑아내는 순수 계산.
    //
    // 벽을 세우는 코드, 플레이어를 가두는 코드, 단서를 놓는 코드가 각자
    // "왼쪽 벽은 -길이/2쯤이겠지" 하고 따로 계산하면 셋이 조금씩 어긋난다.
    // 좌표 규칙은 여기 한 곳에만 둔다. MonoBehaviour가 아니므로 씬 없이
    // 검증할 수 있다.
    public static class RoomGeometry
    {
        // 바닥 윗면과 천장 아랫면. 좌표계 약속상 바닥 윗면이 y = 0이다.
        public static float FloorTopY(MemoryRoomLayout layout) => 0f;
        public static float CeilingY(MemoryRoomLayout layout) => layout.RoomHeight;

        public static float LeftInnerX(MemoryRoomLayout layout) => -layout.RoomLength / 2f;
        public static float RightInnerX(MemoryRoomLayout layout) => layout.RoomLength / 2f;

        // 플레이어와 단서가 실제로 존재할 수 있는 가로 구간. 벽에 딱 붙어
        // 반쯤 파묻힌 것처럼 보이지 않도록 양쪽에 여백을 남긴다.
        public static float MinInteriorX(MemoryRoomLayout layout) => LeftInnerX(layout) + layout.EdgeMargin;
        public static float MaxInteriorX(MemoryRoomLayout layout) => RightInnerX(layout) - layout.EdgeMargin;

        public static float ClampInteriorX(MemoryRoomLayout layout, float x) =>
            Mathf.Clamp(x, MinInteriorX(layout), MaxInteriorX(layout));

        // 바닥은 방 아래쪽으로 두께만큼 자란다 — 방 내부 높이(RoomHeight)를
        // 두께가 갉아먹지 않게 하기 위함이다. 천장도 같은 이유로 위로 자란다.
        public static RoomShape Floor(MemoryRoomLayout layout) =>
            new RoomShape(
                new Vector2(0f, -layout.WallThickness / 2f),
                new Vector2(layout.RoomLength + layout.WallThickness * 2f, layout.WallThickness));

        public static RoomShape Ceiling(MemoryRoomLayout layout) =>
            new RoomShape(
                new Vector2(0f, layout.RoomHeight + layout.WallThickness / 2f),
                new Vector2(layout.RoomLength + layout.WallThickness * 2f, layout.WallThickness));

        public static RoomShape LeftWall(MemoryRoomLayout layout) =>
            new RoomShape(
                new Vector2(LeftInnerX(layout) - layout.WallThickness / 2f, layout.RoomHeight / 2f),
                new Vector2(layout.WallThickness, layout.RoomHeight));

        public static RoomShape RightWall(MemoryRoomLayout layout) =>
            new RoomShape(
                new Vector2(RightInnerX(layout) + layout.WallThickness / 2f, layout.RoomHeight / 2f),
                new Vector2(layout.WallThickness, layout.RoomHeight));

        // 뒷벽 — 2D 사이드뷰에서 포스터가 붙는 면이다. 방 내부를 그대로 덮는
        // 배경 판이라 두께가 없다.
        public static RoomShape BackWall(MemoryRoomLayout layout) =>
            new RoomShape(
                new Vector2(0f, layout.RoomHeight / 2f),
                new Vector2(layout.RoomLength, layout.RoomHeight));

        // 플레이어는 발이 바닥에 닿아야 하므로 중심이 키의 절반만큼 떠 있다.
        public static Vector2 PlayerPosition(MemoryRoomLayout layout, float x) =>
            new Vector2(ClampInteriorX(layout, x), FloorTopY(layout) + layout.PlayerHeight / 2f);
    }
}
