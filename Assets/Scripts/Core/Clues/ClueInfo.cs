using System;
using GameName.Core.Inventory;

namespace GameName.Core.Clues
{
    // 단서에 대해 플레이어(UI, 인벤토리 등)에게 공개해도 되는 정보.
    // ClueDefinition.ToInfo()를 통해서만 만들어지는 단방향 변환이다.
    //
    // RoomId는 담지 않는다. 이 타입은 인벤토리에 그대로 담겨 오래 살아남는
    // "스냅샷"인데, 단서를 아무 방에나 버릴 수 있게 된 지금은 소속 방이 플레이
    // 중에 바뀐다 — 스냅샷에 방을 박아 두면 그 값이 조용히 거짓말을 하게 된다.
    // 지금 어느 방에 놓여 있는지는 항상 IMemoryRoomClueTracker에게 물어야 한다.
    //
    // 반대로 Kind는 담는다 — 벽에 붙은 것인지 바닥에 놓인 것인지는 방에
    // 들어서면 그냥 보이는 사실이라 감출 이유가 없고, 버릴 때 어디에 놓을지를
    // 정하려면 화면 쪽에도 반드시 있어야 하는 정보이기 때문이다.
    public sealed class ClueInfo : IInventoryItem, IEquatable<ClueInfo>
    {
        public ClueId Id { get; }
        public ClueKind Kind { get; }

        // 기획이 정한 자리이지 "지금 놓여 있는 자리"가 아니다 — 플레이어가
        // 다른 곳에 버렸다면 실제로 보이는 자리는 다를 수 있다. 그 차이를
        // 아는 것은 화면 쪽이고, 이 값은 바뀌지 않는 저작 사실이라 인벤토리에
        // 담겨 오래 살아남아도 거짓말이 되지 않는다(RoomId를 빼낸 것과 다른
        // 점이 여기다).
        public CluePositionRatio AuthoredPosition { get; }

        public InventoryItemCategory Category => InventoryItemCategory.Clue;

        public ClueInfo(ClueId id, ClueKind kind, CluePositionRatio authoredPosition)
        {
            Id = id;
            Kind = kind;
            AuthoredPosition = authoredPosition;
        }

        // 인벤토리는 이 식별자 하나로 "같은 단서"를 알아본다. 값이 아니라
        // 정체성(Id)으로 비교한다.
        public bool Equals(ClueInfo other) => other != null && Id.Equals(other.Id);
        public override bool Equals(object obj) => Equals(obj as ClueInfo);
        public override int GetHashCode() => Id.GetHashCode();
    }
}
