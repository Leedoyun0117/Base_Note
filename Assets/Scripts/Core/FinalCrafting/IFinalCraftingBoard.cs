using System.Collections.Generic;
using GameName.Core.Emotions;
using GameName.Core.MemoryRooms;

namespace GameName.Core.FinalCrafting
{
    // 현실로 복귀한 뒤 조향실에서 방마다 확정한 "최종 향"을 읽기만 하는 경계.
    // 이 결과가 곧 의뢰 성공/실패를 가르는 값이므로(시험 조향과 달리 되돌릴
    // 수 없다), 조회와 기록(Set)의 권한을 분리해 완료 처리기·화면은 읽기만
    // 하고, 실제로 값을 채우는 것은 FinalCraftingProcessor만 하게 한다.
    public interface IFinalCraftingBoard
    {
        bool TryGet(MemoryRoomId roomId, out Scent scent);

        // 넘겨준 방 전부에 이미 최종 향이 채워져 있는지 확인한다. 의뢰인에게
        // 결과를 제공하려면(CommissionCompletionProcessor) 모든 방이 채워져
        // 있어야 한다는 규칙을 화면이 스스로 계산하지 않고 여기 물어보게
        // 한다.
        bool IsCompleteFor(IReadOnlyList<MemoryRoomId> roomIds);
    }
}
