using System.Collections.Generic;

namespace GameName.Core.Ampoules
{
    // IAmpouleCraftingQueue 기본 구현.
    // IResettable도 함께 구현한다 — 대기열에 남은 배합은 이전 의뢰가 정의한
    // 목표 방을 가리키므로, 새 의뢰가 시작되면(방 데이터가 통째로 교체되면)
    // 반드시 함께 비워져야 한다. 그 권한은 CommissionSession만 받는다.
    public sealed class AmpouleCraftingQueue : IAmpouleCraftingQueue, IResettable
    {
        private readonly List<AmpouleCraftingRequest> _items = new List<AmpouleCraftingRequest>();

        public IReadOnlyList<AmpouleCraftingRequest> Items => _items;

        public void Add(AmpouleCraftingRequest request) => _items.Add(request);

        public void RemoveAt(int index) => _items.RemoveAt(index);

        public void Clear() => _items.Clear();

        public void Reset() => _items.Clear();
    }
}
