using System;
using System.Collections.Generic;
using GameName.Core.Analysis;
using GameName.Core.Clues;
using GameName.UI.Shared;
using UnityEngine.UIElements;

namespace GameName.UI.AnalysisRoom
{
    // 분석 패널의 화면 요소 구성과 표시 갱신만 담당한다. 어떤 단서를 분석할 수
    // 있는지, 버튼을 켤지 끌지는 전혀 판단하지 않는다 — 컨트롤러가 Core로 이미
    // 얻은 결과를 그대로 그릴 뿐이다.
    public sealed class AnalysisPanelView
    {
        private readonly VisualElement _clueList;
        private readonly Label _failureLabel;
        private readonly VisualElement _resultArea;

        public event Action<ClueId> ClueSelected;
        public event Action<AnalysisDepth> AnalysisRequested;

        public AnalysisPanelView(VisualElement root)
        {
            _clueList = root.Q<VisualElement>("clue-analysis-list");
            _failureLabel = root.Q<Label>("analysis-failure-message");
            _resultArea = root.Q<VisualElement>("analysis-result");
        }

        public void SetClues(IReadOnlyList<ClueAnalysisRowData> rows, int basicCost, int advancedCost)
        {
            _clueList.Clear();
            if (rows.Count == 0)
            {
                var empty = new Label("분석할 수 있는 단서가 없습니다.");
                empty.AddToClassList("caption");
                _clueList.Add(empty);
                return;
            }

            foreach (var row in rows)
                _clueList.Add(CreateClueRow(row, basicCost, advancedCost));
        }

        public void SetFailureMessage(string message)
        {
            _failureLabel.text = message ?? string.Empty;
            _failureLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // 이 세션에서 방금 성공한 분석의 결과만 보여준다 — 예전에(이 화면을
        // 열기 전에) 이미 최고 깊이까지 분석해 둔 단서를 다시 선택해도, Core는
        // 그 결과를 다시 내어주지 않는다(재분석 자체가 막히므로). 화면이 그
        // 값을 인벤토리의 ClueInfo로부터 스스로 재구성하면 ClueAnalyzer의
        // 판정 로직을 UI가 다시 구현하는 셈이라, 그렇게 하지 않는다 — 방금
        // 얻은 결과만 정직하게 보여준다.
        public void SetResult(EmotionAnalysisResult result)
        {
            _resultArea.Clear();
            if (result == null)
                return;

            var depthLabel = new Label(result.Depth == AnalysisDepth.Advanced ? "고급 분석 결과" : "일반 분석 결과");
            depthLabel.AddToClassList("analysis-result-title");
            _resultArea.Add(depthLabel);

            var chipRow = new VisualElement();
            chipRow.AddToClassList("analysis-emotion-chip-row");
            foreach (var detected in result.DetectedEmotions)
                chipRow.Add(EmotionChipFactory.Create(detected));
            _resultArea.Add(chipRow);
        }

        private VisualElement CreateClueRow(ClueAnalysisRowData row, int basicCost, int advancedCost)
        {
            var container = new VisualElement();
            container.AddToClassList("clue-row");
            container.EnableInClassList("clue-row--selected", row.IsSelected);

            var swatchRow = new VisualElement();
            swatchRow.AddToClassList("clue-row__swatches");
            foreach (var emotion in row.Clue.ApparentComposition.Emotions)
            {
                var swatch = new VisualElement();
                swatch.AddToClassList("clue-row__swatch");
                swatch.AddToClassList(EmotionDisplay.ColorClass(emotion));
                swatchRow.Add(swatch);
            }
            container.Add(swatchRow);

            var summaryLabel = new Label(ScentSummaryFormatter.Summarize(row.Clue.ApparentComposition));
            summaryLabel.AddToClassList("clue-row__summary");
            container.Add(summaryLabel);

            var depthLabel = new Label(DescribeDepth(row.AnalyzedDepth));
            depthLabel.AddToClassList("caption");
            container.Add(depthLabel);

            var selectButton = new Button(() => ClueSelected?.Invoke(row.Clue.Id))
            {
                text = row.IsSelected ? "접기" : "선택"
            };
            selectButton.AddToClassList("neighbor-row__move-button");
            container.Add(selectButton);

            if (row.IsSelected)
                container.Add(CreateAnalysisOptions(row, basicCost, advancedCost));

            return container;
        }

        private VisualElement CreateAnalysisOptions(ClueAnalysisRowData row, int basicCost, int advancedCost)
        {
            var options = new VisualElement();
            options.AddToClassList("analysis-options");

            var basicButton = new Button(() => AnalysisRequested?.Invoke(AnalysisDepth.Basic))
            {
                text = AnalysisCostLabel.Basic(basicCost)
            };
            basicButton.AddToClassList("craft-button");
            basicButton.SetEnabled(row.BasicEnabled);
            options.Add(basicButton);

            var advancedButton = new Button(() => AnalysisRequested?.Invoke(AnalysisDepth.Advanced))
            {
                text = AnalysisCostLabel.Advanced(advancedCost)
            };
            advancedButton.AddToClassList("craft-button");
            advancedButton.SetEnabled(row.AdvancedEnabled);
            options.Add(advancedButton);

            return options;
        }

        private static string DescribeDepth(AnalysisDepth? depth)
        {
            if (depth == null) return "미분석";
            return depth.Value == AnalysisDepth.Advanced ? "고급 분석 완료" : "일반 분석 완료";
        }
    }
}
