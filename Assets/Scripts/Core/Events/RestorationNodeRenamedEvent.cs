using GameName.Core.Restoration;

namespace GameName.Core.Events
{
    // 플레이어 노드 하나의 이름이 바뀌었다는 사실. 자동 생성 노드는 이름이
    // 잠겨 있으므로 이 사건은 PlayerAuthored 노드에서만 나온다.
    public readonly struct RestorationNodeRenamedEvent
    {
        public RestorationNodeId NodeId { get; }
        public string Label { get; }

        public RestorationNodeRenamedEvent(RestorationNodeId nodeId, string label)
        {
            NodeId = nodeId;
            Label = label;
        }
    }
}
