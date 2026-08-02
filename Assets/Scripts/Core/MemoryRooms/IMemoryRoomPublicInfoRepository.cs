namespace GameName.Core.MemoryRooms
{
    // 기억 방 공개 정보 조회 경계. 정답(CorrectScent)은 담지 않는
    // MemoryRoomPublicInfo만 돌려준다 — 조향 처리기처럼 "요구 총량"만 알면
    // 되는 소비자가 정답 전체를 손에 쥐지 않도록 하기 위한 것이다.
    public interface IMemoryRoomPublicInfoRepository
    {
        bool TryGetPublicInfo(MemoryRoomId roomId, out MemoryRoomPublicInfo info);
    }
}
