using GameName.Core.Emotions;
using GameName.Core.MemoryRooms;

namespace GameName.Core.FinalCrafting
{
    // 방 하나의 최종 향을 기록(덮어쓰기 포함)하는 권한 하나만 표현하는 좁은
    // 경계. FinalCraftingProcessor만 이 인터페이스를 받는다 — 완료 처리기나
    // 화면은 결과를 읽을 수만 있을 뿐 스스로 값을 써넣을 수 없다.
    public interface IFinalCraftingBoardWriter
    {
        // 같은 방에 다시 호출하면 이전 값을 덮어쓴다. 시험 삼아 여러 번
        // 다시 만들어 볼 수 있어야 하고(그렇지 않으면 실수 한 번으로 의뢰
        // 전체를 그르친다), 마지막으로 확정한 값만 의미를 가진다.
        void Set(MemoryRoomId roomId, Scent scent);
    }
}
