using GameName.Core.Restoration;

namespace GameName.Core.Events
{
    // 복원도 노드 하나가 새 자리로 옮겨졌다는 사실. 배치만 바뀌었을 뿐 내용은
    // 그대로다.
    public readonly struct RestorationNodeMovedEvent
    {
        public RestorationNodeId NodeId { get; }
        public BoardPosition Position { get; }

        public RestorationNodeMovedEvent(RestorationNodeId nodeId, BoardPosition position)
        {
            NodeId = nodeId;
            Position = position;
        }
    }
}
