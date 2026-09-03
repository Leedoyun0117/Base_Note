using System;
using System.Collections.Generic;
using GameName.Core.Dialogue;
using GameName.Core.Memories;

namespace GameName.Core.Authoring
{
    // 같은 검열 키가 어디서나 같은 색으로 적혀 있는지 본다.
    //
    // 키와 색을 갈라 놓으면 생기는 유일한 빈틈이 이것이다. 키는 "무엇이 가려져
    // 있는가"이고 색은 "그것을 열려면 무엇이 필요한가"인데, 한 사실을 여는 데
    // 필요한 색이 대사마다 다를 수는 없다. 그런데 원문에는 대사마다 따로 적히니
    // 복사해 붙이다 색만 어긋나는 일이 생긴다.
    //
    // 이것이 실행 시점에는 어떻게 보이는가: 그 사실은 키 하나라 한 번에 풀리는데,
    // 풀리기 전까지 어떤 줄에서는 파란색을 찾으라 하고 어떤 줄에서는 빨간색을
    // 찾으라 한다. 플레이어는 있지도 않은 단서를 찾아 헤매게 되고, 게임은
    // 아무것도 잘못되지 않은 것처럼 굴러간다 — 저작 시점이 아니면 잡을 곳이 없다.
    //
    // 판을 통틀어 검사하는 이유: 한 사실이 여러 방에 걸쳐 언급되는 것이 이 게임의
    // 전제다. 방마다 따로 보면 방을 건너뛴 어긋남을 그대로 놓친다.
    public sealed class CensorKeyColorConsistencyRule : IDialogueScriptRule
    {
        private readonly ICensorTokenIndexSource _tokens;

        public CensorKeyColorConsistencyRule(ICensorTokenIndexSource tokens)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        public IReadOnlyList<ScriptIssue> Check(RunDefinition run)
        {
            var issues = new List<ScriptIssue>();

            // 키마다 "처음 본 색과 그때의 자리"를 들고 있다가, 다른 색이 나오면
            // 둘을 나란히 보여 준다 — 어긋난 쪽만 알려 주면 어디와 어긋났는지
            // 찾으러 원문을 뒤져야 한다.
            var firstUse = new Dictionary<CensorKey, CensorTokenUse>();

            // 한 키가 세 군데에서 어긋나도 문제는 하나다. 같은 키를 두 번
            // 보고하면 콘솔만 길어진다.
            var reported = new HashSet<CensorKey>();

            foreach (var use in _tokens.For(run).Uses)
            {
                if (!firstUse.TryGetValue(use.Key, out var first))
                {
                    firstUse[use.Key] = use;
                    continue;
                }

                if (first.Color == use.Color || !reported.Add(use.Key))
                    continue;

                issues.Add(new ScriptIssue(
                    ScriptIssueSeverity.Error,
                    $"검열 키 {use.Key}가 {first.Location}에서는 {first.Color}로, " +
                    $"{use.Location}에서는 {use.Color}로 적혀 있다. " +
                    "한 사실을 여는 색은 하나여야 한다."));
            }

            return issues;
        }
    }
}
