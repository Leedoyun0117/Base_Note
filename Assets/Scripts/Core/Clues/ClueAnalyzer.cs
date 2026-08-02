using System;
using System.Collections.Generic;
using GameName.Core.Analysis;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Clues
{
    // IClueAnalyzer 기본 구현. 분석실에 있는 장치 하나를 표현한다.
    //
    // ClueId만 받는다. 정의(ClueDefinition, 진실 포함)는 IMemoryRoomClueTracker를
    // 통해 내부에서만 조회한다 — 호출부(UI 등)가 진실 데이터를 손에 쥐지
    // 않아도 분석을 요청할 수 있어야 하기 때문이다. 대신 "인벤토리에 실제로
    // 들고 있는 단서인가"를 IPlayerInventory로 확인한다 — 습득하지 않은
    // 단서를 방 밖에서 분석 요청하는 것을 막기 위함이다.
    //
    // 분석 진행도는 이 타입이 소유하지 않는다 — IClueAnalysisProgress에 묻고
    // 갱신을 요청할 뿐이다. 의뢰가 바뀌면 그 타입의 Reset()을 호출하는 것으로
    // 초기화되며, 이 분석기 자체는 세션 상태를 전혀 갖지 않는다.
    //
    // 재분석 정책: 이미 분석한 깊이보다 얕거나 같은 깊이로 다시 분석하려는
    // 시도는 막는다(AlreadyAnalyzedAtSameOrDeeperDepth) — 이미 아는 정보를
    // 다시 얻으려고 정신력만 낭비하는 셈이기 때문이다. 반대로 일반 분석을
    // 마친 단서를 고급 분석하는 것은 세기라는 새 정보를 얻으므로 허용한다.
    public sealed class ClueAnalyzer : IClueAnalyzer
    {
        private readonly IPlayerLocation _playerLocation;
        private readonly MemoryGraphNodeId _analysisRoomNodeId;
        private readonly IMentalityGauge _mentalityGauge;
        private readonly IMentalityCostSettings _costSettings;
        private readonly IEventBus _eventBus;
        private readonly IClueAnalysisProgress _analysisProgress;
        private readonly IMemoryRoomClueTracker _clueTracker;
        private readonly IPlayerInventory _inventory;

        public ClueAnalyzer(
            IPlayerLocation playerLocation,
            MemoryGraphNodeId analysisRoomNodeId,
            IMentalityGauge mentalityGauge,
            IMentalityCostSettings costSettings,
            IEventBus eventBus,
            IClueAnalysisProgress analysisProgress,
            IMemoryRoomClueTracker clueTracker,
            IPlayerInventory inventory)
        {
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
            _analysisRoomNodeId = analysisRoomNodeId;
            _mentalityGauge = mentalityGauge ?? throw new ArgumentNullException(nameof(mentalityGauge));
            _costSettings = costSettings ?? throw new ArgumentNullException(nameof(costSettings));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _analysisProgress = analysisProgress ?? throw new ArgumentNullException(nameof(analysisProgress));
            _clueTracker = clueTracker ?? throw new ArgumentNullException(nameof(clueTracker));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        public ClueAnalysisResult Analyze(ClueId clueId, AnalysisDepth depth)
        {
            if (!_playerLocation.Current.Equals(_analysisRoomNodeId))
                return ClueAnalysisResult.Failure(ClueAnalysisFailureReason.NotInAnalysisRoom);

            if (_analysisProgress.TryGetBestDepth(clueId, out var previousDepth) && depth <= previousDepth)
                return ClueAnalysisResult.Failure(ClueAnalysisFailureReason.AlreadyAnalyzedAtSameOrDeeperDepth);

            // 존재하지 않는 식별자는 호출부의 버그이므로 실패 결과가 아니라
            // 예외로 드러낸다 — 인벤토리에 없어서 분석할 수 없는 정상적인
            // 상황(ClueNotInInventory)과는 다른 문제다.
            if (!_clueTracker.TryGetDefinition(clueId, out var definition))
                throw new ArgumentException($"등록되지 않은 단서({clueId})를 분석할 수 없다.", nameof(clueId));

            if (!ContainsClue(definition))
                return ClueAnalysisResult.Failure(ClueAnalysisFailureReason.ClueNotInInventory);

            // CanAct 확인은 Consume 실패와 별개로 반드시 필요하다. 비용이 나중에
            // 0으로 조정되더라도(현재는 아니지만) Consume(0)은 그 자체로 항상
            // 성공하므로, "정신력이 0이면 행동이 막힌다"는 규칙이 비용 설정에
            // 의존하지 않고 항상 지켜지도록 별도로 확인한다.
            if (!_mentalityGauge.CanAct)
                return ClueAnalysisResult.Failure(ClueAnalysisFailureReason.InsufficientMentality);

            var cost = depth == AnalysisDepth.Advanced
                ? _costSettings.AdvancedAnalysisCost
                : _costSettings.BasicAnalysisCost;

            if (!_mentalityGauge.Consume(cost))
                return ClueAnalysisResult.Failure(ClueAnalysisFailureReason.InsufficientMentality);

            // 분석기는 거짓 여부를 판단하지 않는다 — 항상 겉보기 구성만 드러낸다.
            var analysisResult = BuildAnalysisResult(definition, depth);
            _analysisProgress.RecordDepth(clueId, depth);

            _eventBus.Publish(new ClueAnalyzedEvent(clueId, analysisResult));

            return ClueAnalysisResult.Success(analysisResult);
        }

        // IPlayerInventory.Items는 IReadOnlyList라 LINQ 없이 직접 순회한다 — 이
        // 계층은 UnityEngine 의존성 없는 순수 C#만 쓰지만, 그와 별개로 굳이
        // System.Linq를 끌어오지 않아도 되는 곳에서는 쓰지 않는다.
        private bool ContainsClue(ClueDefinition definition)
        {
            var info = definition.ToInfo();
            foreach (var item in _inventory.Items)
            {
                if (item.Equals(info))
                    return true;
            }

            return false;
        }

        private static EmotionAnalysisResult BuildAnalysisResult(ClueDefinition clue, AnalysisDepth depth)
        {
            var detectedEmotions = new List<DetectedEmotion>();
            foreach (var emotion in clue.ApparentComposition.Emotions)
            {
                int? intensity = depth == AnalysisDepth.Advanced
                    ? clue.ApparentComposition.IntensityOf(emotion)
                    : (int?)null;

                detectedEmotions.Add(new DetectedEmotion(emotion, intensity));
            }

            return new EmotionAnalysisResult(depth, detectedEmotions);
        }
    }
}
