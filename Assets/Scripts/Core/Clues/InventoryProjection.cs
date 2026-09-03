using System;
using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.Inventory;

namespace GameName.Core.Clues
{
    // 단서 단계(ClueState)를 표시용 인벤토리에 그대로 비추는 리스너.
    //
    // 수집 처리기가 Available → Collected 전이를 소유하고, 이 타입은 그 사건을
    // 듣고 인벤토리 슬롯에 ClueInfo를 넣는다 — 상태 전이와 슬롯 관리를 한
    // 타입이 함께 갖지 않게 갈라 둔 자리다. 인벤토리는 진실이 아니라 ClueState의
    // 투영이다.
    //
    // 방마다 비워진다(RoomStartedEvent). 인벤토리도 ClueStateStore처럼 방 스코프다.
    //
    // "가방이 가득 찼는가"는 ClueCollectionProcessor가 전이 전에 이미 확인하므로,
    // 정상 플레이 경로에서 여기 TryStore가 실패할 일은 없다. 그래도 실패하면
    // (동시성, 조립 오류 등) ClueState는 이미 Collected로 바뀐 뒤라 조용히
    // 넘기면 손에 없는 단서가 생기므로, 예외로 드러낸다.
    public sealed class InventoryProjection
    {
        private readonly IPlayerInventory _inventory;
        private readonly IMemoryRoomClueTracker _clueTracker;

        public InventoryProjection(
            IPlayerInventory inventory, IMemoryRoomClueTracker clueTracker, IEventBus eventBus)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _clueTracker = clueTracker ?? throw new ArgumentNullException(nameof(clueTracker));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            eventBus.Subscribe<ClueCollectedEvent>(e => Add(e.ClueId));
            eventBus.Subscribe<ClueExtractedEvent>(e => Remove(e.ClueId));
            eventBus.Subscribe<RoomStartedEvent>(_ => Clear());
        }

        private void Add(ClueId clueId)
        {
            if (!_clueTracker.TryGetDefinition(clueId, out var definition))
                throw new InvalidOperationException($"수집된 단서({clueId})의 정의를 카탈로그에서 찾을 수 없다.");

            var result = _inventory.TryStore(definition.ToInfo());
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"수집된 단서({clueId})를 인벤토리에 넣지 못했다: {result.FailureReason}. " +
                    "방 스코프 인벤토리는 그 방 단서를 전부 담을 수 있어야 한다 — 조립 오류다.");
            }
        }

        private void Remove(ClueId clueId)
        {
            foreach (var item in _inventory.Items)
            {
                if (item is ClueInfo clue && clue.Id.Equals(clueId))
                {
                    _inventory.TryRemove(item);
                    return;
                }
            }
        }

        private void Clear()
        {
            var carried = new List<IInventoryItem>(_inventory.Items);
            foreach (var item in carried)
                _inventory.TryRemove(item);
        }
    }
}
