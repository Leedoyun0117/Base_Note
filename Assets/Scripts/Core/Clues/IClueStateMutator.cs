namespace GameName.Core.Clues
{
    // 단서의 단계를 실제로 옮길 수 있는 경계.
    // 수집·대사 사용·추출 각각의 처리기에만 주입한다. 단계 전이가 유효한지
    // (예: Available에서 바로 Extracted로 갈 수 있는가) 판단하는 책임은
    // 구현체에 있고, 호출자는 자신이 만든 사건만 통보한다.
    public interface IClueStateMutator : IClueStateReader
    {
        void SetState(ClueId clueId, ClueState state);
    }
}
