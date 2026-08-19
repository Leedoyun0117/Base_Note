using System;
using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Clues;
using GameName.Core.Inventory;
using GameName.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.ClueZoom
{
    // 단서 확대 화면의 요소 구성과 드래그 조작만 담당한다. 담을 수 있는지,
    // 왜 실패했는지는 하나도 판단하지 않는다 — 그건 컨트롤러가 Core에게 물어
    // 얻은 결론이고, 이 View는 "칸 위에서 손을 놓았다"는 사실만 알린다.
    //
    // 인벤토리 칸을 몇 개 그릴지도 스스로 정하지 않는다. SetInventorySlots가
    // 받은 용량만큼 그린다 — 그 값은 Core의 IPlayerInventory.Capacity에서
    // 그대로 온다. 화면 구성이 2x2를 전제하더라도 칸 개수를 코드에 적지 않는
    // 이유는, 업그레이드로 칸이 늘어나는 것이 이미 게임 규칙에 있기 때문이다
    // (UpgradeCategory.InventoryCapacity).
    public sealed class ClueZoomScreenView
    {
        private readonly VisualElement _stage;
        private readonly VisualElement _clueElement;
        private readonly VisualElement _swatchRow;
        private readonly Label _kindLabel;
        private readonly Label _summaryLabel;
        private readonly VisualElement _inventoryGrid;
        private readonly Label _messageLabel;
        private readonly Button _exitButton;

        private readonly List<VisualElement> _slotElements = new List<VisualElement>();

        // 이 화면을 연 누름이 그대로 닫기로 이어지지 않게 막는 빗장.
        private bool _acceptsStageClose;

        private bool _isDragging;
        private Vector2 _dragStartPointerPosition;
        private Vector2 _currentTranslate;

        // 나가기 버튼을 눌렀거나 빈 공간을 눌렀다.
        public event Action ExitRequested;

        // 드래그하던 단서를 인벤토리 칸 위에서 놓았다.
        public event Action DroppedOnInventory;

        public ClueZoomScreenView(VisualElement root)
        {
            _stage = root.Q<VisualElement>("clue-zoom-stage");
            _clueElement = root.Q<VisualElement>("clue-zoom-clue");
            _swatchRow = root.Q<VisualElement>("clue-zoom-clue-swatches");
            _kindLabel = root.Q<Label>("clue-zoom-clue-kind");
            _summaryLabel = root.Q<Label>("clue-zoom-clue-summary");
            _inventoryGrid = root.Q<VisualElement>("clue-zoom-inventory-grid");
            _messageLabel = root.Q<Label>("clue-zoom-message");
            _exitButton = root.Q<Button>("clue-zoom-exit-button");

            _exitButton.clicked += () => ExitRequested?.Invoke();

            _clueElement.RegisterCallback<PointerDownEvent>(OnCluePointerDown);
            _clueElement.RegisterCallback<PointerMoveEvent>(OnCluePointerMove);
            _clueElement.RegisterCallback<PointerUpEvent>(OnCluePointerUp);

            // 빈 공간을 눌렀을 때만 닫는다. 단서나 나가기 버튼을 누른 경우에는
            // 이벤트가 그 요소에서 올라오는 것이라 target이 무대 자신이 아니다.
            //
            // 다만 이 화면을 열게 한 그 누름으로는 닫지 않는다. 방에서 단서를
            // 누르는 순간 이 화면이 떠오르는데, 같은 프레임에 UI 쪽으로도 그
            // 누름이 한 번 더 전달된다 — 그때 커서 아래에 방금 나타난 이 무대가
            // 있으면 열리자마자 그대로 닫힌다. 화면이 깜빡이지도 않아서, 밖에서
            // 보면 "눌러도 아무 일이 없다"로만 보인다.
            //
            // 전달 여부가 그 프레임에 레이아웃이 갱신되었는지에 달려 있어서
            // 어떨 때는 닫히고 어떨 때는 열린 채로 남는다. 같은 단서를 같은
            // 자리에서 눌러도 결과가 갈리던 이유가 이것이다.
            //
            // 그래서 손을 한 번 뗀 뒤부터 닫기를 받는다. 누름과 뗌은 짝이므로,
            // 이 화면을 연 누름의 뗌이 곧 "이제부터는 사용자의 새 조작"이라는
            // 경계가 된다.
            _stage.RegisterCallback<PointerUpEvent>(_ => _acceptsStageClose = true);

            _stage.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (!_acceptsStageClose)
                    return;

                if (ReferenceEquals(evt.target, _stage))
                    ExitRequested?.Invoke();
            });
        }

        // 드래그가 끝나 원래 자리로 돌아와 있는지. 실패했을 때 제자리로
        // 돌아가는지를 화면 없이 확인하기 위해 노출한다.
        public bool IsClueAtOrigin =>
            !_clueElement.ClassListContains("clue-zoom-clue--dragging") &&
            Mathf.Approximately(_currentTranslate.x, 0f) &&
            Mathf.Approximately(_currentTranslate.y, 0f);

        public void SetClue(ClueInfo clue)
        {
            // 열릴 때마다 빗장을 다시 건다 — 이번 화면을 연 누름은 아직 끝나지
            // 않았다.
            _acceptsStageClose = false;

            _swatchRow.Clear();
            foreach (var emotion in clue.ApparentComposition.Emotions)
            {
                var swatch = new VisualElement();
                swatch.AddToClassList("clue-zoom-clue__swatch");
                swatch.AddToClassList(EmotionDisplay.ColorClass(emotion));
                _swatchRow.Add(swatch);
            }

            _kindLabel.text = clue.Kind == ClueKind.Poster ? "벽에 붙은 포스터" : "바닥에 떨어진 물건";
            _summaryLabel.text = ScentSummaryFormatter.Summarize(clue.ApparentComposition);

            ResetCluePosition();
        }

        public void SetInventorySlots(IReadOnlyList<IInventoryItem> items, int capacity)
        {
            _inventoryGrid.Clear();
            _slotElements.Clear();

            for (var i = 0; i < capacity; i++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("clue-zoom-slot");

                var inner = new VisualElement();
                inner.AddToClassList("clue-zoom-slot__inner");
                slot.Add(inner);

                if (i < items.Count)
                {
                    slot.AddToClassList("clue-zoom-slot--filled");
                    FillSlot(inner, items[i]);
                }

                _inventoryGrid.Add(slot);
                _slotElements.Add(slot);
            }
        }

        public void SetMessage(string message)
        {
            _messageLabel.text = message ?? string.Empty;
            _messageLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // 드래그를 취소하거나 저장에 실패했을 때 단서를 제자리로 되돌린다.
        public void ResetCluePosition()
        {
            _isDragging = false;
            _currentTranslate = Vector2.zero;
            _clueElement.RemoveFromClassList("clue-zoom-clue--dragging");
            _clueElement.style.translate = new Translate(0f, 0f);
            SetSlotHighlight(null);
        }

        private static void FillSlot(VisualElement inner, IInventoryItem item)
        {
            if (item is ClueInfo clue)
            {
                foreach (var emotion in clue.ApparentComposition.Emotions)
                {
                    var swatch = new VisualElement();
                    swatch.AddToClassList("clue-zoom-slot__swatch");
                    swatch.AddToClassList(EmotionDisplay.ColorClass(emotion));
                    inner.Add(swatch);
                }

                var label = new Label("단서");
                label.AddToClassList("clue-zoom-slot__label");
                inner.Add(label);
                return;
            }

            if (item is Ampoule ampoule)
            {
                var swatch = new VisualElement();
                swatch.AddToClassList("clue-zoom-slot__swatch");
                swatch.AddToClassList(EmotionDisplay.ColorClass(ampoule.Scent.BaseEmotion));
                inner.Add(swatch);

                var label = new Label("앰플");
                label.AddToClassList("clue-zoom-slot__label");
                inner.Add(label);
            }
        }

        private void OnCluePointerDown(PointerDownEvent evt)
        {
            _isDragging = true;
            _dragStartPointerPosition = evt.position;

            _clueElement.AddToClassList("clue-zoom-clue--dragging");
            _clueElement.CapturePointer(evt.pointerId);

            SetMessage(null);
            evt.StopPropagation();
        }

        private void OnCluePointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging)
                return;

            _currentTranslate = (Vector2)evt.position - _dragStartPointerPosition;
            _clueElement.style.translate = new Translate(_currentTranslate.x, _currentTranslate.y);

            SetSlotHighlight(FindSlotUnder(evt.position));
        }

        private void OnCluePointerUp(PointerUpEvent evt)
        {
            if (!_isDragging)
                return;

            _clueElement.ReleasePointer(evt.pointerId);

            var droppedSlot = FindSlotUnder(evt.position);

            // 어느 경우든 일단 제자리로 되돌린다. 저장에 성공하면 이 화면 자체가
            // 닫히므로 되돌린 모습은 보이지 않고, 실패하거나 허공에 놓았으면
            // 되돌아간 모습이 그대로 남는다.
            ResetCluePosition();

            if (droppedSlot != null)
                DroppedOnInventory?.Invoke();
        }

        private VisualElement FindSlotUnder(Vector2 pointerPosition)
        {
            foreach (var slot in _slotElements)
            {
                if (slot.worldBound.Contains(pointerPosition))
                    return slot;
            }

            return null;
        }

        private void SetSlotHighlight(VisualElement hoveredSlot)
        {
            foreach (var slot in _slotElements)
                slot.EnableInClassList("clue-zoom-slot--hovered", ReferenceEquals(slot, hoveredSlot));
        }
    }
}
