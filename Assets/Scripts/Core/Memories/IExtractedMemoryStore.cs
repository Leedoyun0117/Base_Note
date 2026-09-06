using System.Collections.Generic;
using GameName.Core.Clues;

namespace GameName.Core.Memories
{
    // 지금 손에 든 추출된 기억을 읽기만 하는 경계.
    //
    // 색깔별 개수만 세던 IMemoryColorWallet과 달리, 여기서는 기억 하나하나가
    // 각자 출처(ClueId)와 태그를 지닌 개별 항목이다 — "해금에 쓸 기억을
    // 고른다"는 선택 자체가 이 목록을 보는 데서 시작되므로, 화면(대화 패널의
    // 제시 목록, HUD 색 요약)은 이쪽만 참조한다.
    public interface IExtractedMemoryStore
    {
        IReadOnlyList<ExtractedMemory> All { get; }

        // 어느 단서에서 나온 기억인지로 조회한다 — 검열 해금 시도가 "이 기억을
        // 제시한다"고 가리키는 단위가 곧 출처 ClueId이기 때문이다.
        bool TryGet(ClueId sourceClueId, out ExtractedMemory memory);
    }
}
