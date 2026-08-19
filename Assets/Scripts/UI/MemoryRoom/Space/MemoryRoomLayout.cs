using System;

namespace GameName.UI.MemoryRoom.Space
{
    // 기억 방 한 칸을 실제 공간으로 그릴 때 필요한 치수 묶음(월드 유닛).
    //
    // 씬에 숫자를 박아 두지 않기 위한 타입이다. 방 크기·플레이어 크기·단서가
    // 걸리는 높이 같은 값은 전부 밸런싱과 아트 교체의 대상이라, 오브젝트를
    // 만드는 코드가 스스로 정하면 안 된다. 실제 값은 MemoryRoomLayoutAsset에
    // 담아 인스펙터에서 조정하고, 이 불변 타입으로 변환해 주입한다 —
    // ScriptableObject 자체를 그대로 넘기지 않는 이유는 계산 코드(RoomGeometry,
    // CluePlacementLayout)가 Unity 에셋 없이도 검증 가능해야 하기 때문이다.
    //
    // 좌표계 약속: 방의 가로 중앙이 x = 0이고, 바닥 윗면이 y = 0이다.
    // 방 내부는 y가 0에서 RoomHeight까지, x가 -RoomLength/2에서 +RoomLength/2까지다.
    public sealed class MemoryRoomLayout
    {
        // 플레이어 키 1을 기준으로 한 방 치수 — 기본값은 높이 4, 길이 7이다.
        public float RoomHeight { get; }
        public float RoomLength { get; }

        // 벽/바닥/천장을 그릴 때의 두께. 공간 자체의 크기에는 영향을 주지 않고
        // 테두리를 얼마나 두껍게 보이게 할지만 정한다.
        public float WallThickness { get; }

        public float PlayerHeight { get; }
        public float PlayerWidth { get; }
        public float PlayerMoveSpeed { get; }

        // 플레이어와 단서가 벽에 딱 붙지 않도록 남기는 좌우 여백.
        public float EdgeMargin { get; }

        // 포스터가 벽에 걸리는 높이(바닥에서 포스터 중심까지)와 크기.
        public float PosterMountHeight { get; }
        public float PosterSize { get; }

        // 바닥에 놓이는 물건의 크기. 바닥에 얹혀 보이도록 중심 높이는 이 값의
        // 절반이 된다(CluePlacementLayout이 계산한다).
        public float FloorObjectSize { get; }

        // 방을 드나드는 지점(문/사다리) 표시의 크기.
        public float ExitMarkerWidth { get; }
        public float ExitMarkerHeight { get; }

        // 카메라가 방을 담을 때 위아래로 남기는 여백. 방을 어떻게 보여줄지도
        // 방 규격의 일부라 여기 함께 둔다 — 씬 구성 도구가 카메라 크기를
        // 스스로 정해 버리면 방 높이를 바꿨을 때 화면만 따로 어긋난다.
        public float CameraVerticalMargin { get; }

        public MemoryRoomLayout(
            float roomHeight,
            float roomLength,
            float wallThickness,
            float playerHeight,
            float playerWidth,
            float playerMoveSpeed,
            float edgeMargin,
            float posterMountHeight,
            float posterSize,
            float floorObjectSize,
            float exitMarkerWidth,
            float exitMarkerHeight,
            float cameraVerticalMargin)
        {
            if (roomHeight <= 0) throw new ArgumentOutOfRangeException(nameof(roomHeight));
            if (roomLength <= 0) throw new ArgumentOutOfRangeException(nameof(roomLength));

            RoomHeight = roomHeight;
            RoomLength = roomLength;
            WallThickness = wallThickness;
            PlayerHeight = playerHeight;
            PlayerWidth = playerWidth;
            PlayerMoveSpeed = playerMoveSpeed;
            EdgeMargin = edgeMargin;
            PosterMountHeight = posterMountHeight;
            PosterSize = posterSize;
            FloorObjectSize = floorObjectSize;
            ExitMarkerWidth = exitMarkerWidth;
            ExitMarkerHeight = exitMarkerHeight;
            CameraVerticalMargin = cameraVerticalMargin;
        }
    }
}
