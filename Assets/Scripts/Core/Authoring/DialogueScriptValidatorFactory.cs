using GameName.Core.Dialogue;

namespace GameName.Core.Authoring
{
    // 검증기를 조립하는 유일한 자리.
    //
    // 어떤 규칙들로 검사하는가가 여기 한 곳에만 적혀 있어야, 규칙이 늘 때 고칠
    // 곳이 하나로 남는다. 저작 도구(에디터 메뉴)든 테스트든 전부 이 조립을
    // 거치게 해서 "에디터에서는 통과했는데 테스트에서는 걸린다"가 생기지 않게
    // 한다 — 그래서 수치를 에셋이 아니라 숫자로 받는다. 에셋을 받으면 Core가
    // UnityEngine에 묶여 테스트가 이 조립을 쓸 수 없다.
    public static class DialogueScriptValidatorFactory
    {
        public static DialogueScriptValidator Create(int expectedRoomCount, float minimumClueSeparation)
        {
            // 검열을 보는 규칙들이 토큰 목록 하나를 나눠 쓴다. 규칙마다 따로
            // 주면 같은 원문을 규칙 수만큼 다시 파싱한다.
            var tokens = new CensorTokenIndexSource(new CensoredTextParser());

            return new DialogueScriptValidator(new IDialogueScriptRule[]
            {
                new RoomCountRule(expectedRoomCount),
                new NextLineExistsRule(),
                new CorrectChoiceExistsRule(),
                new BranchPoolIntegrityRule(),
                new CensoredColorRevealableRule(tokens),
                new CensorKeyColorConsistencyRule(tokens),
                new ChoiceConditionCensorKeyExistsRule(tokens),
                new CluePositionOverlapRule(minimumClueSeparation),
                new ClueDisplayNameExistsRule(),
                new ClueSelectionLineRule(),
            });
        }
    }
}
