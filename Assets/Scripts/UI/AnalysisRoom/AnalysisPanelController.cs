using System;
using System.Collections.Generic;
using GameName.Core.Analysis;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Mentality;
using GameName.UI.Shared;

namespace GameName.UI.AnalysisRoom
{
    // 분석 패널의 입력 처리와 Core 연동을 담당한다. ClueId와 ClueInfo만
    // 다루고 ClueDefinition(진실 구성 포함)에는 접근하지 않는다 —
    // ClueAnalyzer.Analyze(ClueId, AnalysisDepth)만 호출한다.
    //
    // 정신력 변화만은 구독한다 — 이 화면의 지도(이동)가 기억 방 사이를 오갈
    // 때 정신력을 소모할 수 있어서, 분석 버튼의 활성/비활성 상태가 분석 자체를
    // 하지 않아도 곧바로 바뀔 수 있기 때문이다. 그 외 Core 이벤트 구독은 없다
    // — 단서 습득처럼 인벤토리를 바꾸는 행동은 전부 다른 화면(기억 방)에
    // 있어, 이 화면이 다시 열릴 때(OnEnable)마다 새로 읽는 것으로 충분하다.
    // 다만 같은 화면 안의 보관대 패널(ClueStoragePanelController)이
    // 인벤토리↔보관대 전송으로 단서 목록을 바꿀 수 있으므로, 그 알림만은
    // 화면 컨트롤러(AnalysisRoomScreenController)를 통해 공개 Refresh()로
    // 받는다.
    public sealed class AnalysisPanelController : IDisposable
    {
        private readonly AnalysisPanelView _view;
        private readonly IPlayerInventory _inventory;
        private readonly IClueAnalysisProgress _analysisProgress;
        private readonly ClueAnalyzer _analyzer;
        private readonly IMentalityGauge _mentalityGauge;
        private readonly IMentalityCostSettings _costSettings;
        private readonly IDisposable _mentalitySubscription;

        private ClueId? _selectedClueId;

        // 보관대 패널이 이 이벤트를 듣고 깊이 배지를 새로 그린다 — 방금 분석한
        // 단서가 마침 보관대에 있을 수 있기 때문이다.
        public event Action ClueAnalyzed;

        public AnalysisPanelController(
            AnalysisPanelView view,
            IPlayerInventory inventory,
            IClueAnalysisProgress analysisProgress,
            ClueAnalyzer analyzer,
            IMentalityGauge mentalityGauge,
            IMentalityCostSettings costSettings,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _analysisProgress = analysisProgress ?? throw new ArgumentNullException(nameof(analysisProgress));
            _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
            _mentalityGauge = mentalityGauge ?? throw new ArgumentNullException(nameof(mentalityGauge));
            _costSettings = costSettings ?? throw new ArgumentNullException(nameof(costSettings));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.ClueSelected += OnClueSelected;
            _view.AnalysisRequested += OnAnalysisRequested;
            _mentalitySubscription = eventBus.Subscribe<MentalityChangedEvent>(_ => Refresh());

            Refresh();
        }

        private void OnClueSelected(ClueId clueId)
        {
            // 같은 단서를 다시 선택하면 접는다 — "한 번에 하나만 분석 옵션을
            // 연다"는 요구를 만족하면서, 실수로 연 옵션을 닫을 방법도 준다.
            _selectedClueId = _selectedClueId.HasValue && _selectedClueId.Value.Equals(clueId)
                ? (ClueId?)null
                : clueId;

            _view.SetFailureMessage(null);
            _view.SetResult(null);
            Refresh();
        }

        private void OnAnalysisRequested(AnalysisDepth depth)
        {
            if (_selectedClueId == null)
                return;

            var result = _analyzer.Analyze(_selectedClueId.Value, depth);
            if (!result.Succeeded)
            {
                _view.SetFailureMessage(DescribeFailure(result.FailureReason.Value));
                _view.SetResult(null);
                return;
            }

            _view.SetFailureMessage(null);
            _view.SetResult(result.AnalysisResult);
            Refresh();
            ClueAnalyzed?.Invoke();
        }

        // 보관대 패널에서 인벤토리↔보관대 전송이 일어나면(이 화면의 단서
        // 목록 자체가 바뀌므로) 화면 컨트롤러가 이 메서드를 불러 다시 그리게
        // 한다.
        public void Refresh()
        {
            var clues = ClueInventoryFilter.OnlyClues(_inventory.Items);
            var affordability = MentalityAffordabilityCalculator.Calculate(_mentalityGauge, _costSettings);

            var rows = new List<ClueAnalysisRowData>(clues.Count);
            foreach (var clue in clues)
            {
                var hasBestDepth = _analysisProgress.TryGetBestDepth(clue.Id, out var bestDepth);
                var analyzedDepth = hasBestDepth ? (AnalysisDepth?)bestDepth : null;
                var isSelected = _selectedClueId.HasValue && _selectedClueId.Value.Equals(clue.Id);

                rows.Add(new ClueAnalysisRowData(
                    clue,
                    analyzedDepth,
                    isSelected,
                    basicEnabled: ClueAnalysisAvailability.IsBasicAvailable(analyzedDepth, affordability.CanBasicAnalyze),
                    advancedEnabled: ClueAnalysisAvailability.IsAdvancedAvailable(analyzedDepth, affordability.CanAdvancedAnalyze)));
            }

            _view.SetClues(rows, _costSettings.BasicAnalysisCost, _costSettings.AdvancedAnalysisCost);
        }

        private static string DescribeFailure(ClueAnalysisFailureReason reason)
        {
            switch (reason)
            {
                case ClueAnalysisFailureReason.NotInAnalysisRoom: return "분석실에서만 분석할 수 있습니다.";
                case ClueAnalysisFailureReason.ClueNotAccessible: return "인벤토리나 보관대에 없는 단서입니다.";
                case ClueAnalysisFailureReason.AlreadyAnalyzedAtSameOrDeeperDepth: return "이미 같은 깊이 이상으로 분석했습니다.";
                case ClueAnalysisFailureReason.InsufficientMentality: return "정신력이 부족합니다.";
                default: return "분석에 실패했습니다.";
            }
        }

        public void Dispose()
        {
            _view.ClueSelected -= OnClueSelected;
            _view.AnalysisRequested -= OnAnalysisRequested;
            _mentalitySubscription.Dispose();
        }
    }
}
