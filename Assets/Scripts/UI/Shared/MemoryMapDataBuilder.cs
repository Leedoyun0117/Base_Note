using System.Collections.Generic;
using GameName.Core.MemoryRooms;

namespace GameName.UI.Shared
{
    // 그래프/복원 상태/방문 여부로부터 지도가 그릴 데이터를 만드는 계산을
    // 한 곳에 모은다. 기억 방 화면(이동)과 조향실 화면(목표 선택) 양쪽
    // 컨트롤러가 이 계산을 각자 다시 구현하지 않도록 공유한다 — 두 화면 모두
    // "지금 그래프가 어떻게 생겼고, 어디가 복원/방문됐는지"를 지도 데이터로
    // 바꾸는 규칙은 완전히 같고, 다른 것은 그 데이터를 "어떻게 얻은 클릭에
    // 무엇을 할지"뿐이기 때문이다(그 부분은 각 컨트롤러가 따로 정한다).
    public static class MemoryMapDataBuilder
    {
        public static IReadOnlyList<MemoryMapNodeData> BuildNodes(
            IMemoryRoomGraph graph,
            IMemoryRoomRestorationTracker restorationTracker,
            MemoryRoomMovementProcessor movementProcessor,
            MemoryGraphNodeId current,
            MemoryRoomId? selectedRoomId,
            MemoryGraphNodeId memoryEntryNodeId)
        {
            var nodes = graph.Nodes;
            var result = new List<MemoryMapNodeData>(nodes.Count);

            foreach (var node in nodes)
            {
                var isCurrent = node.Id.Equals(current);
                var isRestored = false;
                var isVisited = true; // 허브는 언제나 이미 아는 곳으로 취급한다.
                var isSelected = false;

                if (node.Type == MemoryGraphNodeType.MemoryRoom)
                {
                    var roomId = new MemoryRoomId(node.Id.Value);
                    isRestored = restorationTracker.IsRestored(roomId);
                    isVisited = movementProcessor.HasVisited(roomId);
                    isSelected = selectedRoomId.HasValue && roomId.Equals(selectedRoomId.Value);
                }

                result.Add(new MemoryMapNodeData(
                    node.Id, node.Type, node.Coordinate,
                    isCurrent: isCurrent, isRestored: isRestored, isVisited: isVisited, isSelected: isSelected,
                    isMemoryExit: node.Id.Equals(memoryEntryNodeId)));
            }

            return result;
        }

        public static IReadOnlyList<MemoryMapConnectionData> BuildConnections(
            IMemoryRoomGraph graph, IMemoryRoomRestorationTracker restorationTracker)
        {
            var result = new List<MemoryMapConnectionData>(graph.OpenConnections.Count + graph.LadderConnections.Count);

            foreach (var connection in graph.OpenConnections)
            {
                if (!graph.TryGetNode(connection.NodeA, out var nodeA) ||
                    !graph.TryGetNode(connection.NodeB, out var nodeB))
                    continue;

                result.Add(new MemoryMapConnectionData(nodeA.Coordinate, nodeB.Coordinate, isLadder: false, isLocked: false));
            }

            foreach (var ladder in graph.LadderConnections)
            {
                var upperId = MemoryGraphNodeId.OfRoom(ladder.UpperRoom);
                var lowerId = MemoryGraphNodeId.OfRoom(ladder.LowerRoom);

                if (!graph.TryGetNode(upperId, out var upperNode) || !graph.TryGetNode(lowerId, out var lowerNode))
                    continue;

                var isLocked = !restorationTracker.IsRestored(ladder.LowerRoom);
                result.Add(new MemoryMapConnectionData(
                    upperNode.Coordinate, lowerNode.Coordinate, isLadder: true, isLocked: isLocked));
            }

            return result;
        }
    }
}
