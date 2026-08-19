using System;
using System.Collections.Generic;

namespace GameName.Core.Clues
{
    // 방 데이터 검증기. 주입받은 규칙을 전부 돌려 문제를 모아 반환한다.
    // 규칙 자체의 내용은 몰라도 되고, 어떤 규칙을 어떤 순서로 쓸지만 조립한다.
    //
    // ── 검증의 적용 범위(중요) ─────────────────────────────────────────
    // 이 검증기는 "기획자가 적어 넣은 초기 데이터"만 검사한다. 플레이 중에
    // 일어나는 단서 재배정(단서를 다른 방에 버려 그 방 소속이 되는 것)에는
    // 전혀 적용되지 않으며, 적용할 방법도 없다 — 검사 단위인 MemoryRoomData는
    // 정답과 단서를 짝지어 놓은 "저작 시점의 묶음"이라, 플레이 중에 바뀐 소속을
    // 표현하지 못한다. 그건 IMemoryRoomClueTracker 안의 상태다.
    //
    // 그래서 검증을 통과했다는 사실이 런타임 내내 유지되는 불변식이 되지는
    // 않는다. 특히 ClueTrueCompositionMatchesAnswerRule이 보장하려던 "이 방의
    // 단서들은 이 방 정답과 어울린다"는 성질은, 플레이어가 단서를 다른 방으로
    // 옮기는 순간 깨질 수 있다. 어떤 런타임 코드도 이 검증 결과에 의존해
    // 판단을 내려서는 안 된다(현재 의존하는 코드는 없다 — 결과는 기획자에게
    // 보여주는 경고/오류 목록으로만 쓰인다).
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
