using System;
using GameName.Core.Emotions;
using GameName.Core.MemoryRooms;
using GameName.Core.Validation;

namespace GameName.Core.FinalCrafting
{
    // 현실로 복귀한 뒤, 진짜 조향실에서 방마다 "최종 향" 하나를 확정하는
    // 처리기. 이 결과가 시험 조향과 달리 의뢰 성공/실패를 직접 가른다.
    //
    // 일부러 IPlayerLocation/목표 방 노드 위치를 확인하지 않는다 — 시험
    // 조향(AmpouleCraftingProcessor)과 달리 "지금 조향실 노드에 서 있는가"가
    // 더 이상 의미 있는 질문이 아니다. 현실로 복귀하는 순간(TryReturnToReality)
    // 플레이어 위치는 기억 진입 지점(계단)에 고정되고, 그 뒤로는 기억 방
    // 그래프 자체를 다시 오가지 않기 때문이다. 대신 "지금이 최종 조향을 할 수
    // 있는 시점인가"는 이 화면이 CommissionStage.ReturnedToReality에서만
    // 보인다는 사실(ActiveScreenSelector) 하나로 이미 강제된다 — 그 강제
    // 지점을 여기 또 만들면 같은 규칙을 두 곳에서 어긋나게 관리하게 된다.
    //
    // 일부러 IMentalityGauge/IMentalityCostSettings를 전혀 참조하지 않는다 —
    // 정신력을 요구하지 않는다는 규칙 자체를 "비용을 0으로 설정"이 아니라
    // "비용 개념이 존재하지 않는다"로 구현한 것이다. 최종 조향은 전체 의뢰의
    // 마지막 필수 관문이라(모든 방을 채워야만 완료할 수 있다), 우연히 정신력이
    // 떨어져 있다는 이유로 이미 끝까지 플레이한 의뢰를 완료조차 못 하게
    // 막는 것은 부당하다고 판단했다 — 정신력 자원은 기억 탐색 중의 선택(몇 번
    // 시향할지, 어디까지 이동할지)을 조율하기 위한 것이지, 마지막 제출 자체를
    // 막기 위한 것이 아니다.
    //
    // 조향(시험용) 처리기와 달리 앰플/보관함/인벤토리를 전혀 거치지 않는다 —
    // 방 하나당 값 하나만 있으면 되고(여러 개를 만들어 옮길 필요가 없다),
    // 재작성이 자유로워야 하므로(드럼 단계였던 방도 다시 확인하며 여러 번
    // 고칠 수 있어야 한다) 보관 상한이라는 개념 자체가 어울리지 않는다.
    public sealed class FinalCraftingProcessor
    {
        private readonly IScentCompositionValidator _compositionValidator;
        private readonly IMemoryRoomPublicInfoRepository _publicInfoRepository;
        private readonly IFinalCraftingBoardWriter _board;

        public FinalCraftingProcessor(
            IScentCompositionValidator compositionValidator,
            IMemoryRoomPublicInfoRepository publicInfoRepository,
            IFinalCraftingBoardWriter board)
        {
            _compositionValidator =
                compositionValidator ?? throw new ArgumentNullException(nameof(compositionValidator));
            _publicInfoRepository =
                publicInfoRepository ?? throw new ArgumentNullException(nameof(publicInfoRepository));
            _board = board ?? throw new ArgumentNullException(nameof(board));
        }

        public FinalCraftingResult Craft(MemoryRoomId targetRoomId, Scent scent)
        {
            if (scent == null) throw new ArgumentNullException(nameof(scent));

            // 등록되지 않은 목표 방은 UI가 애초에 골라줄 수 없는 값이므로
            // 호출부의 버그다 — 정상적인 실패 사유가 아니라 예외로 드러낸다.
            if (!_publicInfoRepository.TryGetPublicInfo(targetRoomId, out var publicInfo))
                throw new ArgumentException($"등록되지 않은 목표 방({targetRoomId})이다.", nameof(targetRoomId));

            var validation = _compositionValidator.Validate(scent, publicInfo.RequiredSupportingIntensityTotal);
            if (!validation.IsValid)
                return FinalCraftingResult.Failure(FinalCraftingFailureReason.InvalidComposition);

            _board.Set(targetRoomId, scent);
            return FinalCraftingResult.Success();
        }
    }
}
