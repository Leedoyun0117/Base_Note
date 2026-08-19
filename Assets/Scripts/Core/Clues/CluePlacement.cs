using System;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Clues
{
    // "이 단서는 처음에 이 방에 놓여 있다"는 최초 배치 하나.
    //
    // 예전에는 이 사실이 ClueDefinition.RoomId에 들어 있었지만, 소속 방이
    // 플레이 중에 바뀌게 되면서 정의(불변 진실)와 배치(가변 상태)를 갈라놓아야
    // 했다. 그 둘을 잇는 자리가 여기다 — 기획 데이터(MemoryRoomData)에서
    // 추적기(IMemoryRoomClueTracker)로 "시작 상태"를 전달하는 용도로만 쓰이고,
    // 그 뒤로는 추적기 안의 상태가 유일한 진실 원천이 된다.
    public sealed class CluePlacement
    {
        public MemoryRoomId RoomId { get; }
        public ClueDefinition Clue { get; }

        public CluePlacement(MemoryRoomId roomId, ClueDefinition clue)
        {
            RoomId = roomId;
            Clue = clue ?? throw new ArgumentNullException(nameof(clue));
        }
    }
}
