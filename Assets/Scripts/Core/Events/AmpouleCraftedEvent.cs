using System;
using System.Collections.Generic;
using GameName.Core.Emotions;

namespace GameName.Core.Events
{
    // 앰플 제작이 끝났을 때 발행된다.
    // 한 번에 여러 개를 만들어도 정신력은 1회만 소모되지만, 기록/이벤트에는
    // 만들어진 개별 배합을 모두 담아 무엇을 만들었는지 알 수 있게 한다.
    public readonly struct AmpouleCraftedEvent
    {
        public IReadOnlyList<Scent> Recipes { get; }

        public AmpouleCraftedEvent(IReadOnlyList<Scent> recipes)
        {
            Recipes = recipes == null ? Array.Empty<Scent>() : new List<Scent>(recipes);
        }
    }
}
