namespace GameName.Core.Clues
{
    // 방 데이터 검증 결과의 심각도. 규칙 위반이 항상 명백한 오류는 아니므로
    // 오류와 경고를 구분한다.
    public enum RoomDataIssueSeverity
    {
        Warning,
        Error
    }
}
