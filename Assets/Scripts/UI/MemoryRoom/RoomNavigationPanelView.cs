using System;
using System.Collections.Generic;
using GameName.Core.MemoryRooms;
using GameName.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom
{
    // 이동 패널의 화면 요소 구성과 표시 갱신만 담당한다. 어디로 갈 수 있는지,
    // 사다리가 잠겼는지, 비용이 얼마인지는 전혀 판단하지 않는다 — 컨트롤러가
    // Core로 이미 얻은 결과를 그대로 그릴 뿐이다.
    public sealed class RoomNavigationPanelView
    {
        private readonly Label _currentRoomLabel;
        private readonly Label _restoredBadge;
        private readonly VisualElement _mentalityBarFill;
        private readonly Label _mentalityValueLabel;
        private readonly Label _mentalityNoticeLabel;
        private readonly VisualElement _neighborList;
        private readonly Label _moveFailureLabel;

        public event Action<MemoryGraphNodeId> MoveRequested;

        public RoomNavigationPanelView(VisualElement root)
        {
            _currentRoomLabel = root.Q<Label>("current-room-label");
            _restoredBadge = root.Q<Label>("current-room-restored-badge");
            _mentalityBarFill = root.Q<VisualElement>("mentality-bar-fill");
            _mentalityValueLabel = root.Q<Label>("mentality-value");
            _mentalityNoticeLabel = root.Q<Label>("mentality-notice");
            _neighborList = root.Q<VisualElement>("neighbor-list");
            _moveFailureLabel = root.Q<Label>("move-failure-message");
        }

        public void SetCurrentRoom(string label, bool isRestored)
        {
            _currentRoomLabel.text = label;
            _restoredBadge.style.display = isRestored ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetMentality(int current, int max)
        {
            _mentalityValueLabel.text = $"{current} / {max}";

            var ratio = max <= 0 ? 0f : (float)current / max;
            _mentalityBarFill.style.width = Length.Percent(Mathf.Clamp01(ratio) * 100f);
        }

        public void SetMentalityNotice(string message)
        {
            _mentalityNoticeLabel.text = message ?? string.Empty;
            _mentalityNoticeLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // 인접 노드 목록. 이 화면에서 이동/허브 진입/계단 이탈이 전부 여기서
        // 이루어진다.
        public void SetNeighbors(IReadOnlyList<NeighborRowData> neighbors)
        {
            _neighborList.Clear();
            if (neighbors.Count == 0)
            {
                var empty = new Label("갈 수 있는 곳이 없습니다.");
                empty.AddToClassList("caption");
                _neighborList.Add(empty);
                return;
            }

            foreach (var neighbor in neighbors)
                _neighborList.Add(CreateNeighborRow(neighbor));
        }

        public void SetMoveFailureMessage(string message)
        {
            _moveFailureLabel.text = message ?? string.Empty;
            _moveFailureLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private VisualElement CreateNeighborRow(NeighborRowData neighbor)
        {
            var row = new VisualElement();
            row.AddToClassList("neighbor-row");

            var nameLabel = new Label($"{neighbor.NodeId.Value} ({MemoryGraphNodeTypeDisplay.Label(neighbor.NodeType)})");
            nameLabel.AddToClassList("neighbor-row__name");
            row.Add(nameLabel);

            if (neighbor.NodeType == MemoryGraphNodeType.Staircase)
            {
                var exitCaption = new Label("계단으로 가면 이 기억에서 나가게 됩니다.");
                exitCaption.AddToClassList("caption");
                row.Add(exitCaption);
            }

            if (neighbor.IsLocked)
            {
                var lockedBadge = new Label("사다리 잠김");
                lockedBadge.AddToClassList("neighbor-row__locked-badge");
                row.Add(lockedBadge);
            }

            var costLabel = new Label($"비용: 정신력 {neighbor.Cost}");
            costLabel.AddToClassList("caption");
            row.Add(costLabel);

            var moveButton = new Button(() => MoveRequested?.Invoke(neighbor.NodeId)) { text = "이동" };
            moveButton.AddToClassList("neighbor-row__move-button");
            moveButton.SetEnabled(!neighbor.IsLocked);
            row.Add(moveButton);

            return row;
        }
    }
}
