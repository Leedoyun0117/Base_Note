using System;
using GameName.Core.Complexes;
using GameName.Core.Events;
using GameName.Core.Mind;

namespace GameName.UI.MemoryRoom
{
    // 상단 바에 턴 진행 · 안정 축 위치 · 활성 컴플렉스 수를 그려 넣는다.
    //
    // 값 계산은 하나도 하지 않는다 — 전부 리더 인터페이스(ITurnReader,
    // IStabilityReader, IActiveComplexListReader)를 조회해 그대로 문구로 넘긴다.
    // 다시 그릴 계기만 이벤트로 안다.
    //
    // 첫 RoomStartedEvent는 GameSession 조립 중(이 컨트롤러가 만들어지기 전)에
    // 이미 지나갔으므로, 생성자에서 한 번 강제로 전부 그린다.
    public sealed class MemoryRoomHudController : IDisposable
    {
        private readonly MemoryRoomHudView _view;
        private readonly ITurnReader _turns;
        private readonly IStabilityReader _stability;
        private readonly IActiveComplexListReader _activeComplexes;
        private readonly IDisposable[] _subscriptions;

        public MemoryRoomHudController(
            MemoryRoomHudView view,
            ITurnReader turns,
            IStabilityReader stability,
            IActiveComplexListReader activeComplexes,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _turns = turns ?? throw new ArgumentNullException(nameof(turns));
            _stability = stability ?? throw new ArgumentNullException(nameof(stability));
            _activeComplexes = activeComplexes ?? throw new ArgumentNullException(nameof(activeComplexes));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _subscriptions = new[]
            {
                eventBus.Subscribe<RoomStartedEvent>(_ => RenderAll()),
                // 턴이 넘어가면 남은 턴 표시도 같이 줄어야 하므로 컴플렉스 목록도 다시 그린다.
                eventBus.Subscribe<TurnAdvancedEvent>(_ => { RenderTurn(); RenderComplexes(); }),
                eventBus.Subscribe<StabilityChangedEvent>(_ => RenderStability()),
                eventBus.Subscribe<ComplexActivatedEvent>(_ => RenderComplexes()),
                eventBus.Subscribe<ComplexExpiredEvent>(_ => RenderComplexes()),
            };

            RenderAll();
        }

        private void RenderAll()
        {
            RenderTurn();
            RenderStability();
            RenderComplexes();
        }

        private void RenderTurn() => _view.SetTurn(_turns.CurrentTurn, _turns.TurnsToSurvive);
        private void RenderStability() =>
            _view.SetStability(_stability.Position, _stability.Min, _stability.Max);
        private void RenderComplexes() => _view.SetComplexes(_activeComplexes.InPriorityOrder);

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();

            _view.CancelStabilityAnimation();
        }
    }
}
