using System;
using System.Collections.Generic;

namespace GameName.Core.Authoring
{
    // 저작 데이터 검증기.
    //
    // 왜 저작 시점에 검사하는가: 여기서 걸리는 문제들은 전부 "플레이하다 보면
    // 어느 순간 막힌다" 부류라, 실제로 그 지점까지 플레이해 보기 전에는 드러나지
    // 않는다. 풀 수 없는 검열이 섞인 대사는 특히 그렇다 — 화면에는 멀쩡히
    // 보이지만 그 방에서는 영영 열리지 않는다.
    //
    // 이름이 "대본" 검증기이지만 단서 배치까지 함께 보는 것은, 무엇이 검열을
    // 풀어 주는지가 결국 방에 놓인 단서에 달려 있어 둘을 갈라 놓고는 아무것도
    // 판단할 수 없기 때문이다.
    public sealed class DialogueScriptValidator
    {
        private readonly IReadOnlyList<IDialogueScriptRule> _rules;

        public DialogueScriptValidator(IReadOnlyList<IDialogueScriptRule> rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public IReadOnlyList<ScriptIssue> Validate(RunDefinition run)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));

            var issues = new List<ScriptIssue>();
            foreach (var rule in _rules)
                issues.AddRange(rule.Check(run));

            return issues;
        }
    }
}
