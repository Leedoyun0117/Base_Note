using System.Collections.Generic;

namespace GameName.Core.Authoring
{
    // 저작 데이터 검사 규칙 하나. IMemoryGraphConsistencyRule과 같은 조립
    // 방식이다 — 규칙마다 구현체를 두고 검증기가 차례로 돌리므로, 새 규칙을
    // 더해도 검증기 자체는 바뀌지 않는다.
    //
    // 검사 단위가 방이 아니라 판 전체인 이유: 대사가 참조하는 색이 그 방
    // 단서에서 나올 수 있는지처럼, 방 하나만 봐서는 답할 수 없는 규칙이 이미
    // 있고 앞으로 더 늘어난다(방을 건너뛰는 참조 등).
    public interface IDialogueScriptRule
    {
        IReadOnlyList<ScriptIssue> Check(RunDefinition run);
    }
}
