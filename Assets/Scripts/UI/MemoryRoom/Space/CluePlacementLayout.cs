using GameName.Core.Clues;
using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 단서를 방 안 어디에 놓을지 정하는 순수 계산.
    //
    // 가로와 세로가 서로 다른 곳에서 온다:
    //   · 가로 — 저작 데이터(CluePositionRatio)가 정한다. 방 길이에 대한
    //     비율이라 방 크기가 바뀌어도 의도가 유지된다.
    //   · 세로 — 종류가 정한다. 포스터는 벽 높이, 바닥 물건은 바닥이다.
    //     기획이 고를 값이 아니므로 저작 데이터에 두지 않는다.
    //
    // 예전에는 가로도 여기서 계산했다 — 방에 남은 단서 개수로 균등 분배하는
    // 방식이었는데, 그러면 단서 하나를 집는 순간 남은 단서들이 저절로 자리를
    // 옮겼다. 방에 놓인 물건이 스스로 움직이는 셈이라 공간을 믿을 수 없게
    // 되고, "창가의 사진" 같은 의도적 배치도 만들 수 없었다.
    public static class CluePlacementLayout
    {
        // 포스터는 벽에 걸리므로 바닥에서 떨어진 고정 높이에, 바닥 물건은
        // 바닥에 얹혀 보이도록 제 크기의 절반만큼만 떠 있다.
        public static float HeightOf(MemoryRoomLayout layout, ClueKind kind) =>
            kind == ClueKind.Poster
                ? RoomGeometry.FloorTopY(layout) + layout.PosterMountHeight
                : RoomGeometry.FloorTopY(layout) + layout.FloorObjectSize / 2f;

        public static float SizeOf(MemoryRoomLayout layout, ClueKind kind) =>
            kind == ClueKind.Poster ? layout.PosterSize : layout.FloorObjectSize;

        // 단서가 놓일 수 있는 가로 구간의 길이. 비율 한 칸이 실제로 몇 유닛인지를
        // 알아야 "겹치지 않는 간격"을 비율로 환산할 수 있다.
        public static float InteriorLength(MemoryRoomLayout layout) =>
            RoomGeometry.MaxInteriorX(layout) - RoomGeometry.MinInteriorX(layout);

        // 같은 종류의 단서 둘이 서로 닿지 않으려면 비율로 얼마나 떨어져야 하는가.
        // 같은 종류는 크기가 같으므로 중심 사이가 크기 하나만큼 벌어지면 정확히
        // 맞닿는다 — 그 값을 방 길이로 나눠 비율 단위로 바꾼다. 숫자를 따로
        // 정하지 않고 크기에서 끌어내는 이유는, 단서 크기를 조정했을 때 간격
        // 규칙이 따라 움직이지 않으면 곧 어긋나기 때문이다.
        public static float MinimumSeparationRatio(MemoryRoomLayout layout, ClueKind kind) =>
            SizeOf(layout, kind) / InteriorLength(layout);

        // 비율을 실제 가로 좌표로 편다. 벽에 반쯤 파묻히지 않도록 여백을 뺀
        // 안쪽 구간(MinInteriorX ~ MaxInteriorX)에 대응시킨다 — 그래서 비율 0도
        // 벽을 뚫지 않는다.
        public static float XOf(MemoryRoomLayout layout, CluePositionRatio ratio) =>
            Mathf.Lerp(RoomGeometry.MinInteriorX(layout), RoomGeometry.MaxInteriorX(layout), ratio.Value);

        // XOf의 역방향. 플레이어가 서 있던 자리를 비율로 되돌려 기억해 두는 데
        // 쓴다 — 자리를 월드 좌표로 보관하면 방 크기가 바뀌었을 때 엉뚱한 곳을
        // 가리키게 된다.
        public static CluePositionRatio RatioOf(MemoryRoomLayout layout, float x)
        {
            var ratio = Mathf.InverseLerp(
                RoomGeometry.MinInteriorX(layout), RoomGeometry.MaxInteriorX(layout), x);

            return new CluePositionRatio(Mathf.Clamp01(ratio));
        }

        public static Vector2 PositionAt(MemoryRoomLayout layout, ClueKind kind, CluePositionRatio ratio) =>
            new Vector2(XOf(layout, ratio), HeightOf(layout, kind));
    }
}
