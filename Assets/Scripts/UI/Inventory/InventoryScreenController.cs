using System;
using GameName.Core.Clues;
using GameName.Core.Inventory;

namespace GameName.UI.Inventory
{
    // 인벤토리 오버레이의 Core 연동.
    //
    // 버리기는 반드시 ClueDropProcessor를 거친다 — 인벤토리에서 직접 빼내면
    // "그 단서가 어느 방 소속이 되는가"를 아무도 기록하지 않아 단서가 세상에서
    // 사라진다. 그 재배정은 처리기 안에서만 일어난다.
    //
    // 이 화면은 오버레이라 열려 있는 동안 인벤토리가 바깥에서 바뀔 일이 없다.
    // 그래서 이벤트를 상시 구독하지 않고 열릴 때 Refresh만 한다(가시성을
    // 관리하는 OverlayPanelHost가 그 시점에 불러 준다).
    public sealed class InventoryScreenController : IDisposable
    {
        private readonly InventoryScreenView _view;
        private readonly IPlayerInventory _inventory;
        private readonly ClueDropProcessor _dropProcessor;

        public InventoryScreenController(
            InventoryScreenView view, IPlayerInventory inventory, ClueDropProcessor dropProcessor)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _dropProcessor = dropProcessor ?? throw new ArgumentNullException(nameof(dropProcessor));

            _view.ClueDropRequested += RequestDrop;
            _view.DragReturned += OnDragReturned;
        }

        public void Refresh()
        {
            _view.SetSlots(_inventory.Items, _inventory.Capacity);
            _view.SetMessage(null);
        }

        // 버리기 버튼이 이 메서드를 부른다. 화면 없이 규칙을 확인할 수 있도록
        // 공개 메서드로도 열어 둔다 — 확대 화면의 TryStoreOpenClue와 같은 이유다.
        public void RequestDrop(ClueId clueId)
        {
            if (!TryFindClue(clueId, out var clue))
                return;

            var result = _dropProcessor.Drop(clue);

            // [ClueDebug] 진단용 — 실패는 이벤트가 발행되지 않아 밖에서 지켜볼 수
            // 없으므로 결과만 받아 적는다. 원인이 확인되면 이 두 줄과
            // Assets/Scripts/UI/Diagnostics 폴더를 함께 지운다.
            GameName.UI.Diagnostics.ClueDropDebugHook.Record(clueId, result);

            if (!result.Succeeded)
            {
                _view.SetMessage(DescribeFailure(result.FailureReason.Value));
                return;
            }

            // 버린 단서가 방에 다시 나타나는 것은 ClueDroppedEvent를 듣는 방
            // 컨트롤러의 몫이다. 여기서는 인벤토리 쪽만 다시 그린다.
            _view.SetSlots(_inventory.Items, _inventory.Capacity);

            // 성공도 결과다 — 아무 말이 없으면 끌어다 놓은 것이 통했는지
            // 그냥 사라진 것인지 구분할 수 없다.
            _view.SetMessage("서 있던 자리에 내려놓았습니다.");
        }

        // 격자 안에서 손을 놓아 아무 일도 일어나지 않은 경우. 조용히 되돌아가면
        // 조작이 먹히지 않은 것인지 취소된 것인지 알 수 없다.
        private void OnDragReturned() => _view.SetMessage("제자리로 돌아왔습니다.");

        private bool TryFindClue(ClueId clueId, out ClueInfo clue)
        {
            foreach (var item in _inventory.Items)
            {
                if (item is ClueInfo candidate && candidate.Id.Equals(clueId))
                {
                    clue = candidate;
                    return true;
                }
            }

            clue = null;
            return false;
        }

        private static string DescribeFailure(ClueDropFailureReason reason)
        {
            switch (reason)
            {
                case ClueDropFailureReason.ClueNotInInventory: return "이미 옮겨졌거나 찾을 수 없는 단서입니다.";
                case ClueDropFailureReason.NotInMemoryRoom: return "기억 방 안에서만 단서를 버릴 수 있습니다.";
                default: return "버리기에 실패했습니다.";
            }
        }

        public void Dispose()
        {
            _view.ClueDropRequested -= RequestDrop;
            _view.DragReturned -= OnDragReturned;
        }
    }
}
