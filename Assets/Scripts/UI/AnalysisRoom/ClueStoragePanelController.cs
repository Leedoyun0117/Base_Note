using System;
using System.Collections.Generic;
using GameName.Core.Analysis;
using GameName.Core.Clues;
using GameName.Core.Inventory;
using GameName.UI.Shared;

namespace GameName.UI.AnalysisRoom
{
    // 보관대 패널의 입력 처리와 Core 연동을 담당한다. ClueId만 다루고
    // ClueDefinition(진실 포함)에는 접근하지 않는다 — ClueTransferProcessor.
    // MoveToStorage/MoveToInventory(ClueInfo)만 호출하며, 그 ClueInfo는 이미
    // 인벤토리/보관대에 들어 있는 값을 그대로 다시 찾아 쓸 뿐이다.
    public sealed class ClueStoragePanelController : IDisposable
    {
        private readonly ClueStoragePanelView _view;
        private readonly IPlayerInventory _inventory;
        private readonly IClueStorage _storage;
        private readonly IClueAnalysisProgress _analysisProgress;
        private readonly ClueTransferProcessor _transferProcessor;

        // 분석 패널이 보여주는 "인벤토리 단서 목록"도 이 전송으로 바뀌므로,
        // 화면 컨트롤러가 이 이벤트를 듣고 분석 패널에 새로고침을 지시한다.
        public event Action ClueTransferred;

        public ClueStoragePanelController(
            ClueStoragePanelView view,
            IPlayerInventory inventory,
            IClueStorage storage,
            IClueAnalysisProgress analysisProgress,
            ClueTransferProcessor transferProcessor)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _analysisProgress = analysisProgress ?? throw new ArgumentNullException(nameof(analysisProgress));
            _transferProcessor = transferProcessor ?? throw new ArgumentNullException(nameof(transferProcessor));

            _view.MoveToStorageRequested += OnMoveToStorageRequested;
            _view.MoveToInventoryRequested += OnMoveToInventoryRequested;

            Refresh();
        }

        // 분석 패널에서 분석이 성공하면(그 단서가 마침 보관대에 있을 수 있으므로)
        // 화면 컨트롤러가 이 메서드를 불러 깊이 배지를 새로 그리게 한다.
        public void Refresh()
        {
            var storageRows = new List<ClueStorageRowData>(_storage.Clues.Count);
            foreach (var clue in _storage.Clues)
            {
                var hasBestDepth = _analysisProgress.TryGetBestDepth(clue.Id, out var bestDepth);
                storageRows.Add(new ClueStorageRowData(clue, hasBestDepth ? (AnalysisDepth?)bestDepth : null));
            }
            _view.SetStorage(storageRows, _storage.Capacity);

            var inventoryClues = ClueInventoryFilter.OnlyClues(_inventory.Items);
            var isInventoryFull = _inventory.Items.Count >= _inventory.Capacity;
            _view.SetInventoryClues(inventoryClues, _inventory.Capacity, isInventoryFull);
        }

        private void OnMoveToStorageRequested(ClueId clueId)
        {
            if (!TryFindInInventory(clueId, out var clue))
                return;

            var result = _transferProcessor.MoveToStorage(clue);
            HandleTransferResult(result);
        }

        private void OnMoveToInventoryRequested(ClueId clueId)
        {
            if (!TryFindInStorage(clueId, out var clue))
                return;

            var result = _transferProcessor.MoveToInventory(clue);
            HandleTransferResult(result);
        }

        private void HandleTransferResult(ClueTransferResult result)
        {
            _view.SetTransferFailureMessage(result.Succeeded ? null : DescribeFailure(result.FailureReason.Value));

            Refresh();
            if (result.Succeeded)
                ClueTransferred?.Invoke();
        }

        private bool TryFindInInventory(ClueId clueId, out ClueInfo clue)
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

        private bool TryFindInStorage(ClueId clueId, out ClueInfo clue)
        {
            foreach (var candidate in _storage.Clues)
            {
                if (candidate.Id.Equals(clueId))
                {
                    clue = candidate;
                    return true;
                }
            }

            clue = null;
            return false;
        }

        private static string DescribeFailure(ClueTransferFailureReason reason)
        {
            switch (reason)
            {
                case ClueTransferFailureReason.NotInAnalysisRoom: return "분석실에서만 옮길 수 있습니다.";
                case ClueTransferFailureReason.ClueNotFound: return "이미 옮겨졌거나 찾을 수 없는 단서입니다.";
                case ClueTransferFailureReason.DestinationFull: return "옮길 곳에 자리가 없습니다.";
                default: return "옮기기에 실패했습니다.";
            }
        }

        public void Dispose()
        {
            _view.MoveToStorageRequested -= OnMoveToStorageRequested;
            _view.MoveToInventoryRequested -= OnMoveToInventoryRequested;
        }
    }
}
