using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 드나드는 지점을 방 안 어디에 그릴지 정하는 순수 계산.
    //
    // 문은 좌우 벽에 번갈아 붙인다. 방 하나에 문이 셋 이상 달릴 수도 있는데
    // (데모 의뢰 1의 방 1은 계단·분석실 두 방향으로 열려 있다) 그때도 한쪽에만
    // 몰리지 않게 하기 위함이다. 사다리는 위아래를 잇는 것이므로 벽이 아니라
    // 방 가운데에 세로로 세운다 — 사다리는 걸어 나가는 곳이 아니라 타고
    // 오르내리는 곳이라는 사실이 모양만으로 구분되어야 한다.
    //
    // ── 같은 종류가 여럿일 때 ───────────────────────────────────────────
    // 한 방에 사다리가 둘 달릴 수 있다. 위층과 아래층 양쪽에 이어진 가운데 방이
    // 그렇다(데모 의뢰 1의 방 2가 정확히 그 경우로, 방 1로 내려가는 사다리와
    // 방 3으로 올라가는 사다리를 함께 가진다).
    //
    // 예전에는 사다리 자리를 정할 때 몇 번째인지를 아예 쓰지 않아서 둘이 완전히
    // 포개졌다. 화면에는 하나로 보이고, 판정 영역까지 똑같아서 둘 중 하나는
    // 눌러도 반응하지 않았다 — 그 방에서 한쪽 층으로는 영영 갈 수 없었다.
    // 겹친 것은 눈으로 찾을 수 없으므로(하나처럼 보인다) 애초에 겹칠 수 없게
    // 자리를 나눠 준다.
    public static class RoomExitLayout
    {
        // countOfSameKind는 이 방에 같은 종류가 몇 개인지다. 가운데를 기준으로
        // 대칭이 되게 나눠 세우려면 "몇 번째"만으로는 부족하고 전체 개수를 함께
        // 알아야 한다 — 하나뿐일 때는 정확히 가운데(0)에 서서 예전 배치와 같다.
        public static Vector2 PositionOf(
            MemoryRoomLayout layout, RoomExitKind kind, int indexAmongSameKind, int countOfSameKind)
        {
            if (kind == RoomExitKind.Ladder)
            {
                return new Vector2(
                    LadderX(layout, indexAmongSameKind, countOfSameKind),
                    RoomGeometry.FloorTopY(layout) + layout.RoomHeight / 2f);
            }

            var onLeftSide = indexAmongSameKind % 2 == 0;

            // 같은 벽에 두 번째 문이 붙을 때(문이 셋 이상) 벽에서 안쪽으로 한
            // 칸씩 밀어 둔다. 밀지 않으면 두 문이 정확히 같은 자리에 겹쳐, 앞의
            // 문이 뒤의 문을 완전히 가리고 눌렀을 때 엉뚱한 곳으로 이동한다.
            var indexOnSameWall = indexAmongSameKind / 2;
            var inset = layout.ExitMarkerWidth / 2f + layout.ExitMarkerWidth * indexOnSameWall;

            var x = onLeftSide
                ? RoomGeometry.LeftInnerX(layout) + inset
                : RoomGeometry.RightInnerX(layout) - inset;

            return new Vector2(x, RoomGeometry.FloorTopY(layout) + layout.ExitMarkerHeight / 2f);
        }

        // 사다리들을 방 가운데를 축으로 좌우 대칭이 되게 늘어놓는다. 간격은
        // 사다리 폭의 두 배 — 딱 폭만큼만 벌리면 두 판정 영역이 경계선을 공유해
        // 그 선 위에서는 다시 어느 쪽이 잡힐지 알 수 없어진다. 겹치지 않는다는
        // 것을 눈으로도 확인할 수 있어야 하므로 한 칸을 비워 둔다.
        private static float LadderX(MemoryRoomLayout layout, int indexAmongSameKind, int countOfSameKind)
        {
            var count = Mathf.Max(countOfSameKind, 1);
            var offsetFromCenter = indexAmongSameKind - (count - 1) / 2f;

            return offsetFromCenter * layout.ExitMarkerWidth * 2f;
        }

        public static Vector2 SizeOf(MemoryRoomLayout layout, RoomExitKind kind) =>
            kind == RoomExitKind.Ladder
                ? new Vector2(layout.ExitMarkerWidth, layout.RoomHeight)
                : new Vector2(layout.ExitMarkerWidth, layout.ExitMarkerHeight);
    }
}
