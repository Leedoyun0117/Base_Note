namespace GameName.Core.Clues
{
    // 방 데이터 검증에서 발견된 문제 하나.
    public sealed class RoomDataIssue
    {
        public RoomDataIssueSeverity Severity { get; }
        public string Description { get; }

        public RoomDataIssue(RoomDataIssueSeverity severity, string description)
        {
            Severity = severity;
            Description = description;
        }
    }
}
