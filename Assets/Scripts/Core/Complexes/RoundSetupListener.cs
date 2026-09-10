using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Events;

namespace GameName.Core.Complexes
{
    // 라운드가 시작될 때 활성 컴플렉스를 비우고 그 라운드의 시작 컴플렉스를 건다.
    //
    // 컴플렉스는 라운드 스코프다 — 앞 라운드의 컴플렉스가 다음 라운드로 넘어오지
    // 않는다. RoomStartedEvent를 듣고 ActiveComplexList.Reset() → 시작 컴플렉스
    // 활성화 순서로 처리한다.
    //
    // ActiveComplexList가 스스로 RoomStartedEvent를 구독하지 않는 이유가 이것이다:
    // "라운드마다 리셋"은 기획 결정이라 목록 타입에 박지 않고 이 리스너가 정한다.
    public sealed class RoundSetupListener
    {
        public RoundSetupListener(
            IReadOnlyList<RoomDefinition> rounds,
            IActiveComplexList activeComplexes,
            ComplexCatalog catalog,
            IEventBus eventBus)
        {
            if (rounds == null) throw new ArgumentNullException(nameof(rounds));
            if (activeComplexes == null) throw new ArgumentNullException(nameof(activeComplexes));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            eventBus.Subscribe<RoomStartedEvent>(e =>
            {
                activeComplexes.Reset();

                if (e.RoomIndex < 0 || e.RoomIndex >= rounds.Count)
                    return;

                var startingId = rounds[e.RoomIndex].StartingComplexId;
                if (startingId.HasValue && catalog.TryGet(startingId.Value, out var definition))
                    activeComplexes.TryActivate(definition);
            });
        }
    }
}
