using System.Collections.Generic;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Clues
{
    // 방별 단서 배치와 습득 여부를 관리하는 경계.
    //
    // 이제 이 경계가 "단서가 지금 어느 방에 있는가"의 유일한 진실 원천이다.
    // 예전에는 그 사실이 ClueDefinition.RoomId(불변)에 박혀 있었지만, 플레이어가
    // 아무 방에나 단서를 버릴 수 있게 되면서 소속 방이 플레이 중에 바뀌는
    // 상태가 되었다. 습득 여부라는 가변 상태를 이미 이 경계가 들고 있었으므로,
    // 같은 층위인 배치도 여기로 모은다.
    public interface IMemoryRoomClueTracker
    {
        // 아직 습득되지 않고 어느 방엔가 놓여 있다면, 그 단서의 전체 정의(진실
        // 포함)와 지금 놓여 있는 방을 함께 내어준다. 이미 습득했거나 존재하지
        // 않는 식별자면 false다.
        //
        // 정의와 방을 한 번에 내어주는 이유: 단서를 집을 수 있는지 판단하려면
        // 반드시 두 가지를 함께 알아야 하는데(무엇인가 + 어디 있는가), 두 번
        // 나눠 물으면 그 사이에 답이 달라질 수 있다는 오해를 부른다.
        bool TryGetPlacedClue(ClueId clueId, out ClueDefinition definition, out MemoryRoomId roomId);

        // 습득 여부와 무관하게 정의를 조회한다. 이미 인벤토리로 옮겨진 단서를
        // 분석기가 다시 찾아볼 때처럼, "아직 방에 남아 있는가"가 아니라
        // "이 식별자가 무엇을 가리키는가"만 필요한 신뢰된 소비자를 위한 것이다.
        bool TryGetDefinition(ClueId clueId, out ClueDefinition definition);

        // 해당 단서를 습득 처리한다 — 어느 방에도 놓여 있지 않은 상태가 된다.
        void MarkCollected(ClueId clueId);

        // 단서를 이 방 소속으로 놓는다. 습득한 단서를 버릴 때 쓰이며, 원래
        // 있던 방과 달라도 된다 — 그게 이 메서드의 존재 이유다. 정의 자체는
        // 건드리지 않으므로 분석 진행도나 기록지에는 영향이 없다.
        void PlaceInRoom(ClueId clueId, MemoryRoomId roomId);

        // 특정 방에 지금 놓여 있는 단서의 공개 정보 목록.
        // ClueInfo만 반환한다 — UI가 "이 방에 무엇이 있는지" 그리는 데 쓰이므로
        // 진실 데이터가 새어 나가면 안 된다.
        IReadOnlyList<ClueInfo> GetAvailableClueInfos(MemoryRoomId roomId);
    }
}
