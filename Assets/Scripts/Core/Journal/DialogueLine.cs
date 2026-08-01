namespace GameName.Core.Journal
{
    // 기록지에 남는 대화 한 줄.
    public readonly struct DialogueLine
    {
        public string Speaker { get; }
        public string Text { get; }

        public DialogueLine(string speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }
    }
}
