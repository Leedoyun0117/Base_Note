using GameName.Core.MemoryRooms;

namespace GameName.Core.Events
{
    // 방의 대화가 끝까지 진행되어 닫혔다는 사실 — 다음 대사가 없는 선택지를
    // 골랐을 때다.
    //
    // 이것은 "성공적으로 대화를 마쳤다"는 뜻이지만, 그 순간 신뢰도가 0이라면
    // 방은 실패로 닫힌다. 그 판단은 여기서 하지 않고 RoomCompletionArbiter가
    // 신뢰도까지 함께 보고 정한다 — 대화 진행기는 "대화가 끝났다"는 사실만
    // 알리고, 그것이 클리어인지 실패인지는 모른다.
    public readonly struct DialogueEndedEvent
    {
        public MemoryRoomId RoomId { get; }

        public DialogueEndedEvent(MemoryRoomId roomId)
        {
            RoomId = roomId;
        }
    }
}
