namespace GameName.Core.Authoring
{
    // 저작 데이터 검증에서 발견된 문제 하나.
    public sealed class ScriptIssue
    {
        public ScriptIssueSeverity Severity { get; }
        public string Description { get; }

        public ScriptIssue(ScriptIssueSeverity severity, string description)
        {
            Severity = severity;
            Description = description;
        }
    }
}
