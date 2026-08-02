using System;
using System.Collections.Generic;
using GameName.Core.Emotions;
using GameName.Core.Events;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using GameName.Core.Validation;

namespace GameName.Core.Ampoules
{
    // 조향실 장치. 목표 방 + 향 조합 여러 개를 한 번에 앰플로 만든다.
    //
    // 만들어진 앰플은 인벤토리가 아니라 조향실 보관함(IAmpouleStorage)에 담긴다 —
    // 기획상 보관함과 인벤토리는 서로 다른 장소이고, 목표 방까지 가져가려면
    // 플레이어가 조향실에서 따로 옮겨야 한다(AmpouleTransferProcessor의 몫).
    // 그래서 이 처리기는 인벤토리를 전혀 알지 못한다.
    //
    // 배합 판정 공식(IScentJudge)은 전혀 알지 못한다 — "만들 수 있는 배합인가"
    // (개수/양수/총량/중복 규칙)만 IScentCompositionValidator로 검사하고,
    // "정답과 얼마나 일치하는가"는 시향 처리기(ScentTestingProcessor)의 몫이다.
    // 같은 이유로 시향(장소도 조건도 다른 별개의 행위)과 한 클래스에 두지 않는다.
    //
    // 목표 방의 정답 전체는 절대 이 타입에 들어오지 않는다. 배합 검증에 필요한
    // "요구 총량"은 IMemoryRoomPublicInfoRepository로만 조회한다 — 호출부가
    // 임의의 총량을 요청에 실어 보내는 길 자체를 막기 위함이다.
    public sealed class AmpouleCraftingProcessor
    {
        private readonly IPlayerLocation _playerLocation;
        private readonly MemoryGraphNodeId _perfumeryRoomNodeId;
        private readonly IMentalityGauge _mentalityGauge;
        private readonly IMentalityCostSettings _costSettings;
        private readonly IScentCompositionValidator _compositionValidator;
        private readonly IMemoryRoomPublicInfoRepository _publicInfoRepository;
        private readonly IAmpouleStorage _storage;
        private readonly IAmpouleIdGenerator _idGenerator;
        private readonly IEventBus _eventBus;

        public AmpouleCraftingProcessor(
            IPlayerLocation playerLocation,
            MemoryGraphNodeId perfumeryRoomNodeId,
            IMentalityGauge mentalityGauge,
            IMentalityCostSettings costSettings,
            IScentCompositionValidator compositionValidator,
            IMemoryRoomPublicInfoRepository publicInfoRepository,
            IAmpouleStorage storage,
            IAmpouleIdGenerator idGenerator,
            IEventBus eventBus)
        {
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
            _perfumeryRoomNodeId = perfumeryRoomNodeId;
            _mentalityGauge = mentalityGauge ?? throw new ArgumentNullException(nameof(mentalityGauge));
            _costSettings = costSettings ?? throw new ArgumentNullException(nameof(costSettings));
            _compositionValidator =
                compositionValidator ?? throw new ArgumentNullException(nameof(compositionValidator));
            _publicInfoRepository =
                publicInfoRepository ?? throw new ArgumentNullException(nameof(publicInfoRepository));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public AmpouleCraftingResult Craft(IReadOnlyList<AmpouleCraftingRequest> requests)
        {
            if (requests == null) throw new ArgumentNullException(nameof(requests));
            if (requests.Count == 0)
                throw new ArgumentException("최소 한 개의 배합이 필요하다.", nameof(requests));

            if (!_playerLocation.Current.Equals(_perfumeryRoomNodeId))
                return AmpouleCraftingResult.Failure(AmpouleCraftingFailureReason.NotInPerfumeryRoom);

            foreach (var request in requests)
            {
                // 등록되지 않은 목표 방은 UI가 애초에 골라줄 수 없는 값이므로
                // 호출부의 버그다 — 정상적인 실패 사유가 아니라 예외로 드러낸다.
                if (!_publicInfoRepository.TryGetPublicInfo(request.TargetRoomId, out var publicInfo))
                {
                    throw new ArgumentException(
                        $"등록되지 않은 목표 방({request.TargetRoomId})이다.", nameof(requests));
                }

                var validation =
                    _compositionValidator.Validate(request.Scent, publicInfo.RequiredSupportingIntensityTotal);
                if (!validation.IsValid)
                    return AmpouleCraftingResult.Failure(AmpouleCraftingFailureReason.InvalidComposition);
            }

            // CanAct 확인은 Consume 실패와 별개로 필요하다. 비용이 나중에 0으로
            // 조정되더라도 "정신력이 0이면 막힌다"는 규칙이 항상 지켜지게 한다.
            if (!_mentalityGauge.CanAct)
                return AmpouleCraftingResult.Failure(AmpouleCraftingFailureReason.InsufficientMentality);

            // 몇 개를 만들든 1회만 소모한다 — 개수와 무관하다.
            if (!_mentalityGauge.Consume(_costSettings.AmpouleCraftingCost))
                return AmpouleCraftingResult.Failure(AmpouleCraftingFailureReason.InsufficientMentality);

            var craftedAmpoules = new List<Ampoule>(requests.Count);
            foreach (var request in requests)
            {
                var ampoule = new Ampoule(_idGenerator.Generate(), request.TargetRoomId, request.Scent);
                var storeResult = _storage.TryStore(ampoule);

                if (!storeResult.Succeeded)
                {
                    // 보관함 상한이 얼마인지, 지금 몇 개가 있는지는 IAmpouleStorage만
                    // 아는 사실이다. 그 판단을 여기서 다시 계산해 미리 막으려 하지
                    // 않고, 실제로 담아 보고 실패하면 이번 호출에서 이미 담은
                    // 것들과 소모한 정신력을 되돌려 "부분 성공은 없다"는 원칙을
                    // 지킨다.
                    RollBack(craftedAmpoules);
                    return AmpouleCraftingResult.Failure(AmpouleCraftingFailureReason.StorageFull);
                }

                craftedAmpoules.Add(ampoule);
            }

            // recipes는 requests가 아니라 실제로 만들어진 craftedAmpoules에서
            // 뽑는다 — Id는 조향 시점에 idGenerator가 부여하므로 requests에는
            // 없고, 기록지가 이후 시향 결과와 연결하려면 이 Id가 반드시
            // 필요하다.
            var recipes = new List<AmpouleRecipe>(craftedAmpoules.Count);
            foreach (var ampoule in craftedAmpoules)
                recipes.Add(new AmpouleRecipe(ampoule.Id, ampoule.TargetRoomId, ampoule.Scent));

            _eventBus.Publish(new AmpouleCraftedEvent(recipes));

            return AmpouleCraftingResult.Success(craftedAmpoules);
        }

        private void RollBack(List<Ampoule> craftedSoFar)
        {
            foreach (var ampoule in craftedSoFar)
                _storage.TryRemove(ampoule);

            _mentalityGauge.Restore(_costSettings.AmpouleCraftingCost);
        }
    }
}
