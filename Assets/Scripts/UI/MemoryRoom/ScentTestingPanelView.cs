using System;
using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Judging;
using GameName.UI.Shared;
using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom
{
    // 시향 패널의 화면 요소 구성과 표시 갱신만 담당한다. 어떤 앰플을 시향할 수
    // 있는지, 결과가 무엇인지는 전혀 판단하지 않는다.
    //
    // 시향은 되돌릴 수 없으므로 버튼 한 번으로 바로 실행하지 않는다 — "시향"을
    // 누르면 그 줄에 경고와 확정/취소 버튼이 나타나고(IsArmed), "시향 확정"을
    // 눌러야 실제로 ConfirmTestRequested가 올라간다.
    public sealed class ScentTestingPanelView
    {
        private readonly VisualElement _ampouleList;
        private readonly VisualElement _resultArea;

        public event Action<Ampoule> TestRequested;
        public event Action ConfirmTestRequested;
        public event Action CancelTestRequested;

        public ScentTestingPanelView(VisualElement root)
        {
            _ampouleList = root.Q<VisualElement>("scent-test-list");
            _resultArea = root.Q<VisualElement>("scent-test-result");
        }

        public void SetAmpoules(IReadOnlyList<AmpouleTestRowData> rows)
        {
            _ampouleList.Clear();
            if (rows.Count == 0)
            {
                var empty = new Label("가지고 있는 앰플이 없습니다.");
                empty.AddToClassList("caption");
                _ampouleList.Add(empty);
                return;
            }

            foreach (var row in rows)
                _ampouleList.Add(CreateAmpouleRow(row));
        }

        // 소리는 아직 없다. FeedbackStage에 맞는 효과음 재생 지점은 이 값을
        // 만들어 넘기는 컨트롤러 쪽(OnConfirmTestRequested)에 표시해 둔다 —
        // 여기서는 텍스트로만 결과를 그린다.
        public void SetResult(FeedbackStage? stage, bool isRestored)
        {
            _resultArea.Clear();
            if (stage == null)
                return;

            var resultLabel = new Label($"시향 결과: {FeedbackStageDisplay.Label(stage.Value)}");
            resultLabel.AddToClassList("scent-test-result-text");
            _resultArea.Add(resultLabel);

            if (isRestored)
            {
                var restoredBanner = new Label("이 방이 복원되었습니다! 사다리가 열리고 정신력이 회복됩니다.");
                restoredBanner.AddToClassList("scent-test-restored-banner");
                _resultArea.Add(restoredBanner);
            }
        }

        private VisualElement CreateAmpouleRow(AmpouleTestRowData data)
        {
            var row = new VisualElement();
            row.AddToClassList("ampoule-row");

            var swatch = new VisualElement();
            swatch.AddToClassList("ampoule-row__swatch");
            swatch.AddToClassList(EmotionDisplay.ColorClass(data.Ampoule.Scent.BaseEmotion));
            row.Add(swatch);

            var summaryLabel = new Label(
                $"{data.Ampoule.TargetRoomId.Value} · {ScentSummaryFormatter.Summarize(data.Ampoule.Scent)}");
            summaryLabel.AddToClassList("ampoule-row__summary");
            row.Add(summaryLabel);

            if (!data.Eligible)
            {
                var badge = new Label("다른 방 전용");
                badge.AddToClassList("scent-test-ineligible-badge");
                row.Add(badge);
            }

            var testButton = new Button(() => TestRequested?.Invoke(data.Ampoule)) { text = "시향" };
            testButton.AddToClassList("ampoule-row__action-button");
            testButton.SetEnabled(data.Eligible);
            row.Add(testButton);

            if (data.IsArmed)
                row.Add(CreateConfirmRow());

            return row;
        }

        private VisualElement CreateConfirmRow()
        {
            var confirmRow = new VisualElement();
            confirmRow.AddToClassList("scent-test-confirm");

            var warning = new Label("시향하면 이 앰플은 사라집니다. 되돌릴 수 없습니다.");
            warning.AddToClassList("failure-message");
            confirmRow.Add(warning);

            var confirmButton = new Button(() => ConfirmTestRequested?.Invoke()) { text = "시향 확정" };
            confirmButton.AddToClassList("craft-button");
            confirmRow.Add(confirmButton);

            var cancelButton = new Button(() => CancelTestRequested?.Invoke()) { text = "취소" };
            cancelButton.AddToClassList("ampoule-row__action-button");
            confirmRow.Add(cancelButton);

            return confirmRow;
        }
    }
}
