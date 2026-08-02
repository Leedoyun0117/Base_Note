namespace GameName.Core.MemoryRooms
{
    // 기억 방 정답 저장소 경계 — 정답 전체(진실)를 조회할 수 있는 신뢰된 경계다.
    // 시향 처리기처럼 판정을 위해 실제 정답이 반드시 필요한 소비자만 이
    // 인터페이스를 주입받는다. 요구 총량 같은 공개 정보만 필요한 소비자는
    // 대신 IMemoryRoomPublicInfoRepository를 받아야 한다.
    public interface IMemoryRoomAnswerRepository
    {
        bool TryGetAnswer(MemoryRoomId roomId, out MemoryRoomAnswer answer);
    }
}
