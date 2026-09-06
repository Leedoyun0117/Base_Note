using GameName.Core.Clues;

namespace GameName.Core.Memories
{
    // 추출된 기억을 실제로 넣고 뺄 수 있는 경계.
    // 추출 처리기(Add)와 검열 해금 처리기(Remove)에만 주입한다.
    public interface IExtractedMemoryStoreMutator : IExtractedMemoryStore
    {
        void Add(ExtractedMemory memory);

        // 검열 해금에 제시되어 소모된 기억을 지운다. 없는 출처를 지우려 해도
        // 조용히 무시한다 — 호출자(해금 처리기)가 이미 TryGet으로 존재를
        // 확인한 뒤에만 부르므로, 여기서 다시 검사할 이유가 없다.
        void Remove(ClueId sourceClueId);
    }
}
