using System;
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
    // 이 타입 스스로 방 전환에 반응하지는 않는다 — 손에서 단서가 나가는 계기는
    // 추출·버리기·대화에 답으로 쓰기 세 사건뿐이다. 다음 방으로 넘어갈 때 손에
    // 남은 것을 비우는 것은 RoomEntryInventoryClear가 그 단서들을 버리기
    // (ClueDiscardedEvent)로 돌리고, 여기서 그 사건을 듣고 슬롯을 비우는 식이다.
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
            eventBus.Subscribe<ClueDiscardedEvent>(e => Remove(e.ClueId));
            eventBus.Subscribe<ClueUsedInDialogueEvent>(e => Remove(e.ClueId));
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
                    "빈 칸이 있는지는 수집 처리기가 전이 전에 이미 확인했어야 한다 — 조립 오류다.");
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
    }
}
