using System;
using System.Collections.Generic;

namespace GameName.Core.MemoryRooms
{
    // 기억 방 그래프의 순수 구조 데이터.
    // 어떤 노드(기억 방/허브)가 있고 서로 어떻게 연결되는지만 담으며, 복원 여부
    // 같은 실행 중 상태는 갖지 않는다 — 그건 IMemoryRoomRestorationTracker의
    // 몫이다. 노드/연결 목록은 전부 생성자로 주입받는다. 방 배치를 코드에
    // 상수로 박아두지 않기 위함이다.
    //
    // IMemoryRoomGraph(읽기)와 IMemoryRoomGraphLoader(통째로 교체) 두 경계를
    // 함께 구현한다. 이동 처리기 등 여러 처리기가 이 객체 참조를 생성자로
    // 받아 그대로 들고 있으므로, 의뢰가 바뀌어도 이 객체 자체를 새로 만들지
    // 않고 내부 내용만 Load()로 바꿔치기한다 — 그래야 참조를 들고 있는 모든
    // 처리기를 다시 만들 필요가 없다.
    public sealed class MemoryRoomGraph : IMemoryRoomGraph, IMemoryRoomGraphLoader
    {
        private readonly Dictionary<MemoryGraphNodeId, MemoryGraphNode> _nodesById =
            new Dictionary<MemoryGraphNodeId, MemoryGraphNode>();
        private readonly HashSet<(MemoryGraphNodeId, MemoryGraphNodeId)> _openConnections =
            new HashSet<(MemoryGraphNodeId, MemoryGraphNodeId)>();
        private readonly List<LadderConnection> _ladderConnections = new List<LadderConnection>();

        // 지도가 그리는 데 필요한 "원본 그대로의" 목록 — 위 _openConnections는
        // AreOpenlyConnected를 O(1)로 검사하려고 양방향을 전부 저장해 두므로,
        // 문 하나당 두 번씩 그려지는 것을 막기 위해 원본 개수 그대로인 목록을
        // 따로 둔다.
        private readonly List<MemoryGraphNode> _nodeList = new List<MemoryGraphNode>();
        private readonly List<OpenConnection> _canonicalOpenConnections = new List<OpenConnection>();

        public IReadOnlyList<MemoryGraphNode> Nodes => _nodeList;
        public IReadOnlyList<OpenConnection> OpenConnections => _canonicalOpenConnections;
        public IReadOnlyList<LadderConnection> LadderConnections => _ladderConnections;

        public MemoryRoomGraph(
            IReadOnlyList<MemoryGraphNode> nodes,
            IReadOnlyList<OpenConnection> openConnections,
            IReadOnlyList<LadderConnection> ladderConnections)
        {
            Load(nodes, openConnections, ladderConnections);
        }

        // 지금까지의 노드/연결을 전부 버리고 새 구조로 대체한다. 새 의뢰가
        // 시작될 때 GameSession.LoadCommission이 호출한다.
        public void Load(
            IReadOnlyList<MemoryGraphNode> nodes,
            IReadOnlyList<OpenConnection> openConnections,
            IReadOnlyList<LadderConnection> ladderConnections)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (openConnections == null) throw new ArgumentNullException(nameof(openConnections));
            if (ladderConnections == null) throw new ArgumentNullException(nameof(ladderConnections));

            _nodesById.Clear();
            _openConnections.Clear();
            _ladderConnections.Clear();
            _nodeList.Clear();
            _canonicalOpenConnections.Clear();

            foreach (var node in nodes)
            {
                if (_nodesById.ContainsKey(node.Id))
                    throw new ArgumentException($"노드 식별자가 중복되었다({node.Id}).", nameof(nodes));

                _nodesById.Add(node.Id, node);
                _nodeList.Add(node);
            }

            foreach (var connection in openConnections)
            {
                RequireKnownNode(connection.NodeA);
                RequireKnownNode(connection.NodeB);
                _openConnections.Add((connection.NodeA, connection.NodeB));
                _openConnections.Add((connection.NodeB, connection.NodeA));
                _canonicalOpenConnections.Add(connection);
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

        // 특정 노드와 구조적으로 인접한(가로 연결 또는 사다리로 이어진) 모든
        // 노드 id를 돌려준다. 잠김 여부는 여기서 판단하지 않는다 — 그건 복원
        // 상태(IMemoryRoomRestorationTracker)까지 함께 봐야 하는 별개의 질문이라,
        // 호출부가 이 목록을 얻은 뒤 TryGetLadderLowerRoom + IsRestored를 다시
        // 물어 판단한다. 화면이 인접 노드를 스스로 다시 구성(전수 조사 등)하지
        // 않고 그래프에 직접 물어보게 하려고 추가한 조회 전용 메서드다.
        public IReadOnlyList<MemoryGraphNodeId> GetNeighborIds(MemoryGraphNodeId nodeId)
        {
            RequireKnownNode(nodeId);

            var neighbors = new List<MemoryGraphNodeId>();

            foreach (var pair in _openConnections)
            {
                if (pair.Item1.Equals(nodeId))
                    neighbors.Add(pair.Item2);
            }

            foreach (var ladder in _ladderConnections)
            {
                var upperNodeId = MemoryGraphNodeId.OfRoom(ladder.UpperRoom);
                var lowerNodeId = MemoryGraphNodeId.OfRoom(ladder.LowerRoom);

                if (nodeId.Equals(upperNodeId))
                    neighbors.Add(lowerNodeId);
                else if (nodeId.Equals(lowerNodeId))
                    neighbors.Add(upperNodeId);
            }

            return neighbors;
        }

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
