using System;
using GameName.Core.Emotions;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Clues
{
    // 단서에 대해 플레이어(UI, 인벤토리 등)에게 공개해도 되는 정보.
    // 실제 감정 구성(TrueComposition)은 담지 않으므로, 이 타입만 들고 있는
    // 코드는 거짓 단서의 진실을 구조적으로 읽을 수 없다 — ClueDefinition.ToInfo()를
    // 통해서만 만들어지는 단방향 변환이다.
    public sealed class ClueInfo : IInventoryItem, IEquatable<ClueInfo>
    {
        public ClueId Id { get; }
        public MemoryRoomId RoomId { get; }
        public EmotionBlend ApparentComposition { get; }

        public InventoryItemCategory Category => InventoryItemCategory.Clue;

        public ClueInfo(ClueId id, MemoryRoomId roomId, EmotionBlend apparentComposition)
        {
            Id = id;
            RoomId = roomId;
            ApparentComposition = apparentComposition ?? throw new ArgumentNullException(nameof(apparentComposition));
        }

        // 인벤토리는 이 식별자 하나로 "같은 단서"를 알아본다. 겉보기 구성 값이
        // 우연히 같은 서로 다른 단서와 혼동하지 않기 위해, 값이 아니라
        // 정체성(Id)으로 비교한다.
        public bool Equals(ClueInfo other) => other != null && Id.Equals(other.Id);
        public override bool Equals(object obj) => Equals(obj as ClueInfo);
        public override int GetHashCode() => Id.GetHashCode();
    }
}
