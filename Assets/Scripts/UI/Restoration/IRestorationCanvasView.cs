using System;
using System.Collections.Generic;
using GameName.Core.Memories;
using GameName.Core.Restoration;

namespace GameName.UI.Restoration
{
    // 복원도 캔버스의 표시 표면. 컨트롤러가 Core에서 읽은 결론을 받아 그리고,
    // 플레이어가 캔버스에서 한 행동만(드래그 종료·이름 확정·연결 요청 등)
    // 알린다. 규칙 판정은 하나도 하지 않는다.
    //
    // 테스트가 가짜 구현으로 갈아 끼울 수 있도록 인터페이스로 둔다(Phase 8 패턴).
    public interface IRestorationCanvasView
    {
        // 노드를 드래그로 옮기고 손을 뗐다 — 최종 위치 하나만. 드래그 도중에는
        // 부르지 않는다(이벤트 폭주 방지).
        event Action<RestorationNodeId, BoardPosition> NodeDragCommitted;

        // 플레이어 노드의 이름 편집을 확정했다.
        event Action<RestorationNodeId, string> NodeRenameCommitted;

        // 색 클러스터의 "+메모" 입력을 확인했다.
        event Action<MemoryColor, string> PlayerNodeRequested;

        // 연결 모드에서 두 노드를 차례로 골랐다.
        event Action<RestorationNodeId, RestorationNodeId> EdgeRequested;

        // 플레이어 노드 삭제를 확인했다(잠긴 노드에서는 이 UI 자체가 없다).
        event Action<RestorationNodeId> PlayerNodeDeleteRequested;

        // 플레이어 간선 삭제를 확인했다(잠긴 간선에서는 이 UI 자체가 없다).
        event Action<RestorationEdgeId> PlayerEdgeDeleteRequested;

        // 캔버스를 비운다(전체 다시 그리기 전에).
        void Clear();

        // 색마다 "+메모 추가" 컨트롤을 만든다. 뿌리가 생긴 색만 넘어온다.
        void SetColorClusters(IReadOnlyList<RestorationClusterInfo> clusters);

        void AddNode(RestorationNode node);
        void RemoveNode(RestorationNodeId nodeId);
        void UpdateNodePosition(RestorationNodeId nodeId, BoardPosition position);
        void UpdateNodeLabel(RestorationNodeId nodeId, string label);

        // 현재 확대율은 그대로 두고, 그 노드가 창 한가운데 오도록 이동한다.
        // 방금 추가한 메모가 시야 밖에 생기지 않게 하려는 것.
        void FocusNode(RestorationNodeId nodeId);

        void AddEdge(RestorationEdge edge);
        void RemoveEdge(RestorationEdgeId edgeId);

        // 연결 실패 등 짧은 안내. null이면 지운다.
        void ShowNotice(string message);

        // 연결 모드의 절반 진행된 선택을 취소한다.
        void ResetConnectSelection();
    }

    // 색 클러스터 하나의 표시 정보(뿌리가 생긴 색 + 화면에 쓸 색 이름).
    public readonly struct RestorationClusterInfo
    {
        public MemoryColor Color { get; }
        public string Label { get; }

        public RestorationClusterInfo(MemoryColor color, string label)
        {
            Color = color;
            Label = label;
        }
    }
}
