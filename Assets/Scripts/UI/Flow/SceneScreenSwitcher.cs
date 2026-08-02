using System;
using GameName.Core.Events;
using GameName.UI.Session;
using UnityEngine;

namespace GameName.UI.Flow
{
    // 위치 + 의뢰 단계에 따라 조향실/기억 방/분석실 화면 중 하나만 활성화한다.
    // 세 화면의 내부 구조(View/Controller)는 전혀 건드리지 않는다 — GameObject를
    // 켜고 끌 뿐이다. 기록지와 이 오버레이(FlowOverlay)는 전환 대상이 아니다
    // (둘 다 스스로 보이고 숨기를 관리한다).
    //
    // SetActive를 쓰는 이유: 각 화면의 Bootstrap(PerfumeryBootstrap,
    // MemoryRoomBootstrap, AnalysisRoomBootstrap)은 이미 OnEnable/OnDisable에서
    // 패널 컨트롤러를 새로 만들고 정리하도록 구현되어 있다. GameObject.SetActive는
    // Unity가 이 OnEnable/OnDisable을 그대로 불러주므로, 화면이 바뀔 때마다
    // 구독 해제 + 재구독이 자동으로 일어난다 — 화면 쪽 코드를 한 줄도 바꾸지
    // 않고 재사용할 수 있다.
    //
    // 반대로 세 화면을 전부 활성 상태로 켜 둔 채 스타일(style.display)만
    // 숨기는 방식은 쓰지 않는다. 그러면 보이지 않는 화면들도 Core 이벤트가
    // 발행될 때마다 매번 다시 그리는 낭비가 계속 일어나고, "지금 이 화면 하나만
    // 유효하다"는 화면 전환의 의미 자체와도 어긋난다. 매번 다시 만드는 대신
    // 상태를 살려 두고 싶다면 나중에 이 결정을 바꾸면 되지만, 지금은 프로토타입
    // 규모에서 재구독 비용이 무시할 만하고 구현이 훨씬 단순한 SetActive 쪽을
    // 택한다.
    public sealed class SceneScreenSwitcher : MonoBehaviour
    {
        [SerializeField] private GameSessionBootstrap _gameSession;
        [SerializeField] private GameObject _perfumeryScreen;
        [SerializeField] private GameObject _memoryRoomScreen;
        [SerializeField] private GameObject _analysisRoomScreen;
        [SerializeField] private GameObject _finalCraftingScreen;
        [SerializeField] private GameObject _completionScreen;

        private IDisposable _moveSubscription;
        private IDisposable _stageSubscription;

        private void OnEnable()
        {
            var session = _gameSession.Session;
            _moveSubscription = session.EventBus.Subscribe<MemoryRoomMoveCompletedEvent>(_ => Refresh());
            _stageSubscription = session.EventBus.Subscribe<CommissionStageChangedEvent>(_ => Refresh());

            Refresh();
        }

        private void OnDisable()
        {
            _moveSubscription?.Dispose();
            _stageSubscription?.Dispose();
        }

        private void Refresh()
        {
            var session = _gameSession.Session;
            var selected = ActiveScreenSelector.Select(
                session.CommissionSession.Stage, session.PlayerLocation.Current,
                session.PerfumeryRoomNodeId, session.AnalysisRoomNodeId);

            SetActiveIfChanged(_perfumeryScreen, selected == ActiveScreen.Perfumery);
            SetActiveIfChanged(_analysisRoomScreen, selected == ActiveScreen.AnalysisRoom);
            SetActiveIfChanged(_memoryRoomScreen, selected == ActiveScreen.MemoryRoom);
            SetActiveIfChanged(_finalCraftingScreen, selected == ActiveScreen.FinalCrafting);
            SetActiveIfChanged(_completionScreen, selected == ActiveScreen.Completion);
        }

        // 이미 원하는 상태면 SetActive를 다시 부르지 않는다 — 값이 같아도
        // SetActive 호출 자체가 OnEnable/OnDisable을 다시 트리거하지는 않지만,
        // 불필요한 호출을 피해 의도를 명확히 한다.
        private static void SetActiveIfChanged(GameObject screen, bool active)
        {
            if (screen != null && screen.activeSelf != active)
                screen.SetActive(active);
        }
    }
}
