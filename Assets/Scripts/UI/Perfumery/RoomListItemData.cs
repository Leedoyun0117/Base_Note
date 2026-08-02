using GameName.Core.MemoryRooms;

namespace GameName.UI.Perfumery
{
    // 방 목록을 그리기 위해서만 쓰는 화면 전용 데이터 묶음. Core 타입이 아니다 —
    // 공개 정보(MemoryRoomPublicInfo)에 "복원 여부" 표시용 플래그를 더했을 뿐,
    // 새로운 게임 규칙을 만들지 않는다. IsRestored 값 자체는 항상
    // IMemoryRoomRestorationTracker가 이미 판단해 둔 것을 그대로 옮겨 담는다.
    public readonly struct RoomListItemData
    {
        public MemoryRoomPublicInfo PublicInfo { get; }
        public bool IsRestored { get; }

        public RoomListItemData(MemoryRoomPublicInfo publicInfo, bool isRestored)
        {
            PublicInfo = publicInfo;
            IsRestored = isRestored;
        }
    }
}
