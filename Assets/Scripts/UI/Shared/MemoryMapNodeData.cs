using GameName.Core.MemoryRooms;

namespace GameName.UI.Shared
{
    // 지도가 노드 하나를 그리는 데 필요한 정보만 모은 화면 전용 값. 지도는
    // 이 데이터를 그대로 그릴 뿐, 어떤 방이 복원됐는지/가 봤는지를 스스로
    // 계산하지 않는다 — 그 판단은 화면별 컨트롤러가 Core에 물어 채운다.
    public readonly struct MemoryMapNodeData
    {
        public MemoryGraphNodeId Id { get; }
        public MemoryGraphNodeType Type { get; }
        public MemoryGraphCoordinate Coordinate { get; }
        public bool IsCurrent { get; }
        public bool IsRestored { get; }
        public bool IsVisited { get; }
        public bool IsSelected { get; }

        // 이 노드가 기억 진입/이탈 지점(계단)인지. 지도는 이 노드를 다른 허브
        // 노드와 시각적으로 구분해 그린다 — 클릭했을 때 실제로 일어나는 일
        // (일반 이동이 아니라 즉시 이탈 지점 복귀)이 다르므로, 눌러보기 전에도
        // "여기가 나가는 곳"임을 알 수 있어야 한다.
        public bool IsMemoryExit { get; }

        public MemoryMapNodeData(
            MemoryGraphNodeId id,
            MemoryGraphNodeType type,
            MemoryGraphCoordinate coordinate,
            bool isCurrent,
            bool isRestored,
            bool isVisited,
            bool isSelected,
            bool isMemoryExit)
        {
            Id = id;
            Type = type;
            Coordinate = coordinate;
            IsCurrent = isCurrent;
            IsRestored = isRestored;
            IsVisited = isVisited;
            IsSelected = isSelected;
            IsMemoryExit = isMemoryExit;
        }
    }
}
