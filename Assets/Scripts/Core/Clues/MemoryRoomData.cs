using System;
using System.Collections.Generic;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Clues
{
    // 기억 방 하나에 대해 기획자가 채워 넣는 데이터 묶음(정답 + 단서 목록).
    // RoomDataValidator가 이 묶음을 통째로 검사할 수 있도록 한데 모아 둔다.
    public sealed class MemoryRoomData
    {
        public MemoryRoomAnswer Answer { get; }
        public IReadOnlyList<ClueDefinition> Clues { get; }

        public MemoryRoomData(MemoryRoomAnswer answer, IReadOnlyList<ClueDefinition> clues)
        {
            Answer = answer ?? throw new ArgumentNullException(nameof(answer));
            Clues = clues ?? throw new ArgumentNullException(nameof(clues));
        }
    }
}
