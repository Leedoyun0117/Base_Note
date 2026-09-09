using System;
using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.Inventory;

namespace GameName.Core.Clues
{
    // 다음 방(기억)으로 넘어가면 손에 든 단서를 전부 버린다.
    //
    // 각 기억은 그 안에서 완결된다 — 그 방에서 주운 물건으로 그 방의 대화에
    // 답하고, 떠날 때 다 두고 나온다. 방을 넘어 단서를 들고 다니며 나중 방의
    // 질문에 답하는 흐름은 이제 없다.
    //
    // 버리기(ClueDiscardProcessor)를 그대로 쓴다 — 추출·검열과 달리 아무것도
    // 돌려주지 않고 사라지는 것이 이 전이의 뜻이고, ClueDiscardedEvent가 나가
    // InventoryProjection이 가방을 비운다.
    //
    // 첫 RoomStartedEvent(방 1 시작)는 아직 아무것도 손에 없어 무해하다.
    public sealed class RoomEntryInventoryClear
    {
        public RoomEntryInventoryClear(
            IPlayerInventory inventory, ClueDiscardProcessor discard, IEventBus eventBus)
        {
            if (inventory == null) throw new ArgumentNullException(nameof(inventory));
            if (discard == null) throw new ArgumentNullException(nameof(discard));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            eventBus.Subscribe<RoomStartedEvent>(_ =>
            {
                // 버리기가 ClueDiscardedEvent → InventoryProjection로 가방을
                // 즉시 건드리므로, 순회 중 컬렉션이 바뀌지 않게 먼저 모아 둔다.
                var held = new List<ClueInfo>();
                foreach (var item in inventory.Items)
                    if (item is ClueInfo clue)
                        held.Add(clue);

                foreach (var clue in held)
                {
                    // 손에 든 것(Collected)이면 버리기가 상태까지 옮기고 가방을 비운다.
                    if (discard.Discard(clue.Id).Succeeded)
                        continue;

                    // 버릴 수 없는 단계(이미 대화에 쓴 것 등)가 칸을 잡고 있으면
                    // 표시만이라도 치운다 — 방을 넘어온 뒤 남아 보이면 안 된다.
                    inventory.TryRemove(clue);
                }
            });
        }
    }
}
