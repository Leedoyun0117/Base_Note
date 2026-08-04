using System;
using System.Collections.Generic;
using GameName.Core.MemoryRooms;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.Shared
{
    // 재사용 가능한 기억 지도 컴포넌트. 기억 방/분석실 화면(이동)과 조향실
    // 화면(이동 + 목표 선택) 모두 이 하나만 인스턴스를 새로 만들어 쓴다.
    //
    // 이동/조향 규칙을 전혀 모른다 — 노드를 격자에 배치해 그리고, 연결선을
    // 그리고(문/사다리, 사다리는 잠김·열림 구분), 클릭된 노드 id만 밖으로
    // 알린다. "그 노드를 눌렀을 때 무엇을 할지"는 이 타입이 판단하지 않는다.
    //
    // 화면에는 항상 작은 미리보기(축소 지도)만 보이고, 그걸 누르면 화면
    // 대부분을 덮는 확대 지도가 뜬다 — 좁은 패널 폭 안에 실제 클릭용 지도를
    // 항상 펼쳐두면 스크롤 없이는 전체 구조가 한눈에 안 들어오기 때문이다.
    // 미리보기는 클릭 지점을 구분할 필요가 없어(눌러도 "확대"만 하면 되므로)
    // Painter2D 한 번으로 선+점을 전부 그린다. 확대 지도는 기존처럼 노드마다
    // 실제 VisualElement를 만들어 각각 클릭을 받는다 — 이 둘은 같은
    // SetMap 데이터를 공유할 뿐 렌더링 방식은 서로 다르다.
    //
    // root는 화면 전체(예: "screen")여야 한다 — 좁은 패널 안의 미리보기와,
    // 화면을 덮는 확대 오버레이가 서로 다른 조상 아래 있어 패널 하나만으로는
    // 둘 다 찾을 수 없다.
    public sealed class MemoryMapView
    {
        private const float CellSize = 96f;
        private const float NodeSize = 56f;
        private const float DoorLineWidth = 3f;
        private const float LadderLineWidth = 4f;
        private const float PreviewLineWidth = 1.5f;
        private const float PreviewDotRadius = 4f;
        private const float PreviewMargin = 10f;
        private const float PreviewNodeHitSize = 18f;

        // 기존 디자인 토큰(각 화면 .uss의 :root)과 같은 16진값이다. Painter2D는
        // USS 커스텀 프로퍼티(var(--x))를 읽을 수 없어 리터럴 Color가
        // 필요하므로, 토큰 원본을 그대로 옮겨 적었을 뿐 새 색을 만든 것이
        // 아니다 — 토큰 값이 바뀌면 이 값도 함께 맞춰야 한다.
        private static readonly Color DoorColor = new Color32(0x36, 0x3B, 0x44, 0xFF); // --border
        private static readonly Color UnlockedLadderColor = new Color32(0x6F, 0xA8, 0x6B, 0xFF); // --success
        private static readonly Color LockedLadderColor = new Color32(0xC7, 0x5B, 0x4A, 0xFF); // --danger
        private static readonly Color CurrentNodeColor = new Color32(0xE4, 0xE6, 0xEA, 0xFF); // --text
        private static readonly Color UnvisitedNodeColor = new Color32(0x5A, 0x61, 0x6B, 0xFF); // --text-faint

        private readonly VisualElement _previewFrame;
        private readonly VisualElement _previewDrawLayer;
        private readonly VisualElement _previewNodesLayer;
        private readonly VisualElement _expandedOverlay;
        private readonly Button _closeButton;
        private readonly VisualElement _mapContainer;
        private readonly VisualElement _connectionsLayer;
        private readonly VisualElement _nodesLayer;
        private readonly Label _selectionInfoLabel;
        private readonly MemoryMapLayout _layout;

        private IReadOnlyList<MemoryMapNodeData> _nodes = Array.Empty<MemoryMapNodeData>();
        private IReadOnlyList<MemoryMapConnectionData> _connections = Array.Empty<MemoryMapConnectionData>();
        private int _maxColumn;
        private int _maxRow;

        public event Action<MemoryGraphNodeId> NodeSelected;

        public MemoryMapView(VisualElement root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            _previewFrame = root.Q<VisualElement>("memory-map-preview");
            _expandedOverlay = root.Q<VisualElement>("memory-map-expanded");
            _closeButton = root.Q<Button>("memory-map-close-button");
            _mapContainer = root.Q<VisualElement>("memory-map");
            _connectionsLayer = root.Q<VisualElement>("memory-map-connections");
            _nodesLayer = root.Q<VisualElement>("memory-map-nodes");
            _selectionInfoLabel = root.Q<Label>("memory-map-selection-info");

            // 격자 한 칸의 픽셀 크기(축척)만 여기서 정한다 — 화면 크기가 아니라
            // 지도 자체의 표시 배율이라 해상도가 달라져도 이 값은 바뀌지 않는다.
            // 원점을 NodeSize만큼 띄워 (0,0) 좌표의 노드도 잘리지 않게 한다.
            _layout = new MemoryMapLayout(CellSize, NodeSize, NodeSize);

            // 연결선 레이어는 순전히 배경 장식이라 클릭을 가로채면 안 된다 —
            // 그 위(뒤가 아니라 같은 영역)에 겹쳐 그려지는 노드 클릭이 이
            // 레이어에 막히지 않도록 명시적으로 Ignore로 둔다.
            _connectionsLayer.pickingMode = PickingMode.Ignore;
            _connectionsLayer.generateVisualContent += DrawConnections;

            // 미리보기는 그림(선+점, 클릭을 받지 않음)과 클릭 판정(노드별 작은
            // 히트박스, 그림 위에 투명하게 겹침) 두 레이어로 나눈다. 그림
            // 레이어를 Ignore로 두는 것과 별개로, 히트박스 레이어 자체도 그
            // 컨테이너 전체가 아니라 실제 자식 노드 요소만 클릭을 받아야
            // 한다 — 그래야 노드가 없는 빈 배경을 눌렀을 때 그 클릭이 아래
            // 프레임까지 내려가 "확대" 트리거로 이어진다(컨테이너를 Ignore로
            // 둬도 그 자식 요소 각각의 pickingMode는 영향받지 않는다).
            _previewDrawLayer = new VisualElement { pickingMode = PickingMode.Ignore };
            _previewDrawLayer.style.position = Position.Absolute;
            _previewDrawLayer.style.left = 0;
            _previewDrawLayer.style.top = 0;
            _previewDrawLayer.style.right = 0;
            _previewDrawLayer.style.bottom = 0;
            _previewDrawLayer.generateVisualContent += DrawPreview;
            _previewFrame.Add(_previewDrawLayer);

            _previewNodesLayer = new VisualElement { pickingMode = PickingMode.Ignore };
            _previewNodesLayer.style.position = Position.Absolute;
            _previewNodesLayer.style.left = 0;
            _previewNodesLayer.style.top = 0;
            _previewNodesLayer.style.right = 0;
            _previewNodesLayer.style.bottom = 0;
            _previewFrame.Add(_previewNodesLayer);

            // 배경(빈 곳)을 누르면 확대되고, 노드 히트박스를 누르면 그 자리에서
            // 바로 이동/선택이 일어난다 — evt.target이 실제로 클릭을 받은
            // 요소를 가리키므로, 프레임 자신이 눌렸을 때와 자식 노드가 눌렸을
            // 때가 자연히 구분된다.
            _previewFrame.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == _previewFrame)
                    SetExpanded(true);
            });
            _previewDrawLayer.RegisterCallback<GeometryChangedEvent>(_ => RebuildPreviewNodes());

            if (_closeButton != null)
                _closeButton.clicked += () => SetExpanded(false);

            // 오버레이의 어두운 배경 부분을 직접 눌렀을 때만 닫는다 — 안쪽
            // 지도/노드 클릭이 버블링되어 올라와도 evt.target은 그 클릭을 받은
            // 원래 요소를 계속 가리키므로, 배경 자체를 눌렀을 때와 구분된다.
            _expandedOverlay.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == _expandedOverlay)
                    SetExpanded(false);
            });

            SetExpanded(false);
        }

        public void SetMap(IReadOnlyList<MemoryMapNodeData> nodes, IReadOnlyList<MemoryMapConnectionData> connections)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));

            _nodes = nodes;
            _connections = connections ?? Array.Empty<MemoryMapConnectionData>();

            _nodesLayer.Clear();

            _maxColumn = 0;
            _maxRow = 0;
            foreach (var node in _nodes)
            {
                _nodesLayer.Add(CreateNodeElement(node));
                _maxColumn = Mathf.Max(_maxColumn, node.Coordinate.Column);
                _maxRow = Mathf.Max(_maxRow, node.Coordinate.Row);
            }

            // 지도 전체를 담을 컨테이너 크기는 실제 노드 좌표 범위로 정한다 —
            // 뷰포트 크기가 아니라 데이터가 필요로 하는 크기다. 넘치면 화면
            // 쪽(스크롤 뷰)이 잘라서 보여준다.
            var contentWidth = NodeSize * 2f + _maxColumn * CellSize;
            var contentHeight = NodeSize * 2f + _maxRow * CellSize;
            _mapContainer.style.width = contentWidth;
            _mapContainer.style.height = contentHeight;

            _connectionsLayer.MarkDirtyRepaint();
            _previewDrawLayer.MarkDirtyRepaint();
            RebuildPreviewNodes();
        }

        // 지도 위에서 보여줄 안내 문구 하나(이동 비용 미리보기, 요구 총량 등)를
        // 그대로 표시한다 — 문구의 의미는 이 지도를 쓰는 화면이 정하고, 지도는
        // 그 내용을 전혀 해석하지 않는다.
        public void SetSelectionInfo(string text)
        {
            if (_selectionInfoLabel == null)
                return;

            _selectionInfoLabel.text = text ?? string.Empty;
            _selectionInfoLabel.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void SetExpanded(bool expanded)
        {
            _expandedOverlay.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private VisualElement CreateNodeElement(MemoryMapNodeData data)
        {
            var position = _layout.ToLocalPosition(data.Coordinate);

            var element = new VisualElement();
            element.AddToClassList("memory-map-node");
            // 스프라이트 교체 지점: 지금은 기억 방/허브를 도형(원/사각형)과
            // 텍스트 라벨만으로 구분한다. 나중에 방/허브 종류별 아이콘
            // 스프라이트로 교체할 자리다.
            element.AddToClassList(
                data.Type == MemoryGraphNodeType.MemoryRoom ? "memory-map-node--room" : "memory-map-node--hub");
            element.EnableInClassList("memory-map-node--current", data.IsCurrent);
            element.EnableInClassList("memory-map-node--restored", data.IsRestored);
            element.EnableInClassList("memory-map-node--unvisited", !data.IsVisited);
            element.EnableInClassList("memory-map-node--selected", data.IsSelected);
            element.EnableInClassList("memory-map-node--exit", data.IsMemoryExit);

            element.style.left = position.x - NodeSize / 2f;
            element.style.top = position.y - NodeSize / 2f;
            element.style.width = NodeSize;
            element.style.height = NodeSize;

            // 이 노드를 누르면 실제로는 일반 이동이 아니라 즉시 이탈 지점
            // 복귀가 일어난다(MemoryRoomMapNavigationController) — 그 차이를
            // 라벨로도 드러내, "여기가 나가는 곳"임을 누르기 전에 알 수 있게
            // 한다.
            var labelText = data.IsMemoryExit ? "🚪 나가기" : MemoryGraphNodeTypeDisplay.Label(data.Type);
            var label = new Label(labelText);
            label.AddToClassList("memory-map-node__label");
            element.Add(label);

            element.RegisterCallback<ClickEvent>(_ => NodeSelected?.Invoke(data.Id));

            return element;
        }

        private void DrawConnections(MeshGenerationContext context)
        {
            var painter = context.painter2D;

            foreach (var connection in _connections)
            {
                var from = _layout.ToLocalPosition(connection.From);
                var to = _layout.ToLocalPosition(connection.To);

                painter.lineWidth = connection.IsLadder ? LadderLineWidth : DoorLineWidth;
                painter.strokeColor = ResolveConnectionColor(connection);
                painter.BeginPath();
                painter.MoveTo(from);
                painter.LineTo(to);
                painter.Stroke();
            }
        }

        // 미리보기는 실제 축척(CellSize)이 아니라, 지금 미리보기 칸에 남은
        // 픽셀 크기에 맞춰 매번 다시 계산한 축척과 원점을 쓴다 — 그래야 격자가
        // 몇 칸이든, 가로세로 비율이 어떻든 항상 미리보기 안에 가운데 정렬되어
        // 전부 들어간다("한눈에 보이게"). 이 계산은 미리보기를 어떻게 그릴지에
        // 대한 이 컴포넌트만의 결정이라, 화면 크기와 무관해야 하는
        // MemoryMapLayout 자체의 계약과는 다르다 — 그 계약을 어기지 않고,
        // 매번 다른 CellSize/원점으로 새 인스턴스를 만들어 쓸 뿐이다. 그림
        // (DrawPreview)과 클릭 히트박스(RebuildPreviewNodes) 양쪽이 같은
        // 계산을 써야 점과 클릭 영역이 어긋나지 않으므로 하나로 모았다.
        private MemoryMapLayout ComputePreviewLayout(Rect rect)
        {
            var columns = Mathf.Max(_maxColumn, 1);
            var rows = Mathf.Max(_maxRow, 1);
            var cellSizeX = (rect.width - PreviewMargin * 2f) / columns;
            var cellSizeY = (rect.height - PreviewMargin * 2f) / rows;
            var cellSize = Mathf.Max(4f, Mathf.Min(cellSizeX, cellSizeY));

            var usedWidth = columns * cellSize;
            var usedHeight = rows * cellSize;
            var originX = (rect.width - usedWidth) / 2f;
            var originY = (rect.height - usedHeight) / 2f;

            return new MemoryMapLayout(cellSize, originX, originY);
        }

        private void DrawPreview(MeshGenerationContext context)
        {
            var rect = _previewDrawLayer.contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            var previewLayout = ComputePreviewLayout(rect);
            var painter = context.painter2D;

            foreach (var connection in _connections)
            {
                var from = previewLayout.ToLocalPosition(connection.From);
                var to = previewLayout.ToLocalPosition(connection.To);

                painter.lineWidth = PreviewLineWidth;
                painter.strokeColor = ResolveConnectionColor(connection);
                painter.BeginPath();
                painter.MoveTo(from);
                painter.LineTo(to);
                painter.Stroke();
            }

            foreach (var node in _nodes)
            {
                var position = previewLayout.ToLocalPosition(node.Coordinate);

                painter.fillColor = ResolvePreviewNodeColor(node);
                painter.BeginPath();
                painter.Arc(position, PreviewDotRadius, 0f, 360f);
                painter.Fill();
            }
        }

        // 미리보기 위에서도 노드를 직접 눌러 바로 이동/선택할 수 있게, 그려진
        // 점과 같은 위치에 작은 투명 클릭 영역을 겹쳐 만든다. 배경을 눌렀을
        // 때(확대)와 노드를 눌렀을 때(즉시 이동/선택)를 evt.target으로
        // 구분하므로, 이 히트박스들은 그 판정 대상일 뿐 아무것도 그리지 않는다.
        private void RebuildPreviewNodes()
        {
            _previewNodesLayer.Clear();

            var rect = _previewDrawLayer.contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            var previewLayout = ComputePreviewLayout(rect);
            foreach (var node in _nodes)
                _previewNodesLayer.Add(CreatePreviewNodeElement(node, previewLayout));
        }

        private VisualElement CreatePreviewNodeElement(MemoryMapNodeData data, MemoryMapLayout previewLayout)
        {
            var position = previewLayout.ToLocalPosition(data.Coordinate);

            var element = new VisualElement();
            element.style.position = Position.Absolute;
            element.style.left = position.x - PreviewNodeHitSize / 2f;
            element.style.top = position.y - PreviewNodeHitSize / 2f;
            element.style.width = PreviewNodeHitSize;
            element.style.height = PreviewNodeHitSize;

            element.RegisterCallback<ClickEvent>(_ => NodeSelected?.Invoke(data.Id));

            return element;
        }

        private static Color ResolveConnectionColor(MemoryMapConnectionData connection)
        {
            if (!connection.IsLadder)
                return DoorColor;

            return connection.IsLocked ? LockedLadderColor : UnlockedLadderColor;
        }

        private static Color ResolvePreviewNodeColor(MemoryMapNodeData node)
        {
            if (node.IsCurrent) return CurrentNodeColor;
            if (node.IsMemoryExit) return LockedLadderColor; // --danger: 나가는 곳임을 눈에 띄게 표시
            if (node.IsRestored) return UnlockedLadderColor;
            if (!node.IsVisited) return UnvisitedNodeColor;
            return DoorColor;
        }
    }
}
