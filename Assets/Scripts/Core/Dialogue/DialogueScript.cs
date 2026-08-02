using System;
using System.Collections.Generic;

namespace GameName.Core.Dialogue
{
    // 대화 전체 — 노드 목록. 항상 인덱스 0부터 시작한다.
    // 코드에 대사를 박지 않기 위한 외부 데이터 컨테이너다 — 실제 대사 내용은
    // 이 타입을 만드는 쪽(지금은 DemoGameData, 나중에는 실제 콘텐츠 소스)에서
    // 채운다.
    public sealed class DialogueScript
    {
        public IReadOnlyList<DialogueNode> Nodes { get; }

        public DialogueScript(IReadOnlyList<DialogueNode> nodes)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (nodes.Count == 0) throw new ArgumentException("대화는 최소 한 줄이 있어야 한다.", nameof(nodes));

            Nodes = nodes;
        }
    }
}
