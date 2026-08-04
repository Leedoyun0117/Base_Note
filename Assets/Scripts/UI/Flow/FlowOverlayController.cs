using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Commissions;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;
using UnityEngine.UIElements;

namespace GameName.UI.Flow
{
    // 대화 / 복귀 확인, 두 하위 패널을 조립하고, 의뢰 단계 + 현재 위치에 따라
    // 그중 정확히 하나만 보이게 한다.
    //
    //   PreConversation                                  → 대화 패널
    //   InMemory이고 계단 위 + 이탈을 명시적으로 요청했음 → 복귀 확인 패널
    //   그 외 모든 단계/위치                              → 아무 것도 안 보임
    //                                                             (조향실/기억 방/
    //                                                             분석실/최종 조향/
    //                                                             완료 화면이 대신
    //                                                             보인다 —
    //                                                             SceneScreenSwitcher 소관)
    //
    // 복귀 확인 패널은 정신력과 무관하게 뜬다 — "충분히 살펴봤다"는 판단은
    // 정신력이 바닥났을 때만 허용되는 것이 아니라 언제든 플레이어가 스스로
    // 내릴 수 있는 선택이어야 하기 때문이다(자세한 근거는
    // ReturnToRealityPanelController 주석 참고). 다만 "계단 위에 있다"는
    // 사실만으로는 띄우지 않는다 — 기억 진입 직후 시작 위치가 우연히 계단인
    // 경우에도 조건이 참이 되어, 아무것도 하지 않았는데 이탈 확인이 뜨는
    // 오작동이 생긴다. 그래서 지도에서 계단을 눌러 이탈 의사를 명시적으로
    // 표현했을 때(MemoryExitRequestedEvent)만 띄우고, 계단을 벗어나면 그
    // 의사 표시는 사라진다 — 다시 누르면 언제든 재요청할 수 있다.
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
        private readonly IPlayerLocation _playerLocation;
        private readonly MemoryGraphNodeId _memoryEntryNodeId;

        private bool _exitRequested;

        private readonly IDisposable _stageSubscription;
        private readonly IDisposable _moveSubscription;
        private readonly IDisposable _exitRequestSubscription;

        public DialoguePanelController DialogueController => _dialogueController;

        public FlowOverlayController(
            VisualElement root,
            CommissionSession commissionSession,
            IDialogueProgressor dialogueProgressor,
            IPlayerLocation playerLocation,
            MemoryGraphNodeId memoryEntryNodeId,
            IPlayerInventory inventory,
            IClueStorage clueStorage,
            IClueAnalysisProgress analysisProgress,
            IReadOnlyList<MemoryRoomId> roomIds,
            IMemoryRoomRestorationTracker restorationTracker,
            IEventBus eventBus)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _commissionSession = commissionSession ?? throw new ArgumentNullException(nameof(commissionSession));
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
            _memoryEntryNodeId = memoryEntryNodeId;
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _dialogueView = new DialoguePanelView(root.Q<VisualElement>("dialogue-panel"));
            _dialogueController = new DialoguePanelController(_dialogueView, dialogueProgressor, commissionSession);

            _exitView = new ReturnToRealityPanelView(root.Q<VisualElement>("exit-confirm-panel"));
            _exitController = new ReturnToRealityPanelController(
                _exitView, commissionSession, inventory, clueStorage, analysisProgress, roomIds,
                restorationTracker, eventBus);

            _stageSubscription = eventBus.Subscribe<CommissionStageChangedEvent>(OnStageChanged);
            _moveSubscription = eventBus.Subscribe<MemoryRoomMoveCompletedEvent>(OnMoveCompleted);
            _exitRequestSubscription = eventBus.Subscribe<MemoryExitRequestedEvent>(_ =>
            {
                _exitRequested = true;
                Refresh();
            });

            // 취소를 누르면 패널 안의 확정/취소 버튼만 접히는 게 아니라
            // "계단 앞에 서 있습니다" 패널 자체가 꺼져야 한다 — 취소도 "이탈
            // 의사를 거뒀다"는 뜻이므로, 다시 나가려면 지도의 "나가기"를
            // 다시 눌러야 한다.
            _exitView.ExitCancelled += OnExitCancelled;

            Refresh();
        }

        private void OnExitCancelled()
        {
            _exitRequested = false;
            Refresh();
        }

        // 이 컨트롤러는 한 번 만들어지면 씬 전체(여러 의뢰)에 걸쳐 계속
        // 살아있다. 그런데 이탈 의사 표시(_exitRequested)는 "그 자리에서
        // 방금 나가기를 눌렀다"는 한 순간짜리 상태다 — 의뢰 단계가 바뀌면
        // (대화 시작/기억 진입/현실 복귀/완료 등 무엇이든) 그 맥락은 이미
        // 끝난 것이므로 항상 초기화한다. 이걸 안 하면, 한 의뢰에서 나가기를
        // 눌렀던 기록이 다음 의뢰로 새 나가지 않고 남아 있다가, 다음 의뢰의
        // 시작 위치가 하필 계단이라 조건이 다시 우연히 참이 되어("계단
        // 위에 있음"은 스폰 시점에 항상 참이다) 아무것도 안 했는데 이탈
        // 확인이 또 뜨는 버그로 이어진다.
        private void OnStageChanged(CommissionStageChangedEvent e)
        {
            _exitRequested = false;
            Refresh();
        }

        private void OnMoveCompleted(MemoryRoomMoveCompletedEvent e)
        {
            // 계단을 벗어나면 이탈 의사 표시도 함께 사라진다 — 다시 계단을
            // 눌러야("MemoryExitRequestedEvent") 확인 화면이 재등장한다.
            if (!e.NewPosition.Equals(_memoryEntryNodeId))
                _exitRequested = false;

            Refresh();
        }

        private void Refresh()
        {
            var stage = _commissionSession.Stage;

            var showDialogue = stage == CommissionStage.PreConversation;
            var showExit = stage == CommissionStage.InMemory &&
                _playerLocation.Current.Equals(_memoryEntryNodeId) &&
                _exitRequested;

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
            _exitView.ExitCancelled -= OnExitCancelled;
            _stageSubscription.Dispose();
            _moveSubscription.Dispose();
            _exitRequestSubscription.Dispose();
            _dialogueController.Dispose();
            _exitController.Dispose();
        }
    }
}
