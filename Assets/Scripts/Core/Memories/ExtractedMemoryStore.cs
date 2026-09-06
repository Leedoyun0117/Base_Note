using System;
using System.Collections.Generic;
using GameName.Core.Clues;

namespace GameName.Core.Memories
{
    // IExtractedMemoryStoreMutator 기본 구현. 지금 손에 든 추출된 기억을
    // 출처 단서 id로 든다.
    //
    // 방이 바뀌어도 리셋되지 않는다 — 옛 IMemoryColorWallet과 같은 스코프다.
    // 추출은 런 전체에 걸쳐 유한한 자원(히로민)을 쓰므로, 그렇게 얻은 기억도
    // 런 전체에 걸쳐 살아남아야 다음 방의 검열에 쓸 수 있다.
    public sealed class ExtractedMemoryStore : IExtractedMemoryStoreMutator
    {
        private readonly Dictionary<ClueId, ExtractedMemory> _byClueId = new Dictionary<ClueId, ExtractedMemory>();

        public IReadOnlyList<ExtractedMemory> All => new List<ExtractedMemory>(_byClueId.Values);

        public bool TryGet(ClueId sourceClueId, out ExtractedMemory memory) =>
            _byClueId.TryGetValue(sourceClueId, out memory);

        public void Add(ExtractedMemory memory)
        {
            if (memory == null) throw new ArgumentNullException(nameof(memory));

            _byClueId[memory.SourceClueId] = memory;
        }

        public void Remove(ClueId sourceClueId) => _byClueId.Remove(sourceClueId);
    }
}
