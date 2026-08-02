using System;
using System.Collections.Generic;
using GameName.Core.Journal;
using GameName.UI.Shared;
using UnityEngine.UIElements;

namespace GameName.UI.Journal
{
    // 기록지 화면의 요소 구성과 표시 갱신만 담당한다. 어떤 기록이 진짜인지,
    // 앰플이 지금 어디 있는지는 전혀 판단하지 않는다 — 컨트롤러가 IJournalReader로
    // 이미 얻은 결과를 그대로 그릴 뿐이다. 버튼은 탭 전환뿐이라 이 화면 자체는
    // 게임 상태를 절대 바꾸지 않는다(읽기 전용 화면).
    public sealed class JournalScreenView
    {
        private readonly Label _commissionLabel;
        private readonly VisualElement _contentContainer;
        private readonly Button _dialogueTabButton;
        private readonly Button _analysisTabButton;
        private readonly Button _ampouleTabButton;

        public event Action<JournalCategory> CategorySelected;

        public JournalScreenView(VisualElement root)
        {
            _commissionLabel = root.Q<Label>("commission-label");
            _contentContainer = root.Q<VisualElement>("journal-content");
            _dialogueTabButton = root.Q<Button>("tab-dialogue");
            _analysisTabButton = root.Q<Button>("tab-analysis");
            _ampouleTabButton = root.Q<Button>("tab-ampoules");

            _dialogueTabButton.clicked += () => CategorySelected?.Invoke(JournalCategory.Dialogue);
            _analysisTabButton.clicked += () => CategorySelected?.Invoke(JournalCategory.Analysis);
            _ampouleTabButton.clicked += () => CategorySelected?.Invoke(JournalCategory.Ampoules);
        }

        public void SetCommissionLabel(CommissionId? commissionId) =>
            _commissionLabel.text = commissionId.HasValue
                ? $"의뢰: {commissionId.Value.Value}"
                : "진행 중인 의뢰가 없습니다";

        public void SetSelectedCategory(JournalCategory category)
        {
            _dialogueTabButton.EnableInClassList("journal-tab--selected", category == JournalCategory.Dialogue);
            _analysisTabButton.EnableInClassList("journal-tab--selected", category == JournalCategory.Analysis);
            _ampouleTabButton.EnableInClassList("journal-tab--selected", category == JournalCategory.Ampoules);
        }

        public void ShowNoActiveCommission()
        {
            _contentContainer.Clear();
            _contentContainer.Add(CreateEmptyLabel("진행 중인 의뢰가 없습니다."));
        }

        public void SetDialogue(IReadOnlyList<DialogueLine> lines)
        {
            _contentContainer.Clear();
            if (lines.Count == 0)
            {
                _contentContainer.Add(CreateEmptyLabel("기록된 대화가 없습니다."));
                return;
            }

            foreach (var line in lines)
                _contentContainer.Add(CreateDialogueRow(line));
        }

        public void SetAnalyses(IReadOnlyList<AnalysisRecord> records)
        {
            _contentContainer.Clear();
            if (records.Count == 0)
            {
                _contentContainer.Add(CreateEmptyLabel("분석 기록이 없습니다."));
                return;
            }

            foreach (var record in records)
                _contentContainer.Add(CreateAnalysisRow(record));
        }

        public void SetAmpoules(IReadOnlyList<AmpouleRecord> records)
        {
            _contentContainer.Clear();
            if (records.Count == 0)
            {
                _contentContainer.Add(CreateEmptyLabel("제작한 앰플이 없습니다."));
                return;
            }

            foreach (var record in records)
                _contentContainer.Add(CreateAmpouleRow(record));
        }

        private static Label CreateEmptyLabel(string text)
        {
            var label = new Label(text);
            label.AddToClassList("journal-empty");
            return label;
        }

        private static VisualElement CreateDialogueRow(DialogueLine line)
        {
            var row = new VisualElement();
            row.AddToClassList("journal-entry");

            var speakerLabel = new Label(line.Speaker);
            speakerLabel.AddToClassList("journal-entry__speaker");
            row.Add(speakerLabel);

            var textLabel = new Label(line.Text);
            textLabel.AddToClassList("journal-entry__text");
            row.Add(textLabel);

            return row;
        }

        private static VisualElement CreateAnalysisRow(AnalysisRecord record)
        {
            var row = new VisualElement();
            row.AddToClassList("journal-entry");

            var titleLabel = new Label($"단서 {record.ClueId.Value}");
            titleLabel.AddToClassList("journal-entry__title");
            row.Add(titleLabel);

            var chipRow = new VisualElement();
            chipRow.AddToClassList("journal-emotion-chip-row");
            foreach (var detected in record.Result.DetectedEmotions)
                chipRow.Add(EmotionChipFactory.Create(detected));
            row.Add(chipRow);

            return row;
        }

        private static VisualElement CreateAmpouleRow(AmpouleRecord record)
        {
            var row = new VisualElement();
            row.AddToClassList("journal-entry");

            var titleLabel = new Label(
                $"{record.TargetRoomId.Value} · {ScentSummaryFormatter.Summarize(record.Scent)}");
            titleLabel.AddToClassList("journal-entry__title");
            row.Add(titleLabel);

            var stateLabel = new Label(DescribeState(record.State));
            stateLabel.AddToClassList("journal-entry__state");
            row.Add(stateLabel);

            if (record.TestResult != null)
            {
                var resultLabel = new Label($"시향 결과: {FeedbackStageDisplay.Label(record.TestResult.Result.Stage)}");
                resultLabel.AddToClassList("journal-entry__test-result");
                row.Add(resultLabel);
            }

            return row;
        }

        private static string DescribeState(AmpouleRecordState state)
        {
            switch (state)
            {
                case AmpouleRecordState.InStorage: return "조향실 보관함";
                case AmpouleRecordState.InInventory: return "인벤토리";
                case AmpouleRecordState.Consumed: return "시향으로 소모됨";
                default: return state.ToString();
            }
        }
    }
}
