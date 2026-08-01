namespace GameName.Core.MemoryRooms
{
    // 기억 방과 관련해 플레이어에게 그대로 공개해도 되는 정보.
    // 정답 향(Scent)을 포함하지 않으므로, 이 타입만으로는 절대 정답을 역추적할 수
    // 없다 — MemoryRoomAnswer.ToPublicInfo()를 통해서만 만들어지는 단방향 변환이다.
    public readonly struct MemoryRoomPublicInfo
    {
        public MemoryRoomId RoomId { get; }
        public int RequiredSupportingIntensityTotal { get; }

        public MemoryRoomPublicInfo(MemoryRoomId roomId, int requiredSupportingIntensityTotal)
        {
            RoomId = roomId;
            RequiredSupportingIntensityTotal = requiredSupportingIntensityTotal;
        }
    }
}
