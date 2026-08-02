using System.Collections.Generic;
using GameName.Core.Journal;

namespace GameName.Core.Dialogue
{
    // 지금 어느 대사를 보여주고 있는지 추적하는 경계.
    public interface IDialogueProgressor
    {
        DialogueLine CurrentLine { get; }
        IReadOnlyList<DialogueOption> CurrentOptions { get; }

        // 옵션이 하나도 없으면(대화가 끝난 노드) true다.
        bool IsFinished { get; }

        // optionIndex번째 옵션을 선택해 다음 노드로 진행한다. 범위를 벗어난
        // 인덱스면(이미 끝났을 때 포함) 아무 일도 하지 않고 false를 반환한다.
        bool Advance(int optionIndex);
    }
}
