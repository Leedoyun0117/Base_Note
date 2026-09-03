using System;
using GameName.Core.Clues;

namespace GameName.UI.ClueZoom
{
    // 단서 설명 창의 Core 연동.
    //
    // 습득은 반드시 Core의 ClueCollectionProcessor를 거친다 — 인벤토리에 직접
    // 넣지 않는다. 그래야 "이 방의 단서인가", "이미 집은 것은 아닌가", "신뢰도가
    // 낮아 손이 닿지 않는 자리는 아닌가" 같은 규칙이 이 화면에서만 빠지는 일이
    // 생기지 않는다. 인벤토리에 실제로 담는 것은 그 처리기가 낸 ClueCollectedEvent를
    // 듣는 InventoryProjection의 몫이고, 이 화면은 성패만 본다.
    public sealed class ClueZoomScreenController : IDisposable
    {
        private readonly ClueZoomScreenView _view;
        private readonly ClueCollectionProcessor _collectionProcessor;

        private ClueInfo _openClue;

        // 이 화면을 닫아 달라는 요청(그만두기 버튼, 빈 공간 클릭, 수집 성공).
        public event Action CloseRequested;

        // 단서를 실제로 습득했다 — 방에서 그 단서를 지우고 인벤토리 화면을
        // 새로 그려야 한다.
        public event Action ClueStored;

        public ClueZoomScreenController(ClueZoomScreenView view, ClueCollectionProcessor collectionProcessor)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _collectionProcessor = collectionProcessor ?? throw new ArgumentNullException(nameof(collectionProcessor));

            _view.ExitRequested += OnExitRequested;
            _view.CollectRequested += TryCollectOpenClue;
        }

        public void Open(ClueInfo clue)
        {
            _openClue = clue ?? throw new ArgumentNullException(nameof(clue));
            _view.SetClue(clue);
            _view.SetMessage(null);
        }

        // 이 화면이 아닌 다른 이유로 닫혔을 때 상태를 정리한다.
        public void OnHidden()
        {
            _openClue = null;
            _view.SetMessage(null);
        }

        // [수집]을 눌렀다. 화면 없이 규칙을 확인할 수 있도록 공개 메서드로도 열어 둔다.
        public void TryCollectOpenClue()
        {
            if (_openClue == null)
                return;

            var result = _collectionProcessor.Collect(_openClue.Id);
            if (!result.Succeeded)
            {
                _view.SetMessage(DescribeFailure(result.FailureReason.Value));
                return;
            }

            _openClue = null;
            _view.SetMessage(null);

            ClueStored?.Invoke();
            CloseRequested?.Invoke();
        }

        private void OnExitRequested()
        {
            _openClue = null;
            CloseRequested?.Invoke();
        }

        private static string DescribeFailure(ClueCollectionFailureReason reason)
        {
            switch (reason)
            {
                case ClueCollectionFailureReason.NotAvailable:
                    return "이미 습득했거나 이 방의 단서가 아닙니다.";
                case ClueCollectionFailureReason.OutOfView:
                    return "신뢰도가 낮아 방이 좁게 보입니다 — 이 단서에는 손이 닿지 않습니다.";
                case ClueCollectionFailureReason.InventoryFull:
                    return "가방이 가득 찼습니다 — 다른 단서를 추출하거나 정리한 뒤에 집을 수 있습니다.";
                default:
                    return "습득에 실패했습니다.";
            }
        }

        public void Dispose()
        {
            _view.ExitRequested -= OnExitRequested;
            _view.CollectRequested -= TryCollectOpenClue;
        }
    }
}
