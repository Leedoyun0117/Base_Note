using System.Collections.Generic;
using GameName.Core.Judging;
using GameName.Core.MemoryRooms;
using GameName.Core.Rewards;

namespace GameName.Core.Commissions
{
    // 의뢰 완료 시도의 결과. 완료 화면이 방별 결과를 보여줘야 하므로
    // 방 식별자별 판정 결과까지 함께 담아 돌려준다.
    public sealed class CommissionCompletionResult
    {
        public bool Succeeded { get; }
        public CommissionCompletionFailureReason? FailureReason { get; }
        public double AverageAccuracy { get; }
        public Gift Gift { get; }
        public IReadOnlyDictionary<MemoryRoomId, ScentJudgementResult> RoomResults { get; }

        private CommissionCompletionResult(
            bool succeeded,
            CommissionCompletionFailureReason? failureReason,
            double averageAccuracy,
            Gift gift,
            IReadOnlyDictionary<MemoryRoomId, ScentJudgementResult> roomResults)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            AverageAccuracy = averageAccuracy;
            Gift = gift;
            RoomResults = roomResults;
        }

        public static CommissionCompletionResult Success(
            double averageAccuracy, Gift gift, IReadOnlyDictionary<MemoryRoomId, ScentJudgementResult> roomResults) =>
            new CommissionCompletionResult(true, null, averageAccuracy, gift, roomResults);

        public static CommissionCompletionResult Failure(CommissionCompletionFailureReason reason) =>
            new CommissionCompletionResult(false, reason, 0.0, null, null);
    }
}
