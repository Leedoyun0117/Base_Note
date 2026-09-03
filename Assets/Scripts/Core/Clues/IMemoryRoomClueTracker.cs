using System.Collections.Generic;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Clues
{
    // 방별 단서 정의와 최초 배치를 들고 있는 목록(카탈로그).
    //
    // "이 단서를 집었는가"는 더 이상 이 경계가 답하지 않는다 — 그 진실은 오직
    // ClueState(ClueStateStore)에 있다. 여기 남는 것은 바뀌지 않는 저작 사실뿐이다:
    // 어떤 단서가 어느 방에 놓여 있고, 그 정의(진실 색 포함)가 무엇인가.
    public interface IMemoryRoomClueTracker
    {
        // 습득 여부와 무관하게 정의를 조회한다. 추출 처리기가 단서의 HiddenColor를
        // 읽을 때, 그리고 인벤토리 프로젝션이 ClueInfo를 만들 때 쓴다. 없는
        // 식별자면 false다.
        bool TryGetDefinition(ClueId clueId, out ClueDefinition definition);

        // 그 방에 배치된 단서의 공개 정보 목록(등록 순서). 집힘 여부로 거르지
        // 않는다 — 그 필터는 ClueState를 보는 쪽(방 화면)이 건다. ClueInfo만
        // 반환해 진실 데이터가 새어 나가지 않게 한다.
        IReadOnlyList<ClueInfo> GetCluesInRoom(MemoryRoomId roomId);
    }
}
