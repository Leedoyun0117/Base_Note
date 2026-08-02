using System;
using System.Collections.Generic;
using GameName.Core.Journal;

namespace GameName.Core.Dialogue
{
    // 대화 한 노드 — 대사 한 줄 + 다음으로 갈 수 있는 옵션들.
    // Options가 비어 있으면 대화가 거기서 끝난다는 뜻이다.
    public sealed class DialogueNode
    {
        public DialogueLine Line { get; }
        public IReadOnlyList<DialogueOption> Options { get; }

        public DialogueNode(DialogueLine line, IReadOnlyList<DialogueOption> options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Line = line;
        }
    }
}
