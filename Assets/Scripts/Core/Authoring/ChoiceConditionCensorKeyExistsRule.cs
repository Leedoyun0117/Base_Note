using System;
using System.Collections.Generic;
using GameName.Core.Dialogue;

namespace GameName.Core.Authoring
{
    // 선택지가 풀리기를 요구하는 검열 키가 판 안에 실제로 있는지 본다.
    //
    // 없는 키를 요구하는 선택지는 영영 조건을 만족하지 못해 화면에 한 번도
    // 나오지 않는다. 그런데 그것이 "아직 조건을 못 채웠다"와 겉으로 구분되지
    // 않는다 — 플레이어에게도, 테스트 플레이하는 기획자에게도. 대사가 끊긴 것도
    // 아니고 오류가 나는 것도 아니라서, 그 선택지가 존재했다는 사실 자체가
    // 조용히 사라진다.
    //
    // 키에 오타를 내거나, 대사를 고치면서 토큰의 키만 바꾸고 그 키를 걸어 둔
    // 선택지를 잊는 것이 흔한 경로다.
    //
    // 검사 대상은 CensorKeyRevealed 조건뿐이다. 조건이 없는 선택지는 항상
    // 보이고, 단서 사용 조건은 검열과 다른 축이라 여기서 볼 것이 없다.
    public sealed class ChoiceConditionCensorKeyExistsRule : IDialogueScriptRule
    {
        private readonly ICensorTokenIndexSource _tokens;

        public ChoiceConditionCensorKeyExistsRule(ICensorTokenIndexSource tokens)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        public IReadOnlyList<ScriptIssue> Check(RunDefinition run)
        {
            var issues = new List<ScriptIssue>();

            // 원문을 다시 훑지 않는다. 어떤 키가 판 안에 있는지는 이미 모아 둔
            // 것을 묻고, 여기서는 조건만 본다.
            var tokens = _tokens.For(run);

            foreach (var room in run.Rooms)
            foreach (var line in room.EnumerateAuthoredLines())
            foreach (var choice in line.Choices)
            {
                if (choice.Condition.Kind != ChoiceConditionKind.CensorKeyRevealed)
                    continue;

                var key = choice.Condition.RequiredCensorKey.Value;
                if (tokens.Contains(key))
                    continue;

                issues.Add(new ScriptIssue(
                    ScriptIssueSeverity.Error,
                    $"방 {room.Id}의 대사 {line.Id}에 달린 선택지 {choice.Id}가 검열 키 " +
                    $"{key}가 풀리기를 요구하지만, 판 안의 어떤 대사도 그 키로 가려져 있지 " +
                    "않아 이 선택지는 영영 보이지 않는다."));
            }

            return issues;
        }
    }
}
