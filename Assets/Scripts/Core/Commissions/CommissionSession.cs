using System;
using System.Collections.Generic;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Journal;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Commissions
{
    // 의뢰 하나가 진행되는 동안 유지되는 상태(단계)를 관리하고, 새 의뢰가
    // 시작될 때 필요한 초기화를 한 곳에서 조율한다.
    //
    // 이 타입은 무엇을 초기화하는지(정신력/인벤토리/보관함/복원 상태/분석
    // 진행도가 정확히 무엇인지)를 더 이상 알지 못한다 — 그 시스템들은 전부
    // IResettable이라는 좁은 경계로만 이 타입에 주입되고, Begin()은 그 목록을
    // 순서 무관하게 한 번씩 돌 뿐이다. 새 시스템이 "새 의뢰마다 비워져야
    // 한다"는 규칙에 합류하려면 그 목록에 추가되기만 하면 되고, 이 타입은
    // 전혀 바뀌지 않는다.
    //
    // 단서/방 그래프/정답/대화 대본처럼 "초기화"가 아니라 "통째로 다른 데이터로
    // 교체"가 필요한 것들은 여기서 다루지 않는다 — 그건 각자의 전용 Load
    // 인터페이스(IMemoryRoomClueLoader, IMemoryRoomGraphLoader, ...)로
    // 노출되고, GameSession.LoadCommission이 이 타입의 Begin()과 함께 정확한
    // 순서로 호출한다(자세한 순서 근거는 GameSession 쪽 주석 참고).
    public sealed class CommissionSession
    {
        private readonly IEventBus _eventBus;
        private readonly IReadOnlyList<IResettable> _resettableSystems;
        private readonly IJournal _journal;
        private readonly IDialogueProgressor _dialogueProgressor;
        private readonly IPlayerLocationMover _playerLocationMover;
        private readonly MemoryGraphNodeId _memoryEntryNodeId;

        public CommissionId? CurrentCommissionId { get; private set; }
        public CommissionStage Stage { get; private set; }

        public CommissionSession(
            IEventBus eventBus,
            IReadOnlyList<IResettable> resettableSystems,
            IJournal journal,
            IDialogueProgressor dialogueProgressor,
            IPlayerLocationMover playerLocationMover,
            MemoryGraphNodeId memoryEntryNodeId)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _resettableSystems = resettableSystems ?? throw new ArgumentNullException(nameof(resettableSystems));
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
            _dialogueProgressor = dialogueProgressor ?? throw new ArgumentNullException(nameof(dialogueProgressor));
            _playerLocationMover = playerLocationMover ?? throw new ArgumentNullException(nameof(playerLocationMover));
            _memoryEntryNodeId = memoryEntryNodeId;
        }

        // 새 의뢰를 시작한다. 이전 의뢰의 플레이 상태가 하나도 남지 않도록
        // 등록된 IResettable을 전부 초기화한 뒤, 대화 단계(PreConversation)로
        // 되돌린다.
        //
        // 단서/방 그래프/정답/대화 대본 교체는 호출자(GameSession)가 이 메서드
        // 호출 전후로 각 Load 인터페이스를 통해 직접 수행한다 — 특히 대화
        // 대본 교체는 반드시 이 메서드가 끝난 뒤(= journal.BeginCommission이
        // 불린 뒤)여야 한다. 대본 교체가 발행하는 첫 줄의
        // DialogueLineShownEvent가 새 의뢰의 기록 구간에 들어가야 하기
        // 때문이다.
        public void Begin(CommissionId commissionId)
        {
            foreach (var resettable in _resettableSystems)
                resettable.Reset();

            // 기록지 전환은 초기화가 아니다 — 이전 의뢰 기록은 그대로 보존한 채
            // 새 의뢰용 구간을 연다.
            _journal.BeginCommission(commissionId);

            CurrentCommissionId = commissionId;
            SetStage(CommissionStage.PreConversation);
        }

        // 대화가 끝난 뒤에만 기억으로 들어갈 수 있다. 대화 중에 기억으로
        // 넘어가려는 시도, 이미 기억 안이거나 현실로 돌아온 뒤의 재시도는
        // 전부 거부한다 — 특히 ReturnedToReality에서의 재시도를 막는 것이
        // "복귀 후 재진입 불가" 규칙의 유일한 강제 지점이다(그 근거는 설계
        // 근거 문서를 참고).
        public bool TryAdvanceToMemory()
        {
            if (Stage != CommissionStage.PreConversation)
                return false;
            if (!_dialogueProgressor.IsFinished)
                return false;

            // TODO 컷신 재생 지점: 기억 진입 연출(아직 아트 없음)이 여기 들어간다.
            // 지금은 연출 없이 곧장 위치를 기억 안 시작 지점으로 옮긴다.
            _playerLocationMover.MoveTo(_memoryEntryNodeId);
            SetStage(CommissionStage.InMemory);
            return true;
        }

        // 지도 위 어디에 있든 "이 기억은 충분히 살펴봤다"는 결심 하나로 계단
        // (기억 진입/이탈 지점)까지 곧장 돌아갈 수 있게 한다. 일반 탐색 이동
        // (MemoryRoomMovementProcessor.Move)과 달리 인접 여부나 비용을 전혀
        // 따지지 않는다 — 그 다음 걸어야 할 경로를 찾는 것 자체가 이 행동의
        // 목적이 아니라, 이탈이라는 결심을 표현하는 행동이기 때문이다. 위치만
        // 옮길 뿐 단계를 바꾸지는 않는다 — 실제 이탈 확정은 여전히
        // TryReturnToReality()가 별도로 확인한다. 그래서 계단이라는 장소
        // 자체는 사라지지 않는다: 이탈 확인 화면은 여전히 "계단 위에 있는가"를
        // 전제로만 뜬다.
        public bool TryReturnToEntryPoint()
        {
            if (Stage != CommissionStage.InMemory)
                return false;

            var previous = _playerLocationMover.Current;
            if (!previous.Equals(_memoryEntryNodeId))
            {
                _playerLocationMover.MoveTo(_memoryEntryNodeId);
                _eventBus.Publish(new MemoryRoomMoveCompletedEvent(previous, _memoryEntryNodeId));
            }

            // 위치 이동 여부와 무관하게 항상 발행한다 — 이미 계단 위에 있는
            // 상태에서 다시 눌러도(예: 취소 후 재요청) 여전히 "이탈하겠다"는
            // 의사 표시이기 때문이다. 이 이벤트가 곧 "이탈 확인 화면을 띄워도
            // 되는 유일한 신호"다 — 기억 진입 시 시작 위치가 우연히 계단인
            // 것과는 구분된다(TryAdvanceToMemory는 이 이벤트를 발행하지 않는다).
            _eventBus.Publish(new MemoryExitRequestedEvent());

            return true;
        }

        // 계단(기억 안 시작 지점과 같은 노드)에 서 있을 때만 현실로 복귀할 수
        // 있다. 확인 절차는 화면(되돌릴 수 없는 전환이므로 확정 버튼을 따로
        // 둔다) 쪽 책임이고, 이 메서드는 그 확정이 눌렸을 때 한 번만 호출된다.
        public bool TryReturnToReality()
        {
            if (Stage != CommissionStage.InMemory)
                return false;
            if (!_playerLocationMover.Current.Equals(_memoryEntryNodeId))
                return false;

            SetStage(CommissionStage.ReturnedToReality);
            return true;
        }

        // 최종 조향 결과를 의뢰인에게 제공해 의뢰를 완료 처리했을 때만 호출된다
        // (CommissionCompletionProcessor). 모든 방에 최종 향이 채워졌는지는
        // 이 타입이 알지 못한다 — 그 판단은 IFinalCraftingBoard에게 물어야
        // 하는 별개의 책임이라 호출자(CommissionCompletionProcessor)가 미리
        // 확인한 뒤에만 이 메서드를 부른다.
        public bool TryComplete()
        {
            if (Stage != CommissionStage.ReturnedToReality)
                return false;

            SetStage(CommissionStage.Completed);
            return true;
        }

        private void SetStage(CommissionStage newStage)
        {
            var previous = Stage;
            Stage = newStage;
            _eventBus.Publish(new CommissionStageChangedEvent(previous, newStage));
        }
    }
}
