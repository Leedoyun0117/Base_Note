using GameName.Core.Dialogue;

namespace GameName.Core.Events
{
    // 선택지를 골랐다는 사실.
    // 어느 줄에서 골랐는지를 함께 싣는 이유는, 같은 선택지 키가 여러 줄에
    // 재사용될 수 있어 ChoiceId만으로는 맥락이 복원되지 않기 때문이다.
    public readonly struct ChoiceSelectedEvent
    {
        public DialogueLineId LineId { get; }
        public ChoiceId ChoiceId { get; }

        public ChoiceSelectedEvent(DialogueLineId lineId, ChoiceId choiceId)
        {
            LineId = lineId;
            ChoiceId = choiceId;
        }
    }
}
