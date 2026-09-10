namespace GameName.Core.Authoring
{
    // 검증기를 조립하는 유일한 자리.
    //
    // 어떤 규칙들로 검사하는가가 여기 한 곳에만 적혀 있어야, 규칙이 늘 때 고칠
    // 곳이 하나로 남는다. 저작 도구(에디터 메뉴)든 테스트든 전부 이 조립을
    // 거치게 해서 "에디터에서는 통과했는데 테스트에서는 걸린다"가 생기지 않게
    // 한다 — 그래서 수치를 에셋이 아니라 숫자로 받는다.
    //
    // 3차 개편에서 대사 관련 규칙(정답 선택지·다음 줄·단서로 답하는 줄·분기 풀)이
    // 전부 빠졌다. 남은 것은 라운드 개수와 단서 배치 검사뿐이다.
    public static class DialogueScriptValidatorFactory
    {
        public static DialogueScriptValidator Create(int expectedRoomCount, float minimumClueSeparation)
        {
            return new DialogueScriptValidator(new IDialogueScriptRule[]
            {
                new RoomCountRule(expectedRoomCount),
                new CluePositionOverlapRule(minimumClueSeparation),
                new ClueDisplayNameExistsRule(),
            });
        }
    }
}
