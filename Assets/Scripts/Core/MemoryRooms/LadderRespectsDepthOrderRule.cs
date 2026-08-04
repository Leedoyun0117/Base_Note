using System.Collections.Generic;

namespace GameName.Core.MemoryRooms
{
    // "아래로 갈수록 과거, 위로 갈수록 현재"는 표현이 아니라 규칙이다 — 사다리는
    // 위쪽 방(UpperRoom)이 아래쪽 방(LowerRoom)보다 반드시 더 현재(=더 작은
    // Row)에 있어야 한다. 좌표가 뒤집힌 채로 그래프를 만들면 지도가 사다리를
    // 거꾸로 그리게 되므로, 콘텐츠를 실제로 조립하기 전에 이 규칙으로 미리
    // 잡아낸다.
    public sealed class LadderRespectsDepthOrderRule : IMemoryGraphConsistencyRule
    {
        public IReadOnlyList<MemoryGraphIssue> Check(
            IReadOnlyList<MemoryGraphNode> nodes, IReadOnlyList<LadderConnection> ladderConnections)
        {
            var issues = new List<MemoryGraphIssue>();

            var nodesById = new Dictionary<MemoryGraphNodeId, MemoryGraphNode>(nodes.Count);
            foreach (var node in nodes)
                nodesById[node.Id] = node;

            foreach (var ladder in ladderConnections)
            {
                var upperNodeId = MemoryGraphNodeId.OfRoom(ladder.UpperRoom);
                var lowerNodeId = MemoryGraphNodeId.OfRoom(ladder.LowerRoom);

                if (!nodesById.TryGetValue(upperNodeId, out var upperNode) ||
                    !nodesById.TryGetValue(lowerNodeId, out var lowerNode))
                {
                    // 노드 목록에 없는 방을 사다리가 참조하는 것은 이 규칙이 다룰
                    // 문제가 아니다 — 그래프 조립(MemoryRoomGraph.Load)이 이미
                    // 예외로 드러낸다.
                    continue;
                }

                if (upperNode.Coordinate.Row >= lowerNode.Coordinate.Row)
                {
                    issues.Add(new MemoryGraphIssue(
                        MemoryGraphIssueSeverity.Error,
                        $"사다리의 위쪽 방({ladder.UpperRoom})의 Row({upperNode.Coordinate.Row})가 " +
                        $"아래쪽 방({ladder.LowerRoom})의 Row({lowerNode.Coordinate.Row})보다 " +
                        "작아야 하는데(더 현재여야 하는데) 그렇지 않다."));
                }
            }

            return issues;
        }
    }
}
