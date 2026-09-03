using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.Memories;

namespace GameName.Core.Restoration
{
    // 색(R/G/B)마다 하나씩 있는 복원도를 전부 들고 있는 저장소.
    //
    // 이 타입은 런 전체에 걸쳐 산다 — 방이 바뀌어도 리셋되지 않는다. 그래서
    // RoomStartedEvent를 구독하지 않는다(지갑·추출 자원과 같은 스코프다).
    //
    // 자동 생성(뿌리·단서 노드, 그 사이 잠긴 간선)은 추출을 듣는
    // RestorationBoardExtractionListener만 부른다. 플레이어 편집은 화면이 이
    // 표면을 직접 부른다. 판정이 거의 없는 단순 CRUD라 그 사이에 처리기를 두지
    // 않았다.
    //
    // 노드 식별자는 도메인에서 유도한다(뿌리는 색, 단서 노드는 ClueId). 그래서
    // 같은 색·같은 단서가 두 번 들어와도 자연히 하나로 모인다 — 멱등성이 별도
    // 검사가 아니라 식별자 규칙에서 나온다.
    public sealed class RestorationBoard : IRestorationBoardReader, IRestorationBoardMutator
    {
        private readonly IEventBus _eventBus;

        private readonly Dictionary<RestorationNodeId, RestorationNode> _nodes =
            new Dictionary<RestorationNodeId, RestorationNode>();
        private readonly Dictionary<RestorationEdgeId, RestorationEdge> _edges =
            new Dictionary<RestorationEdgeId, RestorationEdge>();

        // 생성 순서를 지키기 위해 식별자만 따로 이어 둔다. 이동·이름 변경은
        // 사전만 갈아 끼우므로 이 순서는 건드리지 않는다.
        private readonly List<RestorationNodeId> _nodeOrder = new List<RestorationNodeId>();
        private readonly List<RestorationEdgeId> _edgeOrder = new List<RestorationEdgeId>();

        private int _playerNodeSeq;
        private int _playerEdgeSeq;

        public RestorationBoard(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        // ── 읽기 ──────────────────────────────────────────────────────────

        public bool HasColorRoot(MemoryColor color) => _nodes.ContainsKey(ColorRootId(color));

        public bool TryGetColorRoot(MemoryColor color, out RestorationNode root) =>
            _nodes.TryGetValue(ColorRootId(color), out root);

        public IReadOnlyList<RestorationNode> NodesOf(MemoryColor color)
        {
            var result = new List<RestorationNode>();
            foreach (var id in _nodeOrder)
            {
                var node = _nodes[id];
                if (node.Color == color)
                    result.Add(node);
            }

            return result;
        }

        public IReadOnlyList<RestorationEdge> EdgesOf(MemoryColor color)
        {
            var result = new List<RestorationEdge>();
            foreach (var id in _edgeOrder)
            {
                var edge = _edges[id];
                if (edge.Color == color)
                    result.Add(edge);
            }

            return result;
        }

        public bool TryGetNode(RestorationNodeId nodeId, out RestorationNode node) =>
            _nodes.TryGetValue(nodeId, out node);

        public bool TryGetEdge(RestorationEdgeId edgeId, out RestorationEdge edge) =>
            _edges.TryGetValue(edgeId, out edge);

        // ── 자동 생성 ─────────────────────────────────────────────────────

        public RestorationNode EnsureColorRoot(MemoryColor color)
        {
            var rootId = ColorRootId(color);
            if (_nodes.TryGetValue(rootId, out var existing))
                return existing;

            // 뿌리 라벨은 Core가 정하지 않는다 — 색 이름은 번역 대상이고 이미
            // 화면 쪽(MemoryColorLabelAsset)이 갖고 있다. 화면이 Node.Color로
            // 그 표기를 붙인다.
            var root = new RestorationNode(
                rootId, color, RestorationNodeKind.ColorRoot,
                label: string.Empty, position: default, sourceClue: null);

            AddNode(root);
            return root;
        }

        public RestorationNode AddClueNode(MemoryColor color, ClueId clueId, string displayName)
        {
            var root = EnsureColorRoot(color);

            var nodeId = ClueNodeId(clueId);
            if (_nodes.TryGetValue(nodeId, out var existing))
                return existing;

            var node = new RestorationNode(
                nodeId, color, RestorationNodeKind.ClueLinked,
                label: displayName ?? string.Empty, position: default, sourceClue: clueId);
            AddNode(node);

            var edge = new RestorationEdge(
                ClueLinkEdgeId(clueId), root.Id, node.Id, color, isLocked: true);
            AddEdge(edge);

            return node;
        }

        // ── 플레이어 편집 ─────────────────────────────────────────────────

        public RestorationNodeResult AddPlayerNode(MemoryColor color, string label, BoardPosition position)
        {
            if (!HasColorRoot(color))
                return RestorationNodeResult.Failure(RestorationEditFailureReason.ColorNotStarted);

            var node = new RestorationNode(
                new RestorationNodeId($"player-node-{++_playerNodeSeq}"),
                color, RestorationNodeKind.PlayerAuthored,
                label: label ?? string.Empty, position: position, sourceClue: null);

            AddNode(node);
            return RestorationNodeResult.Success(node);
        }

        public RestorationEditResult RenamePlayerNode(RestorationNodeId nodeId, string newLabel)
        {
            if (!_nodes.TryGetValue(nodeId, out var node))
                return RestorationEditResult.Failure(RestorationEditFailureReason.NodeNotFound);

            if (node.Kind != RestorationNodeKind.PlayerAuthored)
                return RestorationEditResult.Failure(RestorationEditFailureReason.NodeNotEditable);

            var renamed = node.WithLabel(newLabel);
            _nodes[nodeId] = renamed;
            _eventBus.Publish(new RestorationNodeRenamedEvent(nodeId, renamed.Label));
            return RestorationEditResult.Success();
        }

        public RestorationEditResult MoveNode(RestorationNodeId nodeId, BoardPosition position)
        {
            if (!_nodes.TryGetValue(nodeId, out var node))
                return RestorationEditResult.Failure(RestorationEditFailureReason.NodeNotFound);

            _nodes[nodeId] = node.WithPosition(position);
            _eventBus.Publish(new RestorationNodeMovedEvent(nodeId, position));
            return RestorationEditResult.Success();
        }

        public RestorationEdgeResult AddPlayerEdge(RestorationNodeId fromNodeId, RestorationNodeId toNodeId)
        {
            if (fromNodeId == toNodeId)
                return RestorationEdgeResult.Failure(RestorationEditFailureReason.SelfLoop);

            if (!_nodes.TryGetValue(fromNodeId, out var from) || !_nodes.TryGetValue(toNodeId, out var to))
                return RestorationEdgeResult.Failure(RestorationEditFailureReason.NodeNotFound);

            if (from.Color != to.Color)
                return RestorationEdgeResult.Failure(RestorationEditFailureReason.ColorMismatch);

            var edge = new RestorationEdge(
                new RestorationEdgeId($"player-edge-{++_playerEdgeSeq}"),
                fromNodeId, toNodeId, from.Color, isLocked: false);

            AddEdge(edge);
            return RestorationEdgeResult.Success(edge);
        }

        public RestorationEditResult RemovePlayerEdge(RestorationEdgeId edgeId)
        {
            if (!_edges.TryGetValue(edgeId, out var edge))
                return RestorationEditResult.Failure(RestorationEditFailureReason.EdgeNotFound);

            if (edge.IsLocked)
                return RestorationEditResult.Failure(RestorationEditFailureReason.EdgeLocked);

            RemoveEdge(edge);
            return RestorationEditResult.Success();
        }

        public RestorationEditResult RemovePlayerNode(RestorationNodeId nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out var node))
                return RestorationEditResult.Failure(RestorationEditFailureReason.NodeNotFound);

            if (node.Kind != RestorationNodeKind.PlayerAuthored)
                return RestorationEditResult.Failure(RestorationEditFailureReason.NodeNotEditable);

            // 이 노드에 닿은 간선을 먼저 걷어낸다. 잠긴 간선은 뿌리 ↔ 단서
            // 노드만 잇지 플레이어 노드에는 붙지 않으므로 여기 걸리는 것은 전부
            // 플레이어 간선이다.
            var touching = new List<RestorationEdge>();
            foreach (var id in _edgeOrder)
            {
                var edge = _edges[id];
                if (edge.Touches(nodeId))
                    touching.Add(edge);
            }

            foreach (var edge in touching)
                RemoveEdge(edge);

            _nodes.Remove(nodeId);
            _nodeOrder.Remove(nodeId);
            _eventBus.Publish(new RestorationNodeRemovedEvent(nodeId, node.Color));
            return RestorationEditResult.Success();
        }

        // ── 내부 ──────────────────────────────────────────────────────────

        private void AddNode(RestorationNode node)
        {
            _nodes.Add(node.Id, node);
            _nodeOrder.Add(node.Id);
            _eventBus.Publish(new RestorationNodeAddedEvent(node));
        }

        private void AddEdge(RestorationEdge edge)
        {
            _edges.Add(edge.Id, edge);
            _edgeOrder.Add(edge.Id);
            _eventBus.Publish(new RestorationEdgeAddedEvent(edge));
        }

        private void RemoveEdge(RestorationEdge edge)
        {
            _edges.Remove(edge.Id);
            _edgeOrder.Remove(edge.Id);
            _eventBus.Publish(new RestorationEdgeRemovedEvent(edge.Id, edge.Color));
        }

        private static RestorationNodeId ColorRootId(MemoryColor color) =>
            new RestorationNodeId($"root-{ColorSlug(color)}");

        private static RestorationNodeId ClueNodeId(ClueId clueId) =>
            new RestorationNodeId($"clue-{clueId.Value}");

        private static RestorationEdgeId ClueLinkEdgeId(ClueId clueId) =>
            new RestorationEdgeId($"link-{clueId.Value}");

        private static string ColorSlug(MemoryColor color)
        {
            switch (color)
            {
                case MemoryColor.Red: return "red";
                case MemoryColor.Green: return "green";
                case MemoryColor.Blue: return "blue";
                default: throw new ArgumentOutOfRangeException(nameof(color), color, null);
            }
        }
    }
}
