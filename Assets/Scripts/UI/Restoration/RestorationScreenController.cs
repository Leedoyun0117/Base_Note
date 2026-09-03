using System;
using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.Memories;
using GameName.Core.Restoration;

namespace GameName.UI.Restoration
{
    // 복원도 캔버스의 Core 연동.
    //
    // Core(RestorationBoard)는 이미 완성돼 있다 — 이 컨트롤러는 6종 이벤트를
    // 구독해 캔버스를 갱신하고, 캔버스에서 온 플레이어 행동을 9개 mutator
    // 메서드로 넘긴다. 새 규칙은 하나도 계산하지 않는다.
    //
    // 색 뿌리 노드의 라벨은 Core가 비워 둔 자리다(색 이름은 번역 대상이라
    // 화면 쪽 MemoryColorLabelAsset에 있다). 여기서 색 → 이름 함수로 채운다.
    //
    // 자동 노드(뿌리·단서)는 Core가 위치를 원점으로 만든다. 처음 등장할 때
    // 겹치지 않는 자리를 계산해 MoveNode로 한 번 적어 넣는다 — 그 뒤로는
    // 플레이어 드래그가 진실이다.
    public sealed class RestorationScreenController : IDisposable
    {
        private static readonly MemoryColor[] AllColors =
            { MemoryColor.Red, MemoryColor.Green, MemoryColor.Blue };

        private readonly IRestorationCanvasView _view;
        private readonly IRestorationBoardReader _reader;
        private readonly IRestorationBoardMutator _editor;
        private readonly Func<MemoryColor, string> _colorLabel;
        private readonly IDisposable[] _subscriptions;

        public RestorationScreenController(
            IRestorationCanvasView view,
            IRestorationBoardReader reader,
            IRestorationBoardMutator editor,
            Func<MemoryColor, string> colorLabel,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _colorLabel = colorLabel ?? throw new ArgumentNullException(nameof(colorLabel));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.NodeDragCommitted += OnNodeDragCommitted;
            _view.NodeRenameCommitted += OnNodeRenameCommitted;
            _view.PlayerNodeRequested += OnPlayerNodeRequested;
            _view.EdgeRequested += OnEdgeRequested;
            _view.PlayerNodeDeleteRequested += OnPlayerNodeDeleteRequested;
            _view.PlayerEdgeDeleteRequested += OnPlayerEdgeDeleteRequested;

            _subscriptions = new[]
            {
                eventBus.Subscribe<RestorationNodeAddedEvent>(OnNodeAdded),
                eventBus.Subscribe<RestorationNodeMovedEvent>(e => _view.UpdateNodePosition(e.NodeId, e.Position)),
                eventBus.Subscribe<RestorationNodeRenamedEvent>(e => _view.UpdateNodeLabel(e.NodeId, e.Label)),
                eventBus.Subscribe<RestorationNodeRemovedEvent>(e => _view.RemoveNode(e.NodeId)),
                eventBus.Subscribe<RestorationEdgeAddedEvent>(e => _view.AddEdge(e.Edge)),
                eventBus.Subscribe<RestorationEdgeRemovedEvent>(e => _view.RemoveEdge(e.EdgeId)),
            };
        }

        // 오버레이가 열리거나 복원도 탭이 선택될 때 전체를 다시 그린다.
        public void Refresh()
        {
            _view.Clear();
            RebuildClusters();

            foreach (var color in AllColors)
            {
                foreach (var node in _reader.NodesOf(color))
                    _view.AddNode(node);

                foreach (var edge in _reader.EdgesOf(color))
                    _view.AddEdge(edge);
            }
        }

        // ── Core 이벤트 → 화면 ────────────────────────────────────────────

        private void OnNodeAdded(RestorationNodeAddedEvent e)
        {
            var node = e.Node;
            _view.AddNode(node);

            if (node.Kind == RestorationNodeKind.ColorRoot)
                RebuildClusters();

            // 자동 노드는 원점으로 들어온다 — 첫 자리를 잡아 Core에 적어 넣는다.
            var autoKind = node.Kind == RestorationNodeKind.ColorRoot
                           || node.Kind == RestorationNodeKind.ClueLinked;
            if (autoKind && IsOrigin(node.Position))
            {
                var index = CountOfKind(node.Color, node.Kind) - 1;
                _editor.MoveNode(node.Id, RestorationAutoLayout.SlotFor(node.Color, node.Kind, index));
            }

            // 방금 추가한 메모가 시야 밖(고정 슬롯)에 생기지 않도록 그 자리로
            // 뷰를 옮긴다. 확대율은 그대로 둔다. Refresh(재진입)는 이 경로를
            // 타지 않으므로 뷰가 제멋대로 튀지 않는다.
            if (node.Kind == RestorationNodeKind.PlayerAuthored)
                _view.FocusNode(node.Id);
        }

        // ── 화면 → Core (9개 mutator) ─────────────────────────────────────

        private void OnNodeDragCommitted(RestorationNodeId nodeId, BoardPosition position) =>
            _editor.MoveNode(nodeId, position);

        private void OnNodeRenameCommitted(RestorationNodeId nodeId, string label)
        {
            var result = _editor.RenamePlayerNode(nodeId, label);
            if (!result.Succeeded)
                _view.ShowNotice(Describe(result.FailureReason));
        }

        private void OnPlayerNodeRequested(MemoryColor color, string label)
        {
            var index = CountOfKind(color, RestorationNodeKind.PlayerAuthored);
            var position = RestorationAutoLayout.SlotFor(color, RestorationNodeKind.PlayerAuthored, index);

            var result = _editor.AddPlayerNode(color, label, position);
            if (!result.Succeeded)
                _view.ShowNotice(Describe(result.FailureReason));
        }

        private void OnEdgeRequested(RestorationNodeId from, RestorationNodeId to)
        {
            var result = _editor.AddPlayerEdge(from, to);
            if (!result.Succeeded)
            {
                _view.ShowNotice(Describe(result.FailureReason));
                _view.ResetConnectSelection();
            }
        }

        private void OnPlayerNodeDeleteRequested(RestorationNodeId nodeId)
        {
            var result = _editor.RemovePlayerNode(nodeId);
            if (!result.Succeeded)
                _view.ShowNotice(Describe(result.FailureReason));
        }

        private void OnPlayerEdgeDeleteRequested(RestorationEdgeId edgeId)
        {
            var result = _editor.RemovePlayerEdge(edgeId);
            if (!result.Succeeded)
                _view.ShowNotice(Describe(result.FailureReason));
        }

        // ── 보조 ──────────────────────────────────────────────────────────

        private void RebuildClusters()
        {
            var clusters = new List<RestorationClusterInfo>();
            foreach (var color in AllColors)
            {
                if (_reader.HasColorRoot(color))
                    clusters.Add(new RestorationClusterInfo(color, _colorLabel(color)));
            }

            _view.SetColorClusters(clusters);
        }

        private int CountOfKind(MemoryColor color, RestorationNodeKind kind)
        {
            var count = 0;
            foreach (var node in _reader.NodesOf(color))
            {
                if (node.Kind == kind)
                    count++;
            }

            return count;
        }

        private static bool IsOrigin(BoardPosition position) =>
            position.X == 0f && position.Y == 0f;

        private static string Describe(RestorationEditFailureReason? reason)
        {
            switch (reason)
            {
                case RestorationEditFailureReason.ColorNotStarted:
                    return "아직 이 색으로 추출한 기억이 없습니다.";
                case RestorationEditFailureReason.ColorMismatch:
                    return "다른 색의 노드끼리는 이을 수 없습니다.";
                case RestorationEditFailureReason.SelfLoop:
                    return "한 노드를 자기 자신과 이을 수는 없습니다.";
                case RestorationEditFailureReason.NodeNotEditable:
                    return "자동으로 만들어진 노드는 고칠 수 없습니다.";
                case RestorationEditFailureReason.EdgeLocked:
                    return "자동으로 이어진 연결은 지울 수 없습니다.";
                case RestorationEditFailureReason.NodeNotFound:
                case RestorationEditFailureReason.EdgeNotFound:
                    return "대상을 찾지 못했습니다.";
                default:
                    return string.Empty;
            }
        }

        public void Dispose()
        {
            _view.NodeDragCommitted -= OnNodeDragCommitted;
            _view.NodeRenameCommitted -= OnNodeRenameCommitted;
            _view.PlayerNodeRequested -= OnPlayerNodeRequested;
            _view.EdgeRequested -= OnEdgeRequested;
            _view.PlayerNodeDeleteRequested -= OnPlayerNodeDeleteRequested;
            _view.PlayerEdgeDeleteRequested -= OnPlayerEdgeDeleteRequested;

            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }
    }
}
