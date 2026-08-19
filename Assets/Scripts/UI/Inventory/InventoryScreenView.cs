using System;
using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Clues;
using GameName.Core.Inventory;
using GameName.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.Inventory
{
    // 인벤토리 오버레이의 요소 구성과 표시 갱신, 그리고 "칸 밖으로 끌어내
    // 버리기" 조작을 담당한다.
    //
    // 칸 개수는 넘겨받은 용량이 정한다 — Core의 IPlayerInventory.Capacity가
    // 그대로 온다. 빈 칸도 그려서 용량이 눈에 보이게 한다.
    //
    // 버리는 방법을 둘 다 둔다: 칸의 "버리기" 버튼과, 칸을 격자 밖으로 끌어다
    // 놓는 드래그. 드래그가 더 자연스럽지만 화면만 봐서는 알 수 없는 조작이라,
    // 버튼과 안내 문구가 그 존재를 알려주는 역할을 한다.
    //
    // 지금 서 있는 곳이 기억 방인지 아닌지는 이 View가 판단하지 않는다. 눌러
    // 보고 실패 사유(NotInMemoryRoom)를 그대로 보여주는 쪽이, 화면이 규칙을
    // 흉내 내다 어긋나는 것보다 안전하다.
    public sealed class InventoryScreenView
    {
        private readonly Label _countLabel;
        private readonly VisualElement _slotGrid;
        private readonly Label _messageLabel;

        private VisualElement _draggingSlot;
        private Vector2 _dragStartPointerPosition;

        public event Action<ClueId> ClueDropRequested;

        // 격자 안에서 손을 놓아 아무 일도 일어나지 않았다. 결과를 알려주는 것은
        // 컨트롤러의 몫이라 사실만 올린다.
        public event Action DragReturned;

        public InventoryScreenView(VisualElement root)
        {
            _countLabel = root.Q<Label>("inventory-count");
            _slotGrid = root.Q<VisualElement>("inventory-slot-grid");
            _messageLabel = root.Q<Label>("inventory-message");
        }

        // 지금 그려진 칸 개수. 칸 수가 Core 설정값을 따르는지 화면 없이 확인할
        // 수 있게 노출한다.
        public int SlotCount => _slotGrid.childCount;

        public void SetSlots(IReadOnlyList<IInventoryItem> items, int capacity)
        {
            CancelDrag();

            _countLabel.text = $"{items.Count} / {capacity}";

            _slotGrid.Clear();
            for (var i = 0; i < capacity; i++)
                _slotGrid.Add(i < items.Count ? CreateFilledSlot(items[i]) : CreateEmptySlot());
        }

        public void SetMessage(string message)
        {
            _messageLabel.text = message ?? string.Empty;
            _messageLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private VisualElement CreateFilledSlot(IInventoryItem item)
        {
            var slot = CreateEmptySlot();
            slot.AddToClassList("inventory-slot--filled");

            var inner = slot[0];

            if (item is ClueInfo clue)
            {
                var swatchRow = new VisualElement();
                swatchRow.AddToClassList("inventory-slot__swatch-row");
                foreach (var emotion in clue.ApparentComposition.Emotions)
                {
                    var swatch = new VisualElement();
                    swatch.AddToClassList("inventory-slot__swatch");
                    swatch.AddToClassList(EmotionDisplay.ColorClass(emotion));
                    swatchRow.Add(swatch);
                }
                inner.Add(swatchRow);

                var categoryLabel = new Label(clue.Kind == ClueKind.Poster ? "단서 · 포스터" : "단서 · 물건");
                categoryLabel.AddToClassList("inventory-slot__category");
                inner.Add(categoryLabel);

                var dropButton = new Button(() => ClueDropRequested?.Invoke(clue.Id)) { text = "버리기" };
                dropButton.AddToClassList("inventory-slot__drop-button");
                inner.Add(dropButton);

                RegisterDrag(slot, clue.Id);
                return slot;
            }

            if (item is Ampoule ampoule)
            {
                var swatch = new VisualElement();
                swatch.AddToClassList("inventory-slot__swatch");
                swatch.AddToClassList(EmotionDisplay.ColorClass(ampoule.Scent.BaseEmotion));
                inner.Add(swatch);

                var categoryLabel = new Label("앰플");
                categoryLabel.AddToClassList("inventory-slot__category");
                inner.Add(categoryLabel);

                var summaryLabel = new Label(
                    $"{ampoule.TargetRoomId.Value} · {ScentSummaryFormatter.Summarize(ampoule.Scent)}");
                summaryLabel.AddToClassList("inventory-caption");
                inner.Add(summaryLabel);
            }

            return slot;
        }

        // 앰플에는 드래그를 붙이지 않는다 — 방에 놓는다는 개념이 단서에만 있다.
        private void RegisterDrag(VisualElement slot, ClueId clueId)
        {
            slot.RegisterCallback<PointerDownEvent>(evt =>
            {
                // 버튼 위에서 시작한 눌림은 드래그가 아니라 버튼 클릭이다.
                if (evt.target is Button)
                    return;

                _draggingSlot = slot;
                _dragStartPointerPosition = evt.position;

                slot.AddToClassList("inventory-slot--dragging");
                slot.CapturePointer(evt.pointerId);

                SetMessage(null);
            });

            slot.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!ReferenceEquals(_draggingSlot, slot))
                    return;

                var delta = (Vector2)evt.position - _dragStartPointerPosition;
                slot.style.translate = new Translate(delta.x, delta.y);
            });

            slot.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!ReferenceEquals(_draggingSlot, slot))
                    return;

                slot.ReleasePointer(evt.pointerId);

                // 격자 밖에서 손을 놓았으면 "방에 내려놓겠다"는 뜻이다.
                var droppedOutside = !_slotGrid.worldBound.Contains(evt.position);
                CancelDrag();

                if (droppedOutside)
                    ClueDropRequested?.Invoke(clueId);
                else
                    DragReturned?.Invoke();
            });
        }

        private void CancelDrag()
        {
            if (_draggingSlot == null)
                return;

            _draggingSlot.RemoveFromClassList("inventory-slot--dragging");
            _draggingSlot.style.translate = new Translate(0f, 0f);
            _draggingSlot = null;
        }

        private static VisualElement CreateEmptySlot()
        {
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot");

            var inner = new VisualElement();
            inner.AddToClassList("inventory-slot__inner");
            slot.Add(inner);

            return slot;
        }
    }
}
