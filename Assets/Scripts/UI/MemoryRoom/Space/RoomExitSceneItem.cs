using GameName.Core.MemoryRooms;
using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 방을 드나드는 지점 하나를 그리기 위한 지시.
    //
    // 목적지 노드 식별자를 그대로 들고 다닌다 — 눌렀을 때 컨트롤러가 이동
    // 처리기에 넘길 값이 바로 이것이고, 씬 쪽은 그 식별자가 무엇을 뜻하는지
    // (어떤 방인지, 갈 수 있는지, 얼마가 드는지) 전혀 알지 못한다.
    public readonly struct RoomExitSceneItem
    {
        public MemoryGraphNodeId TargetNodeId { get; }
        public RoomExitKind Kind { get; }
        public Vector2 Position { get; }
        public string Label { get; }

        public RoomExitSceneItem(MemoryGraphNodeId targetNodeId, RoomExitKind kind, Vector2 position, string label)
        {
            TargetNodeId = targetNodeId;
            Kind = kind;
            Position = position;
            Label = label;
        }
    }
}
