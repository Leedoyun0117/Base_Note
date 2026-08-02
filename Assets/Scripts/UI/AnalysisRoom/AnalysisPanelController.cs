using System;
using System.Collections.Generic;
using GameName.Core.Analysis;
using GameName.Core.Clues;
using GameName.Core.Inventory;
using GameName.Core.Mentality;
using GameName.UI.Shared;

namespace GameName.UI.AnalysisRoom
{
    // 분석 패널의 입력 처리와 Core 연동을 담당한다. ClueId와 ClueInfo만
    // 다루고 ClueDefinition(진실 구성 포함)에는 접근하지 않는다 —
    // ClueAnalyzer.Analyze(ClueId, AnalysisDepth)만 호출한다.
    //
    // 이벤트 구독이 없다 — 이 패널이 보여주는 단서 목록은 인벤토리 내용에만
    // 좌우되는데, 인벤토리를 바꾸는 행동(단서 습득, 시향 소모)은 전부 다른
    // 화면(기억 방)에 있다. 화면 전환은 이번 범위가 아니므로, 이 화면이 다시
    // 열릴 때(OnEnable)마다 새로 읽는 것으로 충분하다.
    public sealed class AnalysisPanelController : IDisposable
    {
        private readonly AnalysisPanelView _view;
        private readonly IPlayerInventory _inventory;
        private readonly IClueAnalysisProgress _analysisProgress;
        private readonly ClueAnalyzer _analyzer;
        private readonly IMentalityCostSettings _costSettings;

        private ClueId? _selectedClueId;

        public AnalysisPanelController(
            AnalysisPanelView view,
            IPlayerInventory inventory,
            IClueAnalysisProgress analysisProgress,
            ClueAnalyzer analyzer,
            IMentalityCostSettings costSettings)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _analysisProgress = analysisProgress ?? throw new ArgumentNullException(nameof(analysisProgress));
            _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
            _costSettings = costSettings ?? throw new ArgumentNullException(nameof(costSettings));

            _view.ClueSelected += OnClueSelected;
            _view.AnalysisRequested += OnAnalysisRequested;

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
        }

        private void Refresh()
        {
            var clues = ClueInventoryFilter.OnlyClues(_inventory.Items);

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
                    basicEnabled: ClueAnalysisAvailability.IsBasicAvailable(analyzedDepth),
                    advancedEnabled: ClueAnalysisAvailability.IsAdvancedAvailable(analyzedDepth)));
            }

            _view.SetClues(rows, _costSettings.BasicAnalysisCost, _costSettings.AdvancedAnalysisCost);
        }

        private static string DescribeFailure(ClueAnalysisFailureReason reason)
        {
            switch (reason)
            {
                case ClueAnalysisFailureReason.NotInAnalysisRoom: return "분석실에서만 분석할 수 있습니다.";
                case ClueAnalysisFailureReason.ClueNotInInventory: return "인벤토리에 없는 단서입니다.";
                case ClueAnalysisFailureReason.AlreadyAnalyzedAtSameOrDeeperDepth: return "이미 같은 깊이 이상으로 분석했습니다.";
                case ClueAnalysisFailureReason.InsufficientMentality: return "정신력이 부족합니다.";
                default: return "분석에 실패했습니다.";
            }
        }

        public void Dispose()
        {
            _view.ClueSelected -= OnClueSelected;
            _view.AnalysisRequested -= OnAnalysisRequested;
        }
    }
}
