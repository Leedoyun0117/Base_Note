using System;
using GameName.Core.Events;
using GameName.Core.Hiromi;
using GameName.Core.Memories;
using GameName.Core.Trust;

namespace GameName.UI.MemoryRoom
{
    // 상단 바에 신뢰 · 히로민 게이지 · 기회 · 손에 든 기억의 색 보유 여부를 그려 넣는다.
    //
    // 값 계산은 하나도 하지 않는다 — 전부 기존 리더 인터페이스(ITrustReader,
    // IHiromiReader, IChanceReader, IExtractedMemoryStore)를 조회해 그대로
    // 문구·막대로 넘긴다. 다시 그릴 계기만 이벤트로 안다.
    //
    // "다음 기억으로" 조작은 여기 없다 — 가방 화면의 레버가 맡는다. 이
    // 컨트롤러는 그 판단에 참고할 정보만 보여 준다.
    //
    // 첫 RoomStartedEvent는 GameSession 조립 중(이 컨트롤러가 만들어지기 전)에
    // 이미 지나갔으므로, 생성자에서 한 번 강제로 전부 그린다.
    public sealed class MemoryRoomHudController : IDisposable
    {
        private readonly MemoryRoomHudView _view;
        private readonly ITrustReader _trust;
        private readonly IHiromiReader _hiromi;
        private readonly int _hiromiMoveThreshold;
        private readonly IChanceReader _chance;
        private readonly IExtractedMemoryStore _memories;
        private readonly IDisposable[] _subscriptions;

        public MemoryRoomHudController(
            MemoryRoomHudView view,
            ITrustReader trust,
            IHiromiReader hiromi,
            int hiromiMoveThreshold,
            IChanceReader chance,
            IExtractedMemoryStore memories,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _trust = trust ?? throw new ArgumentNullException(nameof(trust));
            _hiromi = hiromi ?? throw new ArgumentNullException(nameof(hiromi));
            _hiromiMoveThreshold = hiromiMoveThreshold;
            _chance = chance ?? throw new ArgumentNullException(nameof(chance));
            _memories = memories ?? throw new ArgumentNullException(nameof(memories));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _subscriptions = new[]
            {
                // 방이 바뀌면 신뢰는 시작값으로 리셋되지만, 히로민·기회·손에 든
                // 기억은 런 전체에 걸쳐 이어지므로 그대로 다시 읽는다.
                eventBus.Subscribe<RoomStartedEvent>(_ => RenderAll()),
                eventBus.Subscribe<TrustChangedEvent>(_ => _view.SetTrust(_trust.Current)),
                eventBus.Subscribe<HiromiChangedEvent>(_ => RenderHiromi()),
                eventBus.Subscribe<ChanceChangedEvent>(_ => _view.SetChance(_chance.Remaining)),
                // 손에 든 기억이 바뀌는 계기: 추출로 하나가 들어온다.
                eventBus.Subscribe<MemoryColorRevealedEvent>(_ => RenderMemories()),
            };

            RenderAll();
        }

        private void RenderAll()
        {
            _view.SetTrust(_trust.Current);
            RenderHiromi();
            _view.SetChance(_chance.Remaining);
            RenderMemories();
        }

        private void RenderHiromi() => _view.SetHiromi(_hiromi.Remaining, _hiromiMoveThreshold);

        // 색별 개수가 아니라 보유 여부만 넘긴다 — HUD는 "이 색을 갖고 있나"만
        // 점으로 요약하고, 몇 개이고 어느 단서에서 나왔는지는 가방·복원도의 몫이다.
        private void RenderMemories()
        {
            var red = false;
            var green = false;
            var blue = false;

            foreach (var memory in _memories.All)
            {
                switch (memory.Color)
                {
                    case MemoryColor.Red: red = true; break;
                    case MemoryColor.Green: green = true; break;
                    default: blue = true; break;
                }
            }

            _view.SetHeldColors(red, green, blue);
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }
    }
}
