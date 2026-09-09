using GameName.Core.Dialogue;

namespace GameName.Core.Events
{
    // 대사 한 줄에 들어섰다는 사실. 문장이 아니라 식별자만 싣는다 —
    // 원문을 가져와 그리는 일은 표시 계층이 자기 경계에서 한다.
    public readonly struct DialogueLineEnteredEvent
    {
        public DialogueLineId LineId { get; }

        public DialogueLineEnteredEvent(DialogueLineId lineId)
        {
            LineId = lineId;
        }
    }
}
