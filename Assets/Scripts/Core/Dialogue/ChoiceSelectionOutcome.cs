namespace GameName.Core.Dialogue
{
    // 선택지를 골랐을 때 대화가 어떻게 되었는지.
    public enum ChoiceSelectionOutcome
    {
        // 다음 대사로 넘어갔다.
        Advanced,

        // 다음 대사가 없어 대화가 끝났다.
        DialogueEnded,

        // 지금 이 라인에 표시 중인 선택지가 아니라 무시했다(없는 키, 조건 불충족 등).
        Rejected,

        // 신뢰도가 이미 0이라 방이 실패로 닫힌 뒤다. 선택 입력을 받지 않는다.
        Ignored
    }
}
