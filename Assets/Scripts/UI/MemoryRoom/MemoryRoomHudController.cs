using System;
using GameName.Core.Events;
using GameName.Core.Extraction;
using GameName.Core.Memories;
using GameName.Core.Trust;

namespace GameName.UI.MemoryRoom
{
    // 상단 바에 신뢰 · 남은 추출 자원 · 색별 기억제 보유 수를 그려 넣는다.
    //
    // 값 계산은 하나도 하지 않는다 — 전부 기존 리더 인터페이스(ITrustReader,
    // IExtractionBudget, IMemoryColorWallet)를 조회해 그대로 문구로 넘긴다.
    // 다시 그릴 계기만 이벤트로 안다.
    //
    // 첫 RoomStartedEvent는 GameSession 조립 중(이 컨트롤러가 만들어지기 전)에
    // 이미 지나갔으므로, 생성자에서 한 번 강제로 전부 그린다.
    public sealed class MemoryRoomHudController : IDisposable
    {
        private readonly MemoryRoomHudView _view;
        private readonly ITrustReader _trust;
        private readonly IExtractionBudget _budget;
        private readonly IMemoryColorWallet _wallet;
        private readonly IDisposable[] _subscriptions;

        public MemoryRoomHudController(
            MemoryRoomHudView view,
            ITrustReader trust,
            IExtractionBudget budget,
            IMemoryColorWallet wallet,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _trust = trust ?? throw new ArgumentNullException(nameof(trust));
            _budget = budget ?? throw new ArgumentNullException(nameof(budget));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _subscriptions = new[]
            {
                // 방이 바뀌면 신뢰는 시작값으로, 남은 자원은 그대로다 — 전부 다시 읽는다.
                eventBus.Subscribe<RoomStartedEvent>(_ => RenderAll()),
                eventBus.Subscribe<TrustChangedEvent>(_ => _view.SetTrust(_trust.Current)),
                eventBus.Subscribe<ClueExtractedEvent>(e => _view.SetExtraction(e.RemainingExtractions)),
                // 지갑이 바뀌는 두 계기: 추출로 색이 들어오고, 검열 해금으로 색이 나간다.
                eventBus.Subscribe<MemoryColorRevealedEvent>(_ => RenderWallet()),
                eventBus.Subscribe<CensorKeyUnlockedEvent>(_ => RenderWallet()),
            };

            RenderAll();
        }

        private void RenderAll()
        {
            _view.SetTrust(_trust.Current);
            _view.SetExtraction(_budget.Remaining);
            RenderWallet();
        }

        private void RenderWallet() =>
            _view.SetWallet(
                _wallet.GetCount(MemoryColor.Red),
                _wallet.GetCount(MemoryColor.Green),
                _wallet.GetCount(MemoryColor.Blue));

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }
    }
}
