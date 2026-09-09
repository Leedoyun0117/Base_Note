using GameName.Core.Clues;

namespace GameName.Core.Memories
{
    // 추출된 기억을 실제로 넣고 뺄 수 있는 경계.
    //
    // Remove는 대화에 답으로 낸 기억이 소모될 때 쓰인다 — 그 쓰기 트리거를
    // 대화 페이즈로 옮기는 것은 [7]이고, 지금은 호출자가 없다.
    public interface IExtractedMemoryStoreMutator : IExtractedMemoryStore
    {
        void Add(ExtractedMemory memory);

        // 소모된 기억을 지운다. 없는 출처를 지우려 해도 조용히 무시한다 —
        // 호출자가 이미 TryGet으로 존재를 확인한 뒤에만 부른다.
        void Remove(ClueId sourceClueId);
    }
}
