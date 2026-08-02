using System;
using System.Collections.Generic;
using GameName.Core.MemoryRooms;
using UnityEngine.UIElements;

namespace GameName.UI.Perfumery
{
    // 좌측(방 선택) 패널의 화면 요소 구성과 표시 갱신만 담당한다.
    // 어떤 방을 고를 수 있는지, 선택이 유효한지는 이 클래스가 판단하지 않는다 —
    // 컨트롤러가 넘겨준 데이터를 그대로 그릴 뿐이다. 사용자 입력(클릭)은
    // RoomSelected 이벤트로 밖에 알리기만 하고 직접 처리하지 않는다.
    public sealed class RoomSelectionPanelView
    {
        private const string SelectedRowClass = "room-row--selected";
        private const string RestoredRowClass = "room-row--restored";

        private readonly VisualElement _roomListContainer;
        private readonly Label _requiredTotalValueLabel;
        private readonly Dictionary<MemoryRoomId, VisualElement> _rowsByRoomId =
            new Dictionary<MemoryRoomId, VisualElement>();

        public event Action<MemoryRoomId> RoomSelected;

        public RoomSelectionPanelView(VisualElement root)
        {
            _roomListContainer = root.Q<VisualElement>("room-list");
            _requiredTotalValueLabel = root.Q<Label>("required-total-value");
        }

        public void SetRooms(IReadOnlyList<RoomListItemData> rooms, MemoryRoomId? selectedRoomId)
        {
            _roomListContainer.Clear();
            _rowsByRoomId.Clear();

            foreach (var data in rooms)
            {
                var row = CreateRoomRow(data);
                _rowsByRoomId.Add(data.PublicInfo.RoomId, row);
                _roomListContainer.Add(row);
            }

            SetSelectedRoom(selectedRoomId);
        }

        public void SetSelectedRoom(MemoryRoomId? selectedRoomId)
        {
            foreach (var pair in _rowsByRoomId)
            {
                var isSelected = selectedRoomId.HasValue && pair.Key.Equals(selectedRoomId.Value);
                pair.Value.EnableInClassList(SelectedRowClass, isSelected);
            }
        }

        public void SetRequiredTotal(int? total)
        {
            _requiredTotalValueLabel.text = total.HasValue ? total.Value.ToString() : "-";
        }

        private VisualElement CreateRoomRow(RoomListItemData data)
        {
            var roomId = data.PublicInfo.RoomId;

            var row = new VisualElement();
            row.AddToClassList("room-row");
            row.EnableInClassList(RestoredRowClass, data.IsRestored);

            var nameLabel = new Label(roomId.Value);
            nameLabel.AddToClassList("room-row__name");
            row.Add(nameLabel);

            if (data.IsRestored)
            {
                var badge = new Label("복원됨");
                badge.AddToClassList("room-row__badge");
                row.Add(badge);
            }

            row.RegisterCallback<ClickEvent>(_ => RoomSelected?.Invoke(roomId));

            return row;
        }
    }
}
