namespace GameName.Core.MemoryRooms
{
    // 기억 그래프 검증 결과의 심각도. RoomDataIssueSeverity(GameName.Core.Clues)와
    // 개념은 같지만 별도로 둔다 — 서로 다른 도메인(방 데이터 vs 그래프 구조)이
    // 우연히 같은 모양이라는 이유만으로 의존을 만들지 않기 위함이다.
    public enum MemoryGraphIssueSeverity
    {
        Warning,
        Error
    }
}
