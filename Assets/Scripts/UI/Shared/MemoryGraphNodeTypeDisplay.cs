using GameName.Core.MemoryRooms;

namespace GameName.UI.Shared
{
    // 그래프 노드 종류를 화면에 보여주기 위한 순수 표시용 매핑.
    internal static class MemoryGraphNodeTypeDisplay
    {
        public static string Label(MemoryGraphNodeType type)
        {
            switch (type)
            {
                case MemoryGraphNodeType.MemoryRoom: return "기억 방";
                case MemoryGraphNodeType.Staircase: return "계단";
                default: return type.ToString();
            }
        }
    }
}
