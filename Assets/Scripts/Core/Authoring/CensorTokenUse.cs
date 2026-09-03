using GameName.Core.Dialogue;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Authoring
{
    // 검열 토큰이 원문 어딘가에 한 번 쓰인 사실.
    //
    // RoomId를 Location과 별도로 드는 이유: 어떤 규칙은 방 단위로 판단해야 하고
    // (그 방 단서가 이 색을 내주는가) 어떤 규칙은 판 단위로 판단해야 한다(같은
    // 키가 어디서나 같은 색인가). 사람이 읽는 문장에서 방을 도로 파내게 할 수는 없다.
    //
    // 반대로 Location이 문자열인 이유: 이 값은 오직 문제를 보고할 때만 쓰이고,
    // 저작자가 원문에서 그 자리를 찾아갈 수 있으면 그만이다. 대사와 선택지를
    // 구조로 구분해 두면 규칙마다 다시 문장으로 조립해야 한다.
    public readonly struct CensorTokenUse
    {
        public CensorKey Key { get; }
        public MemoryColor Color { get; }
        public MemoryRoomId RoomId { get; }
        public string Location { get; }

        public CensorTokenUse(CensorKey key, MemoryColor color, MemoryRoomId roomId, string location)
        {
            Key = key;
            Color = color;
            RoomId = roomId;
            Location = location;
        }
    }
}
