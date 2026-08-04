using System.Collections.Generic;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Clues
{
    // 방별 단서 배치와 습득 여부를 관리하는 경계.
    public interface IMemoryRoomClueTracker
    {
        // 아직 습득되지 않았다면 그 단서의 전체 정의(진실 포함)를 내어주고
        // true를 반환한다. 이미 습득했거나 존재하지 않는 식별자면 false다.
        bool TryGetAvailableDefinition(ClueId clueId, out ClueDefinition definition);

        // 습득 여부와 무관하게 정의를 조회한다. 이미 인벤토리로 옮겨진 단서를
        // 분석기가 다시 찾아볼 때처럼, "아직 방에 남아 있는가"가 아니라
        // "이 식별자가 무엇을 가리키는가"만 필요한 신뢰된 소비자를 위한 것이다.
        bool TryGetDefinition(ClueId clueId, out ClueDefinition definition);

        // 해당 단서를 습득 처리한다. 이후로는 같은 식별자로 다시 습득할 수 없다.
        void MarkCollected(ClueId clueId);

        // 습득 기록을 되돌린다 — 그 단서를 원래 방에 되돌려놓았을 때 쓰인다.
        // 이후로는 그 방에서 다시 습득할 수 있다. 정의 자체는 건드리지 않으므로
        // 분석 진행도나 기록지에는 영향이 없다.
        void MarkReturned(ClueId clueId);

        // 특정 방에 아직 습득되지 않고 남아 있는 단서의 공개 정보 목록.
        // ClueInfo만 반환한다 — UI가 "이 방에 무엇이 있는지" 그리는 데 쓰이므로
        // 진실 데이터가 새어 나가면 안 된다.
        IReadOnlyList<ClueInfo> GetAvailableClueInfos(MemoryRoomId roomId);
    }
}
