using System.Collections.Generic;

namespace GameName.Core.Dialogue
{
    // 저작된 대사 데이터를 읽어 오는 유일한 경계.
    //
    // Core에는 문장이 한 글자도 없다. 대사와 선택지 문구는 전부 이 경계 너머의
    // 저작 데이터에 있고, Core는 식별자로만 가리킨다. 반환되는 문자열은 아직
    // 검열이 표기된 채인 "원문"이며, 화면에 나가기 전에
    // ICensoredTextParser와 ICensorResolver를 거친다.
    //
    // 없는 식별자는 예외가 아니라 false다 — 저작 데이터의 누락은 진행 중
    // 흔히 생기는 일이라 호출부가 이어서 처리할 수 있어야 한다.
    public interface IDialogueScriptReader
    {
        bool TryGetLineText(DialogueLineId lineId, out string authoredText);

        bool TryGetChoiceText(ChoiceId choiceId, out string authoredText);

        IReadOnlyList<ChoiceId> GetChoicesAt(DialogueLineId lineId);
    }
}
