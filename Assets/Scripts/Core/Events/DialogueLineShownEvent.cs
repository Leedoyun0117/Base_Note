using GameName.Core.Journal;

namespace GameName.Core.Events
{
    // 대사 한 줄이 화면에 표시될 때(대화가 시작되거나 다음 줄로 넘어갈 때)마다
    // 발행된다. 기록지가 이 이벤트를 구독해 기록하므로, 대화를 보여주는 쪽
    // (DialogueProgressor)이 이 이벤트를 발행하는 유일한 경로다 — 직접
    // IJournal을 호출하는 별도 경로를 두지 않는다.
    public readonly struct DialogueLineShownEvent
    {
        public DialogueLine Line { get; }

        public DialogueLineShownEvent(DialogueLine line)
        {
            Line = line;
        }
    }
}
