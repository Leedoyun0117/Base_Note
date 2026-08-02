using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.UI.Shared;
using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom
{
    // 단서 목록 패널의 화면 요소 구성과 표시 갱신만 담당한다.
    //
    // 단서는 아직 아트가 없어 목록 형태로 보여준다. 나중에 방 배경 위에 놓인
    // 오브젝트(클릭해서 줍는 방식)로 교체될 자리다 — 그때는 이 스크롤 목록
    // 전체가 방 배경 씬 오브젝트 렌더링으로 바뀌고, CollectRequested 이벤트만
    // 그대로 재사용하면 된다.
    public sealed class ClueCollectionPanelView
    {
        private readonly VisualElement _clueList;
        private readonly Label _collectFailureLabel;

        public event Action<ClueId> CollectRequested;

        public ClueCollectionPanelView(VisualElement root)
        {
            _clueList = root.Q<VisualElement>("clue-list");
            _collectFailureLabel = root.Q<Label>("clue-collect-failure-message");
        }

        public void SetClues(IReadOnlyList<ClueInfo> clues)
        {
            _clueList.Clear();
            if (clues.Count == 0)
            {
                var empty = new Label("이 방에 남은 단서가 없습니다.");
                empty.AddToClassList("caption");
                _clueList.Add(empty);
                return;
            }

            foreach (var clue in clues)
                _clueList.Add(CreateClueRow(clue));
        }

        public void SetCollectFailureMessage(string message)
        {
            _collectFailureLabel.text = message ?? string.Empty;
            _collectFailureLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private VisualElement CreateClueRow(ClueInfo clue)
        {
            var row = new VisualElement();
            row.AddToClassList("clue-row");

            var swatchRow = new VisualElement();
            swatchRow.AddToClassList("clue-row__swatches");
            foreach (var emotion in clue.ApparentComposition.Emotions)
            {
                var swatch = new VisualElement();
                swatch.AddToClassList("clue-row__swatch");
                swatch.AddToClassList(EmotionDisplay.ColorClass(emotion));
                swatchRow.Add(swatch);
            }
            row.Add(swatchRow);

            var summaryLabel = new Label(ScentSummaryFormatter.Summarize(clue.ApparentComposition));
            summaryLabel.AddToClassList("clue-row__summary");
            row.Add(summaryLabel);

            var collectButton = new Button(() => CollectRequested?.Invoke(clue.Id)) { text = "줍기" };
            collectButton.AddToClassList("neighbor-row__move-button");
            row.Add(collectButton);

            return row;
        }
    }
}
