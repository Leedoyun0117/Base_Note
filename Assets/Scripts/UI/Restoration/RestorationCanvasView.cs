using System;
using System.Collections.Generic;
using GameName.Core.Memories;
using GameName.Core.Restoration;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.Restoration
{
    // 복원도 캔버스의 UI Toolkit 구현.
    //
    //   · 창(restoration-viewport)은 고정 크기다. 그 안의 넓은 논리 판
    //     (restoration-canvas)을 휠로 확대/축소하고 빈 곳을 끌어 이동한다 —
    //     위·옆으로 긴 스크롤 대신 "멀어지고 가까워지는" 방식.
    //   · 노드는 절대 위치 VisualElement. BoardPosition을 그대로 논리 좌표
    //     style.left/top(px)로. 확대/축소·이동은 노드가 아니라 판에 얹는다.
    //   · 간선은 generateVisualContent + Painter2D로 노드 중심을 잇는 직선.
    //     (Unity 6 / com.unity.modules.uielements 에서 지원 확인됨.)
    //   · 드래그는 포인터 캡처로. 화면은 매 이동마다 갱신하되 Core에는 손을 뗄
    //     때(PointerUp) 최종 위치로 한 번만 알린다. 픽셀 이동량은 현재 배율로
    //     나눠 논리 좌표로 되돌린다.
    //
    // 잠긴 노드/간선(자동 생성)에는 이름 편집·삭제 UI를 아예 만들지 않는다 —
    // Core가 막기도 하지만 화면에서도 막아 이중 방어.
    public sealed class RestorationCanvasView : IRestorationCanvasView
    {
        private static readonly CustomStyleProperty<Color> LockedEdgeColorProp =
            new CustomStyleProperty<Color>("--edge-locked-color");
        private static readonly CustomStyleProperty<Color> PlayerEdgeColorProp =
            new CustomStyleProperty<Color>("--edge-player-color");

        private const float DragThreshold = 3f;
        private const float EdgeHandleSize = 16f;
        private const float MaxZoom = 1.6f;

        private readonly Func<MemoryColor, string> _rootLabeler;

        private readonly VisualElement _viewport;
        private readonly VisualElement _pan;
        private readonly VisualElement _canvas;
        private readonly VisualElement _edgeLayer;
        private readonly VisualElement _nodeLayer;
        private readonly VisualElement _addBar;
        private readonly Button _connectToggle;
        private readonly Label _notice;

        // 창(뷰포트)은 고정 크기다. 휠로 확대/축소(_zoom)하고 빈 곳을 끌어
        // 이동(_panX/_panY)한다. 노드 좌표는 논리값 그대로 두고, 이 변환만
        // restoration-pan/canvas에 얹는다.
        private float _zoom = 1f;
        private float _panX;
        private float _panY;
        private float _minZoom = 0.2f;
        private bool _viewInitialized;

        private bool _panning;
        private Vector2 _panStartPointer;
        private float _panStartX;
        private float _panStartY;

        private readonly Dictionary<RestorationNodeId, NodeElement> _nodes =
            new Dictionary<RestorationNodeId, NodeElement>();
        private readonly Dictionary<RestorationEdgeId, RestorationEdge> _edges =
            new Dictionary<RestorationEdgeId, RestorationEdge>();
        private readonly Dictionary<RestorationEdgeId, VisualElement> _edgeHandles =
            new Dictionary<RestorationEdgeId, VisualElement>();

        private Color _lockedEdgeColor = new Color(0.6f, 0.6f, 0.68f, 0.35f);
        private Color _playerEdgeColor = new Color(0.72f, 0.6f, 0.86f, 1f);

        private bool _connectMode;
        private NodeElement _pendingConnectFrom;

        public event Action<RestorationNodeId, BoardPosition> NodeDragCommitted;
        public event Action<RestorationNodeId, string> NodeRenameCommitted;
        public event Action<MemoryColor, string> PlayerNodeRequested;
        public event Action<RestorationNodeId, RestorationNodeId> EdgeRequested;
        public event Action<RestorationNodeId> PlayerNodeDeleteRequested;
        public event Action<RestorationEdgeId> PlayerEdgeDeleteRequested;

        public RestorationCanvasView(VisualElement overlayRoot, Func<MemoryColor, string> rootLabeler)
        {
            if (overlayRoot == null) throw new ArgumentNullException(nameof(overlayRoot));
            _rootLabeler = rootLabeler ?? throw new ArgumentNullException(nameof(rootLabeler));

            _viewport = overlayRoot.Q<VisualElement>("restoration-viewport");
            _pan = overlayRoot.Q<VisualElement>("restoration-pan");
            _canvas = overlayRoot.Q<VisualElement>("restoration-canvas");
            _edgeLayer = overlayRoot.Q<VisualElement>("restoration-edges");
            _nodeLayer = overlayRoot.Q<VisualElement>("restoration-nodes");
            _addBar = overlayRoot.Q<VisualElement>("restoration-addbar");
            _connectToggle = overlayRoot.Q<Button>("restoration-connect-toggle");
            _notice = overlayRoot.Q<Label>("restoration-notice");

            _canvas.style.width = RestorationAutoLayout.CanvasWidth;
            _canvas.style.height = RestorationAutoLayout.CanvasHeight;

            _edgeLayer.generateVisualContent += OnGenerateEdges;
            _edgeLayer.RegisterCallback<CustomStyleResolvedEvent>(OnEdgeStyleResolved);

            _connectToggle.clicked += ToggleConnectMode;
            overlayRoot.Q<Button>("restoration-zoom-in").clicked += () => ZoomAtViewportCenter(1.2f);
            overlayRoot.Q<Button>("restoration-zoom-out").clicked += () => ZoomAtViewportCenter(1f / 1.2f);
            overlayRoot.Q<Button>("restoration-zoom-reset").clicked += FitToViewport;

            // 창 안에서 휠 = 확대/축소, 빈 곳 끌기 = 이동.
            _viewport.RegisterCallback<WheelEvent>(OnWheel);
            _viewport.RegisterCallback<PointerDownEvent>(OnViewportPointerDown);
            _viewport.RegisterCallback<PointerMoveEvent>(OnViewportPointerMove);
            _viewport.RegisterCallback<PointerUpEvent>(OnViewportPointerUp);
            _viewport.RegisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);

            // 노드의 크기가 확정되거나 바뀌면 간선·핸들을 다시 맞춘다(패널이 처음
            // 보이게 될 때도 여기로 들어온다).
            _nodeLayer.RegisterCallback<GeometryChangedEvent>(_ => RefreshEdges());

            ShowNotice(null);
        }

        // ── 전체 다시 그리기 ──────────────────────────────────────────────

        public void Clear()
        {
            _nodeLayer.Clear();
            _nodes.Clear();
            _edges.Clear();
            _edgeHandles.Clear();
            _pendingConnectFrom = null;
            _edgeLayer.MarkDirtyRepaint();
        }

        public void SetColorClusters(IReadOnlyList<RestorationClusterInfo> clusters)
        {
            _addBar.Clear();
            foreach (var cluster in clusters)
                _addBar.Add(BuildAddMemoRow(cluster));
        }

        // ── 노드 ──────────────────────────────────────────────────────────

        public void AddNode(RestorationNode node)
        {
            if (_nodes.TryGetValue(node.Id, out var existing))
            {
                existing.Bind(node);
                return;
            }

            var element = new NodeElement(this, node);
            _nodes.Add(node.Id, element);
            _nodeLayer.Add(element.Root);
            element.ApplyInverseZoom(_zoom);
            RefreshEdges();
        }

        public void RemoveNode(RestorationNodeId nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out var element))
                return;

            _nodeLayer.Remove(element.Root);
            _nodes.Remove(nodeId);

            if (_pendingConnectFrom == element)
                _pendingConnectFrom = null;

            RefreshEdges();
        }

        public void UpdateNodePosition(RestorationNodeId nodeId, BoardPosition position)
        {
            if (_nodes.TryGetValue(nodeId, out var element))
                element.SetPosition(position);

            RefreshEdges();
        }

        public void UpdateNodeLabel(RestorationNodeId nodeId, string label)
        {
            if (_nodes.TryGetValue(nodeId, out var element))
                element.SetLabel(label);
        }

        // 확대율은 그대로, 그 노드가 창 한가운데 오도록 팬만 옮긴다.
        public void FocusNode(RestorationNodeId nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out var element))
                return;

            var vp = _viewport.contentRect;
            if (float.IsNaN(vp.width) || vp.width <= 0f || vp.height <= 0f)
                return; // 창이 아직 측정되지 않았다 — 팬을 NaN으로 만들지 않는다

            var pan = CenteringPan(vp, element.Center, _zoom);
            _panX = pan.x;
            _panY = pan.y;

            ClampPanAndApply();
        }

        // 노드 중심을 창 중심에 두는 팬 값. 순수 계산이라 따로 검증할 수 있다.
        internal static Vector2 CenteringPan(Rect viewport, Vector2 nodeCenter, float zoom) =>
            new Vector2(
                viewport.width * 0.5f - nodeCenter.x * zoom,
                viewport.height * 0.5f - nodeCenter.y * zoom);

        // ── 테스트 접근 (패널에 붙일 수 없는 EditMode에서 확인용) ─────────
        internal NodeElement NodeElementFor(RestorationNodeId nodeId) =>
            _nodes.TryGetValue(nodeId, out var element) ? element : null;

        internal Vector2 PanForTest => new Vector2(_panX, _panY);
        internal float ZoomForTest => _zoom;

        internal void SetZoomForTest(float zoom)
        {
            _zoom = zoom;
            ApplyTransform();
        }

        // ── 간선 ──────────────────────────────────────────────────────────

        public void AddEdge(RestorationEdge edge)
        {
            _edges[edge.Id] = edge;

            if (!edge.IsLocked && !_edgeHandles.ContainsKey(edge.Id))
            {
                var handle = BuildEdgeHandle(edge.Id);
                _edgeHandles.Add(edge.Id, handle);
                _nodeLayer.Add(handle);
            }

            RefreshEdges();
        }

        public void RemoveEdge(RestorationEdgeId edgeId)
        {
            _edges.Remove(edgeId);

            if (_edgeHandles.TryGetValue(edgeId, out var handle))
            {
                _nodeLayer.Remove(handle);
                _edgeHandles.Remove(edgeId);
            }

            RefreshEdges();
        }

        // ── 안내 · 연결 모드 ──────────────────────────────────────────────

        public void ShowNotice(string message)
        {
            _notice.text = message ?? string.Empty;
            _notice.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void ResetConnectSelection()
        {
            _pendingConnectFrom?.SetConnectHighlight(false);
            _pendingConnectFrom = null;
        }

        private void ToggleConnectMode()
        {
            _connectMode = !_connectMode;
            _connectToggle.EnableInClassList("restoration-toolbar__toggle--active", _connectMode);
            ResetConnectSelection();
            ShowNotice(_connectMode ? "이을 노드를 차례로 두 개 고르세요." : null);
        }

        // 이벤트는 선언한 타입 안에서만 발행할 수 있으므로, 중첩된 NodeElement가
        // 쓸 수 있게 발행 지점을 여기로 모은다.
        private void RaiseNodeDragCommitted(RestorationNodeId id, BoardPosition position) =>
            NodeDragCommitted?.Invoke(id, position);

        private void RaiseNodeRenameCommitted(RestorationNodeId id, string label) =>
            NodeRenameCommitted?.Invoke(id, label);

        private void RaisePlayerNodeDeleteRequested(RestorationNodeId id) =>
            PlayerNodeDeleteRequested?.Invoke(id);

        // NodeElement가 부른다.
        private void OnNodeClicked(NodeElement node)
        {
            if (!_connectMode)
                return;

            if (_pendingConnectFrom == null)
            {
                _pendingConnectFrom = node;
                node.SetConnectHighlight(true);
                ShowNotice("이제 이을 상대 노드를 고르세요.");
                return;
            }

            if (_pendingConnectFrom == node)
            {
                ResetConnectSelection();
                ShowNotice("이을 노드를 차례로 두 개 고르세요.");
                return;
            }

            var from = _pendingConnectFrom.Model.Id;
            var to = node.Model.Id;
            ResetConnectSelection();
            ShowNotice(null);
            EdgeRequested?.Invoke(from, to);
        }

        private bool IsConnectMode => _connectMode;

        // ── 간선 그리기 ───────────────────────────────────────────────────

        private void RefreshEdges()
        {
            foreach (var pair in _edgeHandles)
            {
                if (!_edges.TryGetValue(pair.Key, out var edge))
                    continue;
                if (!_nodes.TryGetValue(edge.From, out var a) || !_nodes.TryGetValue(edge.To, out var b))
                {
                    pair.Value.style.display = DisplayStyle.None;
                    continue;
                }

                var mid = (a.Center + b.Center) * 0.5f;
                pair.Value.style.display = DisplayStyle.Flex;
                pair.Value.style.left = mid.x - EdgeHandleSize / 2f;
                pair.Value.style.top = mid.y - EdgeHandleSize / 2f;
            }

            _edgeLayer.MarkDirtyRepaint();
        }

        private void OnGenerateEdges(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;

            foreach (var edge in _edges.Values)
            {
                if (!_nodes.TryGetValue(edge.From, out var a) || !_nodes.TryGetValue(edge.To, out var b))
                    continue;

                painter.lineWidth = edge.IsLocked ? 1.5f : 3f;
                painter.strokeColor = edge.IsLocked ? _lockedEdgeColor : _playerEdgeColor;
                painter.BeginPath();
                painter.MoveTo(a.Center);
                painter.LineTo(b.Center);
                painter.Stroke();
            }
        }

        private void OnEdgeStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(LockedEdgeColorProp, out var locked))
                _lockedEdgeColor = locked;
            if (evt.customStyle.TryGetValue(PlayerEdgeColorProp, out var player))
                _playerEdgeColor = player;

            _edgeLayer.MarkDirtyRepaint();
        }

        // ── 확대/축소 · 이동 ─────────────────────────────────────────────

        // 노드 드래그가 화면 픽셀 이동량을 논리 좌표로 되돌릴 때 쓴다.
        private float Zoom => _zoom;

        private void OnViewportGeometryChanged(GeometryChangedEvent evt)
        {
            if (!_viewInitialized && evt.newRect.width > 1f && evt.newRect.height > 1f)
                FitToViewport();
            else
                ClampPanAndApply();
        }

        // 캔버스 전체가 창에 들어오도록 축소하고 가운데로 놓는다.
        private void FitToViewport()
        {
            var vp = _viewport.contentRect;
            if (vp.width < 1f || vp.height < 1f)
                return;

            var fit = Mathf.Min(
                vp.width / RestorationAutoLayout.CanvasWidth,
                vp.height / RestorationAutoLayout.CanvasHeight);

            _minZoom = fit * 0.8f;
            _zoom = Mathf.Clamp(fit, _minZoom, MaxZoom);
            _panX = (vp.width - RestorationAutoLayout.CanvasWidth * _zoom) * 0.5f;
            _panY = (vp.height - RestorationAutoLayout.CanvasHeight * _zoom) * 0.5f;
            _viewInitialized = true;

            ApplyTransform();
        }

        private void OnWheel(WheelEvent evt)
        {
            ZoomAt(evt.localMousePosition, Mathf.Exp(-evt.delta.y * 0.15f));
            evt.StopPropagation();
        }

        private void ZoomAtViewportCenter(float factor)
        {
            var vp = _viewport.contentRect;
            ZoomAt(new Vector2(vp.width * 0.5f, vp.height * 0.5f), factor);
        }

        // pivot(창 안 좌표)을 고정한 채 배율을 factor배 한다.
        private void ZoomAt(Vector2 pivot, float factor)
        {
            var old = _zoom;
            _zoom = Mathf.Clamp(old * factor, _minZoom, MaxZoom);

            var ratio = _zoom / old;
            _panX = pivot.x - (pivot.x - _panX) * ratio;
            _panY = pivot.y - (pivot.y - _panY) * ratio;

            ClampPanAndApply();
        }

        private void OnViewportPointerDown(PointerDownEvent evt)
        {
            // 노드가 아니라 빈 배경을 눌렀을 때만 이동을 시작한다.
            if (evt.target != _viewport && evt.target != _pan && evt.target != _canvas)
                return;

            _panning = true;
            _panStartPointer = evt.position;
            _panStartX = _panX;
            _panStartY = _panY;
            _viewport.CapturePointer(evt.pointerId);
        }

        private void OnViewportPointerMove(PointerMoveEvent evt)
        {
            if (!_panning || !_viewport.HasPointerCapture(evt.pointerId))
                return;

            var delta = (Vector2)evt.position - _panStartPointer;
            _panX = _panStartX + delta.x;
            _panY = _panStartY + delta.y;
            ClampPanAndApply();
        }

        private void OnViewportPointerUp(PointerUpEvent evt)
        {
            if (!_panning)
                return;

            _panning = false;
            if (_viewport.HasPointerCapture(evt.pointerId))
                _viewport.ReleasePointer(evt.pointerId);
        }

        // 캔버스가 창 밖으로 완전히 사라지지 않게 이동 범위를 묶는다.
        private void ClampPanAndApply()
        {
            var vp = _viewport.contentRect;
            if (vp.width >= 1f && vp.height >= 1f)
            {
                var cw = RestorationAutoLayout.CanvasWidth * _zoom;
                var ch = RestorationAutoLayout.CanvasHeight * _zoom;
                _panX = Mathf.Clamp(_panX, vp.width * 0.15f - cw, vp.width * 0.85f);
                _panY = Mathf.Clamp(_panY, vp.height * 0.15f - ch, vp.height * 0.85f);
            }

            ApplyTransform();
        }

        private void ApplyTransform()
        {
            _pan.style.left = _panX;
            _pan.style.top = _panY;
            _canvas.style.scale = new Scale(new Vector2(_zoom, _zoom));

            // 노드는 판과 함께 축소되지만 삭제(×) 버튼은 화면상 크기를 유지해야
            // 낮은 확대율에서도 누를 수 있다 — 반대 배율로 되돌린다.
            foreach (var element in _nodes.Values)
                element.ApplyInverseZoom(_zoom);

            _edgeLayer.MarkDirtyRepaint();
        }

        // ── 요소 만들기 ───────────────────────────────────────────────────

        private VisualElement BuildAddMemoRow(RestorationClusterInfo cluster)
        {
            var row = new VisualElement();
            row.AddToClassList("restoration-addbar__row");

            var swatch = new VisualElement();
            swatch.AddToClassList("restoration-addbar__swatch");
            swatch.AddToClassList(ColorClass("restoration-addbar__swatch", cluster.Color));
            row.Add(swatch);

            var name = new Label(cluster.Label);
            name.AddToClassList("restoration-addbar__name");
            row.Add(name);

            var field = new TextField { isDelayed = false };
            field.AddToClassList("restoration-addbar__field");
            row.Add(field);

            var button = new Button { text = "+메모" };
            button.AddToClassList("restoration-addbar__button");
            button.clicked += () =>
            {
                var text = field.value?.Trim();
                if (string.IsNullOrEmpty(text))
                    return;

                field.value = string.Empty;
                PlayerNodeRequested?.Invoke(cluster.Color, text);
            };
            row.Add(button);

            return row;
        }

        private VisualElement BuildEdgeHandle(RestorationEdgeId edgeId)
        {
            var handle = new VisualElement();
            handle.AddToClassList("restoration-edge-handle");
            handle.style.width = EdgeHandleSize;
            handle.style.height = EdgeHandleSize;

            handle.RegisterCallback<ClickEvent>(_ => ConfirmEdgeDelete(edgeId, handle));
            return handle;
        }

        private void ConfirmEdgeDelete(RestorationEdgeId edgeId, VisualElement anchor)
        {
            var popup = BuildConfirmPopup(
                "이 연결을 지울까요?",
                () => PlayerEdgeDeleteRequested?.Invoke(edgeId));
            anchor.Add(popup);
        }

        // NodeElement가 자기 삭제 확인을 띄울 때 쓴다.
        private VisualElement BuildConfirmPopup(string message, Action onConfirm)
        {
            var popup = new VisualElement();
            popup.AddToClassList("restoration-confirm");

            var label = new Label(message);
            label.AddToClassList("restoration-confirm__text");
            popup.Add(label);

            var actions = new VisualElement();
            actions.AddToClassList("restoration-confirm__actions");

            var yes = new Button { text = "삭제" };
            yes.AddToClassList("restoration-confirm__yes");
            yes.clicked += () =>
            {
                popup.RemoveFromHierarchy();
                onConfirm();
            };

            var no = new Button { text = "취소" };
            no.AddToClassList("restoration-confirm__no");
            no.clicked += () => popup.RemoveFromHierarchy();

            actions.Add(yes);
            actions.Add(no);
            popup.Add(actions);
            return popup;
        }

        private static string ColorClass(string prefix, MemoryColor color)
        {
            switch (color)
            {
                case MemoryColor.Red: return prefix + "--r";
                case MemoryColor.Green: return prefix + "--g";
                default: return prefix + "--b";
            }
        }

        // ── 노드 요소 ─────────────────────────────────────────────────────

        internal sealed class NodeElement
        {
            // 크기가 아직 잡히지 않았을 때(레이아웃 전) 중심 계산에 쓰는 어림값.
            private static readonly Vector2 FallbackSize = new Vector2(120f, 40f);

            private readonly RestorationCanvasView _owner;
            private readonly Label _label;
            private readonly Button _deleteButton; // PlayerAuthored에만 있다

            private Vector2 _size;
            private bool _dragging;
            private bool _pointerMoved;
            private Vector2 _dragStartPointer;
            private float _dragStartLeft;
            private float _dragStartTop;

            private TextField _editField;
            private VisualElement _confirmPopup;
            private Action _confirmDelete;

            public VisualElement Root { get; }
            public RestorationNode Model { get; private set; }

            public float Left { get; private set; }
            public float Top { get; private set; }

            public Vector2 Center
            {
                get
                {
                    var w = _size.x > 0f ? _size.x : FallbackSize.x;
                    var h = _size.y > 0f ? _size.y : FallbackSize.y;
                    return new Vector2(Left + w / 2f, Top + h / 2f);
                }
            }

            // 테스트가 × 버튼을 찾을 수 있게 연다.
            internal Button DeleteButton => _deleteButton;

            public NodeElement(RestorationCanvasView owner, RestorationNode node)
            {
                _owner = owner;
                Model = node;

                Root = new VisualElement();
                Root.AddToClassList("restoration-node");
                Root.AddToClassList(KindClass(node.Kind));
                if (node.Kind == RestorationNodeKind.ColorRoot)
                    Root.AddToClassList(ColorClass("restoration-node", node.Color));

                _label = new Label();
                _label.AddToClassList("restoration-node__label");
                Root.Add(_label);

                SetPosition(node.Position);
                SetLabel(ResolveLabel(node));

                Root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                Root.RegisterCallback<PointerDownEvent>(OnPointerDown);
                Root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                Root.RegisterCallback<PointerUpEvent>(OnPointerUp);

                Root.RegisterCallback<ClickEvent>(OnClick);

                if (node.Kind == RestorationNodeKind.PlayerAuthored)
                {
                    // 항상 보이는 삭제 버튼. 우클릭도 유지하되 필수 진입점은 아니다.
                    _deleteButton = new Button { text = "×" };
                    _deleteButton.AddToClassList("restoration-node__delete");
                    _deleteButton.clicked += ShowDeleteConfirm;
                    Root.Add(_deleteButton);

                    Root.RegisterCallback<ContextClickEvent>(_ => ShowDeleteConfirm());
                }
            }

            // 확대하면(zoom↑) 노드와 함께 커지는 × 를 되돌려 작게, 축소하면
            // 살짝만 키운다. 역배율을 좁게 묶어(0.65~1.15) 옛날처럼 노드보다
            // 커지는 일이 없게 한다 — 낮은 확대율에선 노드 자체가 작으니 이
            // 정도면 충분히 눌린다.
            public void ApplyInverseZoom(float zoom)
            {
                if (_deleteButton == null)
                    return;

                var inverse = Mathf.Clamp(1f / Mathf.Max(zoom, 0.01f), 0.65f, 1.15f);
                _deleteButton.style.scale = new Scale(new Vector2(inverse, inverse));
            }

            public void Bind(RestorationNode node)
            {
                Model = node;
                SetPosition(node.Position);
                if (_editField == null)
                    SetLabel(ResolveLabel(node));
            }

            public void SetPosition(BoardPosition position)
            {
                Left = Mathf.Clamp(position.X, 0f, RestorationAutoLayout.CanvasWidth - Mathf.Max(_size.x, 1f));
                Top = Mathf.Clamp(position.Y, 0f, RestorationAutoLayout.CanvasHeight - Mathf.Max(_size.y, 1f));
                Root.style.left = Left;
                Root.style.top = Top;
            }

            public void SetLabel(string label)
            {
                Model = Model.WithLabel(label);
                _label.text = string.IsNullOrEmpty(label) ? "(빈 메모)" : label;
            }

            public void SetConnectHighlight(bool on) =>
                Root.EnableInClassList("restoration-node--connect-pick", on);

            private string ResolveLabel(RestorationNode node)
            {
                if (node.Kind == RestorationNodeKind.ColorRoot && string.IsNullOrEmpty(node.Label))
                    return _owner._rootLabeler(node.Color);

                return node.Label;
            }

            private void OnGeometryChanged(GeometryChangedEvent evt)
            {
                _size = evt.newRect.size;
                _owner.RefreshEdges();
            }

            // × 버튼·확인 팝업·이름 편집칸에서 시작된 포인터/클릭은 노드
            // 본체(드래그·연결 선택·더블클릭 편집)가 가로채면 안 된다 — 이들이
            // 잡으면 × 를 눌러도 드래그만 시작되고 버튼은 안 먹는다.
            private bool EventFromOverlayControl(IEventHandler target)
            {
                for (var ve = target as VisualElement; ve != null && ve != Root; ve = ve.parent)
                {
                    if (ve == _deleteButton || ve == _confirmPopup || ve == _editField)
                        return true;
                }

                return false;
            }

            private void OnPointerDown(PointerDownEvent evt)
            {
                // 지난 드래그의 흔적을 먼저 지운다 — 안 그러면 다음 클릭이
                // "방금 드래그였다"로 오인돼 삼켜진다(연결 모드 클릭 포함).
                _pointerMoved = false;

                if (_editField != null || EventFromOverlayControl(evt.target))
                    return;

                if (_owner.IsConnectMode)
                {
                    // 연결 모드에서는 클릭이 우선 — 드래그를 시작하지 않는다.
                    return;
                }

                _dragging = true;
                _dragStartPointer = evt.position;
                _dragStartLeft = Left;
                _dragStartTop = Top;

                // 포인터를 캡처하면 이후 이동/뗌이 이 노드로만 오고 스크롤뷰가
                // 가로채 패닝하지 않는다.
                Root.CapturePointer(evt.pointerId);
            }

            private void OnPointerMove(PointerMoveEvent evt)
            {
                if (!_dragging || !Root.HasPointerCapture(evt.pointerId))
                    return;

                var screenDelta = (Vector2)evt.position - _dragStartPointer;
                if (screenDelta.magnitude >= DragThreshold)
                    _pointerMoved = true;

                // 화면 픽셀 이동량을 현재 배율로 나눠 논리 좌표 이동량으로 되돌린다.
                var zoom = Mathf.Max(_owner.Zoom, 0.01f);
                SetPosition(new BoardPosition(
                    _dragStartLeft + screenDelta.x / zoom,
                    _dragStartTop + screenDelta.y / zoom));
                _owner.RefreshEdges();
            }

            private void OnPointerUp(PointerUpEvent evt)
            {
                if (!_dragging)
                    return;

                _dragging = false;
                if (Root.HasPointerCapture(evt.pointerId))
                    Root.ReleasePointer(evt.pointerId);

                if (_pointerMoved)
                    _owner.RaiseNodeDragCommitted(Model.Id, new BoardPosition(Left, Top));
            }

            private void OnClick(ClickEvent evt)
            {
                if (_pointerMoved || EventFromOverlayControl(evt.target))
                    return;

                if (_owner.IsConnectMode)
                {
                    _owner.OnNodeClicked(this);
                    return;
                }

                if (evt.clickCount >= 2 && Model.Kind == RestorationNodeKind.PlayerAuthored)
                    EnterEdit();
            }

            private void EnterEdit()
            {
                if (_editField != null)
                    return;

                _editField = new TextField { value = Model.Label };
                _editField.AddToClassList("restoration-node__edit");
                _label.style.display = DisplayStyle.None;
                Root.Insert(0, _editField);
                _editField.Focus();
                _editField.SelectAll();

                _editField.RegisterCallback<BlurEvent>(_ => CommitEdit());
                _editField.RegisterCallback<KeyDownEvent>(e =>
                {
                    if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                        CommitEdit();
                    else if (e.keyCode == KeyCode.Escape)
                        CancelEdit();
                });
            }

            private void CommitEdit()
            {
                if (_editField == null)
                    return;

                var text = _editField.value?.Trim() ?? string.Empty;
                ExitEdit();

                if (!string.IsNullOrEmpty(text) && text != Model.Label)
                    _owner.RaiseNodeRenameCommitted(Model.Id, text);
                else
                    SetLabel(Model.Label);
            }

            private void CancelEdit()
            {
                ExitEdit();
                SetLabel(Model.Label);
            }

            private void ExitEdit()
            {
                if (_editField == null)
                    return;

                _editField.RemoveFromHierarchy();
                _editField = null;
                _label.style.display = DisplayStyle.Flex;
            }

            private void ShowDeleteConfirm()
            {
                if (_confirmPopup != null)
                    return;

                _confirmDelete = () =>
                {
                    _confirmPopup?.RemoveFromHierarchy();
                    _confirmPopup = null;
                    _confirmDelete = null;
                    _owner.RaisePlayerNodeDeleteRequested(Model.Id);
                };

                _confirmPopup = _owner.BuildConfirmPopup("이 메모를 지울까요?", _confirmDelete);
                _confirmPopup.RegisterCallback<DetachFromPanelEvent>(_ =>
                {
                    _confirmPopup = null;
                    _confirmDelete = null;
                });
                Root.Add(_confirmPopup);
            }

            // ── 테스트 접근 ──────────────────────────────────────────────
            // Button.clicked는 패널 없는 EditMode에서 부를 수 없으므로, × 클릭과
            // 확인 클릭이 타는 코드 경로를 그대로 여기서도 열어 둔다.
            internal bool HasDeleteConfirmOpen => _confirmPopup != null;
            internal void ClickDeleteButtonForTest() => ShowDeleteConfirm();
            internal void ConfirmDeleteForTest() => _confirmDelete?.Invoke();

            private static string KindClass(RestorationNodeKind kind)
            {
                switch (kind)
                {
                    case RestorationNodeKind.ColorRoot: return "restoration-node--root";
                    case RestorationNodeKind.ClueLinked: return "restoration-node--clue";
                    default: return "restoration-node--player";
                }
            }
        }
    }
}
