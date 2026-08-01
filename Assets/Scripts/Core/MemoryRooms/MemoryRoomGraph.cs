using System;
using System.Collections.Generic;

namespace GameName.Core.MemoryRooms
{
    // 기억 방 그래프의 순수 구조 데이터.
    // 어떤 노드(기억 방/허브)가 있고 서로 어떻게 연결되는지만 담으며, 복원 여부
    // 같은 실행 중 상태는 갖지 않는다 — 그건 IMemoryRoomRestorationTracker의
    // 몫이다. 노드/연결 목록은 전부 생성자로 주입받는다. 방 배치를 코드에
    // 상수로 박아두지 않기 위함이다.
    public sealed class MemoryRoomGraph
    {
        private readonly Dictionary<MemoryGraphNodeId, MemoryGraphNode> _nodesById =
            new Dictionary<MemoryGraphNodeId, MemoryGraphNode>();
        private readonly HashSet<(MemoryGraphNodeId, MemoryGraphNodeId)> _openConnections =
            new HashSet<(MemoryGraphNodeId, MemoryGraphNodeId)>();
        private readonly List<LadderConnection> _ladderConnections = new List<LadderConnection>();

        public MemoryRoomGraph(
            IReadOnlyList<MemoryGraphNode> nodes,
            IReadOnlyList<OpenConnection> openConnections,
            IReadOnlyList<LadderConnection> ladderConnections)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (openConnections == null) throw new ArgumentNullException(nameof(openConnections));
            if (ladderConnections == null) throw new ArgumentNullException(nameof(ladderConnections));

            foreach (var node in nodes)
            {
                if (_nodesById.ContainsKey(node.Id))
                    throw new ArgumentException($"노드 식별자가 중복되었다({node.Id}).", nameof(nodes));

                _nodesById.Add(node.Id, node);
            }

            foreach (var connection in openConnections)
            {
                RequireKnownNode(connection.NodeA);
                RequireKnownNode(connection.NodeB);
                _openConnections.Add((connection.NodeA, connection.NodeB));
                _openConnections.Add((connection.NodeB, connection.NodeA));
            }

            foreach (var ladder in ladderConnections)
            {
                RequireKnownNode(MemoryGraphNodeId.OfRoom(ladder.UpperRoom));
                RequireKnownNode(MemoryGraphNodeId.OfRoom(ladder.LowerRoom));
                _ladderConnections.Add(ladder);
            }
        }

        public bool TryGetNode(MemoryGraphNodeId id, out MemoryGraphNode node) =>
            _nodesById.TryGetValue(id, out node);

        public bool AreOpenlyConnected(MemoryGraphNodeId a, MemoryGraphNodeId b) =>
            _openConnections.Contains((a, b));

        public bool TryGetLadderLowerRoom(MemoryGraphNodeId a, MemoryGraphNodeId b, out MemoryRoomId lowerRoomId)
        {
            foreach (var ladder in _ladderConnections)
            {
                var upperNodeId = MemoryGraphNodeId.OfRoom(ladder.UpperRoom);
                var lowerNodeId = MemoryGraphNodeId.OfRoom(ladder.LowerRoom);
                var matchesEitherDirection =
                    (a.Equals(upperNodeId) && b.Equals(lowerNodeId)) ||
                    (a.Equals(lowerNodeId) && b.Equals(upperNodeId));

                if (matchesEitherDirection)
                {
                    lowerRoomId = ladder.LowerRoom;
                    return true;
                }
            }

            lowerRoomId = default;
            return false;
        }

        private void RequireKnownNode(MemoryGraphNodeId id)
        {
            if (!_nodesById.ContainsKey(id))
                throw new ArgumentException($"그래프에 등록되지 않은 노드({id})를 연결에서 참조했다.");
        }
    }
}
