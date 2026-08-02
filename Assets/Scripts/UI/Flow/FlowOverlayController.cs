using System;
using GameName.Core.Commissions;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using UnityEngine.UIElements;

namespace GameName.UI.Flow
{
    // 대화 / 복귀 확인 / 복귀 후 안내, 세 하위 패널을 조립하고, 의뢰 단계 +
    // 현재 위치에 따라 그중 정확히 하나만 보이게 한다.
    //
    //   PreConversation                                        → 대화 패널
    //   InMemory이고 기억 진입/탈출 지점에 있고 정신력이 바닥임 → 복귀 확인 패널
    //   그 외 모든 단계/위치                                    → 아무 것도 안 보임
    //                                                             (조향실/기억 방/
    //                                                             분석실/최종 조향/
    //                                                             완료 화면이 대신
    //                                                             보인다 —
    //                                                             SceneScreenSwitcher 소관)
    //
    // ReturnedToReality에 진입한 직후 한 번 보여주던 "복귀 후 안내" 정적
    // 패널은 이제 보여주지 않는다 — 그 자리를 실제 화면(최종 조향)이 대신
    // 채우므로, 안내만 하고 아무 것도 할 수 없던 막다른 화면을 남겨둘 이유가
    // 없다.
    //
    // 각 하위 패널의 내부 구조는 건드리지 않는다 — 이 컨트롤러는 무엇을
    // 보여줄지만 정한다.
    public sealed class FlowOverlayController : IDisposable
    {
        private readonly VisualElement _root;
        private readonly DialoguePanelView _dialogueView;
        private readonly DialoguePanelController _dialogueController;
        private readonly ReturnToRealityPanelView _exitView;
        private readonly ReturnToRealityPanelController _exitController;

        private readonly CommissionSession _commissionSession;
        private readonly IMentalityGauge _mentalityGauge;
        private readonly IPlayerLocation _playerLocation;
        private readonly MemoryGraphNodeId _memoryEntryNodeId;

        private readonly IDisposable _stageSubscription;
        private readonly IDisposable _moveSubscription;
        private readonly IDisposable _mentalitySubscription;

        public DialoguePanelController DialogueController => _dialogueController;

        public FlowOverlayController(
            VisualElement root,
            CommissionSession commissionSession,
            IDialogueProgressor dialogueProgressor,
            IMentalityGauge mentalityGauge,
            IPlayerLocation playerLocation,
            MemoryGraphNodeId memoryEntryNodeId,
            IEventBus eventBus)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _commissionSession = commissionSession ?? throw new ArgumentNullException(nameof(commissionSession));
            _mentalityGauge = mentalityGauge ?? throw new ArgumentNullException(nameof(mentalityGauge));
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
            _memoryEntryNodeId = memoryEntryNodeId;
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _dialogueView = new DialoguePanelView(root.Q<VisualElement>("dialogue-panel"));
            _dialogueController = new DialoguePanelController(_dialogueView, dialogueProgressor, commissionSession);

            _exitView = new ReturnToRealityPanelView(root.Q<VisualElement>("exit-confirm-panel"));
            _exitController = new ReturnToRealityPanelController(_exitView, commissionSession, mentalityGauge, eventBus);

            _stageSubscription = eventBus.Subscribe<CommissionStageChangedEvent>(_ => Refresh());
            _moveSubscription = eventBus.Subscribe<MemoryRoomMoveCompletedEvent>(_ => Refresh());
            _mentalitySubscription = eventBus.Subscribe<MentalityChangedEvent>(_ => Refresh());

            Refresh();
        }

        private void Refresh()
        {
            var stage = _commissionSession.Stage;

            var showDialogue = stage == CommissionStage.PreConversation;

            // 정신력이 남아 있는 동안에는 계단에 서 있어도 복귀 확인 패널을
            // 띄우지 않는다 — 복귀는 정신력이 완전히 바닥났을 때만 선택할 수
            // 있는 행동이다. 정신력이 있는 동안 자유롭게 오가며 계속 탐색할지
            // 말지를 플레이어가 임의로 고를 수 있게 두지 않는다.
            var showExit = stage == CommissionStage.InMemory &&
                _playerLocation.Current.Equals(_memoryEntryNodeId) &&
                !_mentalityGauge.CanAct;

            _dialogueView.SetVisible(showDialogue);
            _exitView.SetVisible(showExit);

            // 아무 것도 보여줄 게 없으면 이 오버레이 UIDocument의 root 자체를
            // display:none으로 접는다. 여러 UIDocument(패널)이 겹쳐 있을 때는
            // picking-mode: Ignore만으로 클릭이 아래 패널까지 안정적으로
            // 통과한다는 보장이 없다 — display:none은 이 패널을 히트 테스트에서
            // 아예 빼버리므로 확실하다. 기록지(JournalVisibilityController)가
            // 이미 같은 방식을 쓰고 있다.
            _root.style.display = (showDialogue || showExit) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Dispose()
        {
            _stageSubscription.Dispose();
            _moveSubscription.Dispose();
            _mentalitySubscription.Dispose();
            _dialogueController.Dispose();
            _exitController.Dispose();
        }
    }
}
