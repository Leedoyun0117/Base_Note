using System;
using System.Collections.Generic;
using GameName.Core.FinalCrafting;
using GameName.Core.Judging;
using GameName.Core.MemoryRooms;
using GameName.Core.Rewards;

namespace GameName.Core.Commissions
{
    // 최종 조향 결과를 의뢰인에게 제공해 의뢰를 마무리하는 처리기.
    //
    // 방마다 시향 판정을 다시 계산한다 — 시험 조향(ScentTestingProcessor)과는
    // 완전히 별개의 판정이다. 최종 향은 인벤토리를 거치지 않고 IFinalCraftingBoard에
    // 직접 있으므로 판정 결과도 이 타입이 새로 만든다. 점수 합산은
    // CommissionScorer에, 등급별 선물 산정은 RewardTable에 위임하고, 이 타입은
    // "모든 방이 채워졌는지 확인 -> 방마다 판정 -> 점수 합산 -> 선물 지급 ->
    // 의뢰 단계 전환"이라는 순서 하나만 조율한다.
    public sealed class CommissionCompletionProcessor
    {
        private readonly IFinalCraftingBoard _board;
        private readonly IMemoryRoomAnswerRepository _answerRepository;
        private readonly IScentJudge _scentJudge;
        private readonly DisplayCollection _displayCollection;
        private readonly CommissionSession _commissionSession;

        public CommissionCompletionProcessor(
            IFinalCraftingBoard board,
            IMemoryRoomAnswerRepository answerRepository,
            IScentJudge scentJudge,
            DisplayCollection displayCollection,
            CommissionSession commissionSession)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _answerRepository = answerRepository ?? throw new ArgumentNullException(nameof(answerRepository));
            _scentJudge = scentJudge ?? throw new ArgumentNullException(nameof(scentJudge));
            _displayCollection = displayCollection ?? throw new ArgumentNullException(nameof(displayCollection));
            _commissionSession = commissionSession ?? throw new ArgumentNullException(nameof(commissionSession));
        }

        public bool CanComplete(IReadOnlyList<MemoryRoomId> roomIds) => _board.IsCompleteFor(roomIds);

        public CommissionCompletionResult Complete(IReadOnlyList<MemoryRoomId> roomIds, RewardTable rewardTable)
        {
            if (roomIds == null) throw new ArgumentNullException(nameof(roomIds));
            if (rewardTable == null) throw new ArgumentNullException(nameof(rewardTable));

            if (!CanComplete(roomIds))
                return CommissionCompletionResult.Failure(CommissionCompletionFailureReason.RoomsIncomplete);

            var roomResults = new Dictionary<MemoryRoomId, ScentJudgementResult>(roomIds.Count);
            var resultList = new List<ScentJudgementResult>(roomIds.Count);
            foreach (var roomId in roomIds)
            {
                // CanComplete가 이미 모든 방에 값이 있음을 확인했고, 정답 저장소는
                // GameSession이 같은 방 목록으로 조립하므로 여기서 조회가 실패하는
                // 것은 호출부의 데이터 정합성이 깨졌다는 뜻이라 예외로 드러낸다.
                if (!_board.TryGet(roomId, out var scent))
                    throw new InvalidOperationException($"방({roomId})의 최종 향을 찾지 못했다.");
                if (!_answerRepository.TryGetAnswer(roomId, out var answer))
                    throw new InvalidOperationException($"방({roomId})의 정답을 찾지 못했다.");

                var judgement = _scentJudge.Judge(scent, answer);
                roomResults.Add(roomId, judgement);
                resultList.Add(judgement);
            }

            if (!_commissionSession.TryComplete())
                return CommissionCompletionResult.Failure(CommissionCompletionFailureReason.WrongStage);

            var averageAccuracy = CommissionScorer.AverageAccuracy(resultList);
            var gift = rewardTable.Resolve(averageAccuracy);
            _displayCollection.Add(gift);

            return CommissionCompletionResult.Success(averageAccuracy, gift, roomResults);
        }
    }
}
