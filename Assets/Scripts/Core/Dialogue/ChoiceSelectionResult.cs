namespace GameName.Core.Dialogue
{
    // 선택지 선택 처리의 결과.
    public sealed class ChoiceSelectionResult
    {
        public ChoiceSelectionOutcome Outcome { get; }

        public bool Succeeded => Outcome == ChoiceSelectionOutcome.Advanced
            || Outcome == ChoiceSelectionOutcome.DialogueEnded;

        private ChoiceSelectionResult(ChoiceSelectionOutcome outcome)
        {
            Outcome = outcome;
        }

        public static ChoiceSelectionResult Advanced() =>
            new ChoiceSelectionResult(ChoiceSelectionOutcome.Advanced);

        public static ChoiceSelectionResult DialogueEnded() =>
            new ChoiceSelectionResult(ChoiceSelectionOutcome.DialogueEnded);

        public static ChoiceSelectionResult Rejected() =>
            new ChoiceSelectionResult(ChoiceSelectionOutcome.Rejected);

        public static ChoiceSelectionResult Ignored() =>
            new ChoiceSelectionResult(ChoiceSelectionOutcome.Ignored);
    }
}
