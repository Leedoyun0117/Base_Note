using System;
using GameName.Core.Events;

namespace GameName.UI.MemoryRoom
{
    // 라운드를 버텨 냈을 때(RoundSurvivedEvent), 그리고 판 전체가 끝났을 때
    // (RunCompletedEvent) 상단 안내 줄에 한 문장을 띄운다.
    //
    // 규칙은 하나도 판단하지 않는다 — 사건을 받아 문구로만 옮긴다. 자원
    // 페널티로 라운드가 무너지는 실패 경로는 아직 Core에 없어(RunProgressor 주석
    // 참조) 여기서도 다루지 않는다. 생기면 그 사건을 여기에 더한다.
    //
    // 마지막 라운드에서는 RoundSurvivedEvent 직후 RunProgressor가 곧바로
    // RunCompletedEvent를 낸다. 그 뒤에도 RoundSurvivedEvent 순회가 이어져
    // "라운드 클리어"가 "모든 라운드" 문구를 덮어쓰지 않도록, 한 번 끝난 판은
    // 래치(_completed)로 문구를 고정한다.
    //
    // 다음 라운드가 시작되면(RoomStartedEvent) 직전 라운드의 "클리어" 문구를
    // 지운다 — 안 그러면 새 방까지 문구가 남는다.
    public sealed class RoundOutcomeController : IDisposable
    {
        private readonly MemoryRoomHudView _hud;
        private readonly IDisposable[] _subscriptions;

        private bool _completed;

        public RoundOutcomeController(MemoryRoomHudView hud, IEventBus eventBus)
        {
            _hud = hud ?? throw new ArgumentNullException(nameof(hud));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _subscriptions = new[]
            {
                eventBus.Subscribe<RoundSurvivedEvent>(_ => OnRoundSurvived()),
                eventBus.Subscribe<RunCompletedEvent>(_ => OnRunCompleted()),
                eventBus.Subscribe<RoomStartedEvent>(_ => { if (!_completed) _hud.SetMessage(null); }),
            };
        }

        private void OnRoundSurvived()
        {
            if (_completed) return;
            _hud.SetMessage("이 기억을 버텨 냈다 — 라운드 클리어");
        }

        private void OnRunCompleted()
        {
            _completed = true;
            _hud.SetMessage("모든 라운드를 지나왔다");
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }
    }
}
