using System;
using System.Collections.Generic;
using GameName.Core.Ampoules;

namespace GameName.Core.Events
{
    // 앰플 제작이 끝났을 때 발행된다.
    // 한 번에 여러 개를 만들어도 정신력은 1회만 소모되지만, 기록/이벤트에는
    // 만들어진 개별 배합을 모두 담아 무엇을, 어느 방을 목표로 만들었는지 알 수
    // 있게 한다 — 향(Scent)만 담으면 목표 방이 유실된다.
    public readonly struct AmpouleCraftedEvent
    {
        public IReadOnlyList<AmpouleRecipe> Recipes { get; }

        public AmpouleCraftedEvent(IReadOnlyList<AmpouleRecipe> recipes)
        {
            Recipes = recipes == null ? Array.Empty<AmpouleRecipe>() : new List<AmpouleRecipe>(recipes);
        }
    }
}
