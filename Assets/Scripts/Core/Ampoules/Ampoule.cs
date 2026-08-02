using System;
using GameName.Core.Emotions;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Ampoules
{
    // 조향실에서 만든 향을 담는 용기. 목표 기억 방과 향을 함께 갖는다.
    // 플레이어 자신이 조향한 것이므로 숨길 정보가 없다 — 단서와 달리 공개/진실을
    // 나눌 필요가 없고, 이 타입 자체가 그대로 UI에 노출되어도 안전하다.
    public sealed class Ampoule : IInventoryItem, IEquatable<Ampoule>
    {
        public AmpouleId Id { get; }
        public MemoryRoomId TargetRoomId { get; }
        public Scent Scent { get; }

        public InventoryItemCategory Category => InventoryItemCategory.Ampoule;

        public Ampoule(AmpouleId id, MemoryRoomId targetRoomId, Scent scent)
        {
            Id = id;
            TargetRoomId = targetRoomId;
            Scent = scent ?? throw new ArgumentNullException(nameof(scent));
        }

        // 인벤토리는 이 식별자로 "같은 앰플"을 알아본다. 같은 배합으로 여러 개를
        // 한 번에 만들어도 각 앰플은 서로 다른 물리적 개체이므로, 조향 처리기가
        // 앰플마다 고유한 Id를 부여해 서로 다른 개체로 취급되게 한다.
        public bool Equals(Ampoule other) => other != null && Id.Equals(other.Id);
        public override bool Equals(object obj) => Equals(obj as Ampoule);
        public override int GetHashCode() => Id.GetHashCode();
    }
}
