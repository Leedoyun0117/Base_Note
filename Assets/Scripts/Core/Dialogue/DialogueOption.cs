namespace GameName.Core.Dialogue
{
    // 대사 한 줄 다음에 갈 수 있는 곳 하나.
    // ChoiceLabel이 없으면(null) 그냥 "다음"으로 자동 진행되는 선형 옵션이다.
    // 라벨이 있는 옵션을 한 노드에 여러 개 두면 분기(선택지)가 된다 — 이번
    // 데모 대사는 전부 라벨 없는 옵션 하나씩만 가진 선형 구조라 분기를 실제로
    // 쓰지는 않지만, 특수 의뢰가 분기를 필요로 할 때 이 구조를 그대로 쓸 수
    // 있도록 미리 열어 둔다.
    public readonly struct DialogueOption
    {
        public string ChoiceLabel { get; }
        public int NextNodeIndex { get; }

        public DialogueOption(int nextNodeIndex, string choiceLabel = null)
        {
            NextNodeIndex = nextNodeIndex;
            ChoiceLabel = choiceLabel;
        }
    }
}
