using System;
using System.Collections.Generic;

namespace GameName.Core.Clues
{
    // 방 데이터 검증기. 주입받은 규칙을 전부 돌려 문제를 모아 반환한다.
    // 규칙 자체의 내용은 몰라도 되고, 어떤 규칙을 어떤 순서로 쓸지만 조립한다.
    public sealed class RoomDataValidator
    {
        private readonly IReadOnlyList<IRoomDataConsistencyRule> _rules;

        public RoomDataValidator(IReadOnlyList<IRoomDataConsistencyRule> rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public IReadOnlyList<RoomDataIssue> Validate(MemoryRoomData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var issues = new List<RoomDataIssue>();
            foreach (var rule in _rules)
                issues.AddRange(rule.Check(data));

            return issues;
        }
    }
}
