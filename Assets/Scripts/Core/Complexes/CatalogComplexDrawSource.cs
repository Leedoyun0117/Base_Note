using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Events;

namespace GameName.Core.Complexes
{
    // IComplexDrawSource 기본 구현. 지금 라운드의 발생 풀에서 아직 활성이 아닌
    // 컴플렉스 하나를 결정적으로 뽑는다.
    //
    // RoomStartedEvent를 구독해 지금 몇 번째 라운드인지 안다. 뽑기는 (시드, 턴)
    // 해시로 결정되어 재현 가능하다(BranchResolver·ComplexSpawnListener와 같은
    // 철학). 뽑을 후보가 없으면(풀이 비었거나 전부 이미 활성) false.
    public sealed class CatalogComplexDrawSource : IComplexDrawSource
    {
        private readonly ComplexCatalog _catalog;
        private readonly IActiveComplexListReader _active;
        private readonly IReadOnlyList<RoomDefinition> _rounds;
        private readonly int _seed;

        private int _roundIndex = -1;

        public CatalogComplexDrawSource(
            ComplexCatalog catalog,
            IActiveComplexListReader active,
            IReadOnlyList<RoomDefinition> rounds,
            int seed,
            IEventBus eventBus)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _active = active ?? throw new ArgumentNullException(nameof(active));
            _rounds = rounds ?? throw new ArgumentNullException(nameof(rounds));
            _seed = seed;
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            eventBus.Subscribe<RoomStartedEvent>(e => _roundIndex = e.RoomIndex);
        }

        public bool TryDraw(int turn, out ComplexDefinition definition)
        {
            definition = null;
            if (_roundIndex < 0 || _roundIndex >= _rounds.Count)
                return false;

            var candidates = new List<ComplexDefinition>();
            foreach (var id in _rounds[_roundIndex].ComplexPoolIds)
            {
                if (IsActive(id))
                    continue;
                if (_catalog.TryGet(id, out var candidate))
                    candidates.Add(candidate);
            }

            if (candidates.Count == 0)
                return false;

            var index = (int)(StableHash(_seed, turn) % (uint)candidates.Count);
            definition = candidates[index];
            return true;
        }

        private bool IsActive(ComplexId id)
        {
            foreach (var entry in _active.InPriorityOrder)
            {
                if (entry.Definition.Id.Equals(id))
                    return true;
            }

            return false;
        }

        // FNV-1a로 (시드, 턴)을 섞는다. 플랫폼·런타임 무관하게 같은 입력이면 같은 값.
        private static uint StableHash(int seed, int turn)
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

                return hash;
            }
        }
    }
}
