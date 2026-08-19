using System;
using GameName.Core.Clues;
using GameName.Core.Inventory;

namespace GameName.UI.ClueZoom
{
    // 단서 확대 화면의 Core 연동.
    //
    // 저장은 반드시 Core의 ClueCollector를 거친다 — 인벤토리에 직접 넣지
    // 않는다. 그래야 "지금 있는 방의 단서인가", "이미 집은 것은 아닌가",
    // "자리가 있는가" 같은 규칙이 이 화면에서만 빠지는 일이 생기지 않는다.
    // 실패하면 처리기가 구분해 둔 사유를 그대로 보여주고 단서를 제자리로
    // 돌려놓는다.
    //
    // 인벤토리 칸 개수는 IPlayerInventory.Capacity에서 읽어 View에 넘긴다 —
    // 화면이 칸 수를 알고 있지 않다는 것이 요점이다.
    public sealed class ClueZoomScreenController : IDisposable
    {
        private readonly ClueZoomScreenView _view;
        private readonly IPlayerInventory _inventory;
        private readonly ClueCollector _clueCollector;

        private ClueInfo _openClue;

        // 이 화면을 닫아 달라는 요청(나가기 버튼, 빈 공간 클릭, 저장 성공).
        // 실제로 닫는 것은 오버레이 가시성을 관리하는 쪽의 몫이다.
        public event Action CloseRequested;

        // 단서를 실제로 인벤토리에 담았다 — 방에서 그 단서를 지우고 인벤토리
        // 화면을 새로 그려야 한다.
        public event Action ClueStored;

        public ClueZoomScreenController(
            ClueZoomScreenView view, IPlayerInventory inventory, ClueCollector clueCollector)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _clueCollector = clueCollector ?? throw new ArgumentNullException(nameof(clueCollector));

            _view.ExitRequested += OnExitRequested;
            _view.DroppedOnInventory += TryStoreOpenClue;
        }

        public void Open(ClueInfo clue)
        {
            _openClue = clue ?? throw new ArgumentNullException(nameof(clue));

            _view.SetClue(clue);
            _view.SetMessage(null);
            RefreshInventorySlots();
        }

        // 이 화면이 아닌 다른 이유로 닫혔을 때(예: 기록지를 열어 라우터가
        // 감췄을 때) 남아 있던 드래그 상태를 정리한다.
        public void OnHidden()
        {
            _openClue = null;
            _view.ResetCluePosition();
            _view.SetMessage(null);
        }

        // 드래그해서 인벤토리 칸에 놓았다. View의 이벤트로도 불리고, 화면 없이
        // 규칙을 확인할 수 있도록 공개 메서드로도 열어 둔다.
        public void TryStoreOpenClue()
        {
            if (_openClue == null)
                return;

            var result = _clueCollector.Collect(_openClue.Id);
            if (!result.Succeeded)
            {
                // 원래 자리로 돌려놓고 사유를 보여준다. View는 손을 놓는 순간
                // 이미 제자리로 돌아가 있지만, 실패 경로에서 그 사실을 다시 한 번
                // 확정해 두면 나중에 View의 되돌리기 방식이 바뀌어도 이 규칙이
                // 깨지지 않는다.
                _view.ResetCluePosition();
                _view.SetMessage(DescribeFailure(result.FailureReason.Value));
                RefreshInventorySlots();
                return;
            }

            _openClue = null;
            _view.SetMessage(null);

            ClueStored?.Invoke();
            CloseRequested?.Invoke();
        }

        private void RefreshInventorySlots() => _view.SetInventorySlots(_inventory.Items, _inventory.Capacity);

        private void OnExitRequested()
        {
            _openClue = null;
            CloseRequested?.Invoke();
        }

        private static string DescribeFailure(ClueCollectionFailureReason reason)
        {
            switch (reason)
            {
                case ClueCollectionFailureReason.NotAvailable: return "이미 습득했거나 존재하지 않는 단서입니다.";
                case ClueCollectionFailureReason.WrongRoom: return "지금 있는 방의 단서가 아닙니다.";
                case ClueCollectionFailureReason.InventoryFull: return "인벤토리에 자리가 없습니다.";
                case ClueCollectionFailureReason.ItemTypeNotAccepted: return "이 물건은 담을 수 없습니다.";
                case ClueCollectionFailureReason.Duplicate: return "이미 가지고 있는 단서입니다.";
                default: return "담기에 실패했습니다.";
            }
        }

        public void Dispose()
        {
            _view.ExitRequested -= OnExitRequested;
            _view.DroppedOnInventory -= TryStoreOpenClue;
        }
    }
}
