namespace GameName.Core.MemoryRooms
{
    // 기억 그래프 검증에서 발견된 문제 하나.
    public sealed class MemoryGraphIssue
    {
        public MemoryGraphIssueSeverity Severity { get; }
        public string Description { get; }

        public MemoryGraphIssue(MemoryGraphIssueSeverity severity, string description)
        {
            Severity = severity;
            Description = description;
        }
    }
}
