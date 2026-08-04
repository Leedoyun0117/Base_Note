using System.Collections.Generic;

namespace GameName.Core.Clues
{
    // 분석실 보관대 경계. 조향실 보관함(IAmpouleStorage)과 자리(방/조건/용량
    // 개념)는 대칭이지만 규칙은 다르다 — 앰플 보관함은 그 자체로는 분석되지
    // 않는 완성품을 잠시 놓아두는 곳이고, 이 보관대는 "아직 조사가 끝나지
    // 않은" 단서를 인벤토리 밖에 놓아두는 곳이라 분석 가능 여부와 얽힌다
    // (ClueAnalyzer가 이 인터페이스를 함께 참조하는 이유). 그래서 공통
    // 인터페이스로 억지로 묶지 않고 별도 타입으로 둔다.
    public interface IClueStorage
    {
        int Capacity { get; }
        IReadOnlyList<ClueInfo> Clues { get; }

        ClueStorageStoreResult TryStore(ClueInfo clue);
        ClueStorageRemoveResult TryRemove(ClueInfo clue);

        // 지금 개수에 더해 additionalCount만큼 더 담아도 상한을 넘지 않는지
        // 확인한다. IAmpouleStorage.CanAccept와 같은 이유로 존재한다 — 소비자가
        // 상한 계산을 직접 반복하지 않게 하기 위함이다.
        bool CanAccept(int additionalCount);

        // 특정 단서가 지금 이 보관대에 있는지만 확인한다. ClueAnalyzer가
        // "인벤토리 또는 보관대에 있어야 분석할 수 있다"는 규칙을 검사할 때
        // 전체 목록을 순회하지 않고 바로 물어볼 수 있게 한다.
        bool Contains(ClueId clueId);
    }
}
