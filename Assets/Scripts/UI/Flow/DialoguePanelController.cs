using System;
using GameName.Core.Commissions;
using GameName.Core.Dialogue;

namespace GameName.UI.Flow
{
    // 대화 패널의 입력 처리를 담당한다. 대사가 진짜로 몇 줄인지, 다음이 어디로
    // 가는지는 전혀 계산하지 않는다 — IDialogueProgressor가 이미 아는 것만
    // 그대로 따른다.
    //
    // 여러 옵션(분기)이 있는 노드가 오면 이 화면은 아직 선택지를 보여주지
    // 않고 0번 옵션으로만 진행한다 — 이번 데모 대사는 전부 옵션이 하나뿐이라
    // 문제가 없다. 특수 의뢰가 실제로 분기를 쓰게 되면, 이 부분을 옵션 개수만큼
    // 버튼을 그리는 방식으로 확장하면 된다(데이터 구조는 이미 지원한다).
    public sealed class DialoguePanelController : IDisposable
    {
        private readonly DialoguePanelView _view;
        private readonly IDialogueProgressor _progressor;
        private readonly CommissionSession _commissionSession;

        public DialoguePanelController(
            DialoguePanelView view, IDialogueProgressor progressor, CommissionSession commissionSession)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _progressor = progressor ?? throw new ArgumentNullException(nameof(progressor));
            _commissionSession = commissionSession ?? throw new ArgumentNullException(nameof(commissionSession));

            _view.AdvanceRequested += RequestAdvance;

            Refresh();
        }

        // 스페이스 키 입력(DialogueAdvanceInput)과 버튼 클릭이 공통으로 호출한다.
        public void RequestAdvance()
        {
            if (_progressor.IsFinished)
            {
                // TODO 컷신 재생 지점: 기억 진입 연출(아직 아트 없음).
                _commissionSession.TryAdvanceToMemory();
                return;
            }

            _progressor.Advance(0);
            Refresh();
        }

        private void Refresh()
        {
            _view.SetLine(_progressor.CurrentLine);
            _view.SetAdvanceButtonText(_progressor.IsFinished ? "기억으로 들어가기" : "다음 (Space)");
        }

        public void Dispose()
        {
            _view.AdvanceRequested -= RequestAdvance;
        }
    }
}
