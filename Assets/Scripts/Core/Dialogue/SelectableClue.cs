using GameName.Core.Clues;

namespace GameName.Core.Dialogue
{
    // ClueSelection 줄에서 답으로 낼 수 있는 손에 든 단서 하나 — 화면이 목록을
    // 그릴 때 필요한 것만 담는다.
    //
    // MemoryExtracted를 함께 싣는 이유: 이 줄에서 단서를 답으로 내미는 것과
    // 그 자리에서 기억을 추출하는 것은 별개 조작인데, 이미 추출한 단서에는
    // 추출 버튼을 다시 띄우지 않아야 한다. 그 판단의 근거를 화면이 ClueState를
    // 직접 뒤지지 않고 여기서 받는다.
    public readonly struct SelectableClue
    {
        public ClueId Id { get; }
        public string DisplayName { get; }
        public bool MemoryExtracted { get; }

        public SelectableClue(ClueId id, string displayName, bool memoryExtracted)
        {
            Id = id;
            DisplayName = displayName;
            MemoryExtracted = memoryExtracted;
        }
    }
}
