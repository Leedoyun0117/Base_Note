using System.Collections.Generic;
using GameName.Core.MemoryRooms;

namespace GameName.UI.Shared
{
    // 그래프 노드 id만으로는 "지금 있는 곳이 실제 기억 방인지, 어느 방인지"를
    // 알 수 없다(허브 노드도 같은 타입의 식별자이기 때문) — 세션이 아는 방 id
    // 목록과 대조해서 알아낸다. 이동/단서/시향 패널이 전부 "지금 방이 뭔가"를
    // 똑같은 방식으로 물어야 하므로, 세 곳에 같은 로직을 따로 만들지 않도록
    // 이 하나로 모은다.
    public static class CurrentRoomResolver
    {
        public static bool TryResolve(
            MemoryGraphNodeId currentNodeId, IReadOnlyList<MemoryRoomId> knownRoomIds, out MemoryRoomId roomId)
        {
            foreach (var candidate in knownRoomIds)
            {
                if (MemoryGraphNodeId.OfRoom(candidate).Equals(currentNodeId))
                {
                    roomId = candidate;
                    return true;
                }
            }

            roomId = default;
            return false;
        }
    }
}
