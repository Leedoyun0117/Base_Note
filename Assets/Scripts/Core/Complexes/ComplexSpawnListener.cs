using System;
using GameName.Core.Events;
using GameName.Core.Mind;

namespace GameName.Core.Complexes
{
    // 매 턴, 안정 축 위치에 따른 확률로 새 컴플렉스를 하나 발생시키는 리스너.
    //
    // TurnAdvancedEvent를 듣고, 그 턴의 결정적 굴림값이 IComplexSpawnPolicy가
    // 낸 확률보다 작으면 IComplexDrawSource에서 하나 뽑아 ActiveComplexList에
    // 넣는다. 목록이 꽉 찼으면 TryActivate가 false를 내고 이 리스너는 조용히
    // 넘어간다(다음 턴에 다시 시도).
    //
    // 굴림은 (시드, 턴 번호) 해시로 결정된다 — System.Random의 누적 상태에
    // 기대지 않으므로 같은 시드·같은 턴이면 언제나 같은 결과다(BranchResolver와
    // 같은 철학). 시드는 RunDefinition.Seed를 재사용한다.
    //
    // 조립 순서 주의: ActiveComplexList보다 나중에 생성해야 한다. 그래야 매
    // 턴 TurnAdvancedEvent에서 "지속 턴 감소 → 이번 턴 발생" 순서가 되어,
    // 갓 생긴 컴플렉스가 같은 턴에 곧바로 한 턴을 잃지 않는다.
    public sealed class ComplexSpawnListener
    {
        private readonly IComplexSpawnPolicy _policy;
        private readonly IStabilityReader _stability;
        private readonly IActiveComplexList _active;
        private readonly IComplexDrawSource _drawSource;
        private readonly int _seed;

        public ComplexSpawnListener(
            IComplexSpawnPolicy policy,
            IStabilityReader stability,
            IActiveComplexList active,
            IComplexDrawSource drawSource,
            int seed,
            IEventBus eventBus)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _stability = stability ?? throw new ArgumentNullException(nameof(stability));
            _active = active ?? throw new ArgumentNullException(nameof(active));
            _drawSource = drawSource ?? throw new ArgumentNullException(nameof(drawSource));
            _seed = seed;
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            eventBus.Subscribe<TurnAdvancedEvent>(OnTurnAdvanced);
        }

        private void OnTurnAdvanced(TurnAdvancedEvent e)
        {
            var chance = _policy.SpawnChance(_stability.Position);
            if (chance <= 0f)
                return;

            if (Roll(_seed, e.Turn) >= chance)
                return;

            if (_drawSource.TryDraw(e.Turn, out var definition) && definition != null)
                _active.TryActivate(definition);
        }

        // (시드, 턴) → [0,1) 결정적 굴림. FNV-1a로 두 정수를 섞는다.
        private static double Roll(int seed, int turn)
        {
            unchecked
            {
                const uint offsetBasis = 2166136261;
                const uint prime = 16777619;

                var hash = offsetBasis;
                var mixed = (uint)seed ^ ((uint)turn * 2654435769u);
                for (var i = 0; i < 4; i++)
                {
                    hash = (hash ^ (mixed & 0xFF)) * prime;
                    mixed >>= 8;
                }

                return hash / (uint.MaxValue + 1.0);
            }
        }
    }
}
