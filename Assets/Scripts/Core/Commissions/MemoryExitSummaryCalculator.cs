using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Commissions
{
    // MemoryExitSummary 계산을 한 곳에 모은다 — 화면마다 "인벤토리+보관대
    // 단서 중 한 번도 분석하지 않은 것", "아직 복원 못한 방"을 각자 다시 세면
    // 기준이 어긋날 수 있으므로, 이탈 확인 화면은 항상 이 계산 하나만 쓴다.
    public static class MemoryExitSummaryCalculator
    {
        public static MemoryExitSummary Calculate(
            IPlayerInventory inventory,
            IClueStorage clueStorage,
            IClueAnalysisProgress analysisProgress,
            IReadOnlyList<MemoryRoomId> roomIds,
            IMemoryRoomRestorationTracker restorationTracker)
        {
            var unanalyzedClueCount = 0;

            foreach (var item in inventory.Items)
            {
                if (item is ClueInfo clue && !analysisProgress.TryGetBestDepth(clue.Id, out _))
                    unanalyzedClueCount++;
            }

            foreach (var clue in clueStorage.Clues)
            {
                if (!analysisProgress.TryGetBestDepth(clue.Id, out _))
                    unanalyzedClueCount++;
            }

            var unrestoredRoomCount = 0;
            foreach (var roomId in roomIds)
            {
                if (!restorationTracker.IsRestored(roomId))
                    unrestoredRoomCount++;
            }

            return new MemoryExitSummary(unanalyzedClueCount, unrestoredRoomCount);
        }
    }
}
