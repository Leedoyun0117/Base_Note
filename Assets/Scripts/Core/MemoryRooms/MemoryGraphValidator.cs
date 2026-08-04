using System;
using System.Collections.Generic;

namespace GameName.Core.MemoryRooms
{
    // 기억 그래프 검증기. RoomDataValidator와 같은 조립 방식 — 주입받은 규칙을
    // 전부 돌려 문제를 모아 반환할 뿐, 규칙 내용 자체는 몰라도 된다.
    public sealed class MemoryGraphValidator
    {
        private readonly IReadOnlyList<IMemoryGraphConsistencyRule> _rules;

        public MemoryGraphValidator(IReadOnlyList<IMemoryGraphConsistencyRule> rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public IReadOnlyList<MemoryGraphIssue> Validate(
            IReadOnlyList<MemoryGraphNode> nodes, IReadOnlyList<LadderConnection> ladderConnections)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (ladderConnections == null) throw new ArgumentNullException(nameof(ladderConnections));

            var issues = new List<MemoryGraphIssue>();
            foreach (var rule in _rules)
                issues.AddRange(rule.Check(nodes, ladderConnections));

            return issues;
        }
    }
}
