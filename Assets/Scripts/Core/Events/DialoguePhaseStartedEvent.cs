using GameName.Core.MemoryRooms;

namespace GameName.Core.Events
{
    // 이 방의 대화 국면이 시작되었다는 사실 — 이제 유키와의 대화를 연다.
    //
    // RoomStartedEvent와 갈라 둔 이유: 방이 시작된 순간(조사 국면)과 대화가
    // 시작되는 순간은 다르다. DialogueProgressor는 방이 열리자마자가 아니라
    // 이 사건에서 그 방의 대사를 싣는다. RoomIndex를 함께 싣는 것은
    // RoomStartedEvent와 같은 이유다 — 구독자가 방 목록에서 자기 방을 집는다.
    public readonly struct DialoguePhaseStartedEvent
    {
        public MemoryRoomId RoomId { get; }
        public int RoomIndex { get; }

        public DialoguePhaseStartedEvent(MemoryRoomId roomId, int roomIndex)
        {
            RoomId = roomId;
            RoomIndex = roomIndex;
        }
    }
}
