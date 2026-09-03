using GameName.Core.MemoryRooms;

namespace GameName.Core.Events
{
    // 방 하나가 시작되었다는 사실. 런이 시작될 때와, 앞 방이 성공이든 실패든
    // 닫히고 다음 방으로 넘어갈 때 발행된다.
    //
    // 방마다 리셋되어야 하는 것들(신뢰 게이지, 단서 단계, 대화 진행)은 이
    // 사실을 각자 구독해 스스로 초기화한다 — RunProgressor가 그것들을 하나하나
    // 다시 만들거나 붙잡고 흔들면 그 자리가 곧 God Class가 되기 때문이다.
    //
    // RoomIndex를 함께 싣는 이유: 구독자가 RunDefinition의 방 목록에서 자기
    // 방을 집으려면 순서 번호가 필요하다. 방 식별자만으로 목록을 훑게 하면
    // 구독자마다 같은 순회를 반복한다.
    public readonly struct RoomStartedEvent
    {
        public MemoryRoomId RoomId { get; }
        public int RoomIndex { get; }

        public RoomStartedEvent(MemoryRoomId roomId, int roomIndex)
        {
            RoomId = roomId;
            RoomIndex = roomIndex;
        }
    }
}
