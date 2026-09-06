namespace GameName.Core.Dialogue
{
    // 대사 한 줄이 플레이어에게 무엇을 묻는가.
    //
    // 기본값이 TextChoice인 것이 중요하다 — 지금까지의 모든 저작 데이터는 이
    // 종류이고, 새 값이 생겼다는 이유로 기존 줄이 다르게 동작해서는 안 된다.
    public enum DialoguePromptKind
    {
        // 지금까지의 방식: 걸러진 텍스트 선택지 목록에서 하나를 고른다.
        TextChoice,

        // "이 질문엔 가진 단서로 답하라": 들고 있는(Collected) 단서 목록에서
        // 하나를 골라 답한다. 그 단서의 태그가 정답 태그 집합(RequiredTags)에
        // 걸치면 CorrectNext로, 아니면 IncorrectNext(오답 서브체인)로 간다.
        // 이 종류의 줄에는 텍스트 선택지를 두지 않는다.
        ClueSelection
    }
}
