using System;
using System.Collections.Generic;

namespace GameName.Core.Authoring
{
    // 한 방 안에서 단서들이 서로 너무 가까이 놓였는지 본다.
    //
    // 경고에서 멈추고 자리를 옮기지 않는 이유가 이 규칙의 요점이다. 단서가
    // 놓인 자리는 그 자체가 콘텐츠라(창가의 사진, 문 앞의 반지) 코드가 예쁘게
    // 흩어 놓는 순간 기획이 심어 둔 의미가 조용히 사라진다. 나란히 놓인 두
    // 물건이 의도인 경우도 흔하다. 그래서 판단은 사람에게 돌려주고, 여기서는
    // "겹쳐 보일 수 있다"는 사실만 알린다.
    //
    // 얼마나 가까우면 겹치는가는 아트와 방 길이에 달린 값이라 주입받는다 —
    // 포스터 크기가 바뀌면 이 수치도 함께 바뀐다.
    public sealed class CluePositionOverlapRule : IDialogueScriptRule
    {
        private readonly float _minimumSeparation;

        public CluePositionOverlapRule(float minimumSeparation)
        {
            if (minimumSeparation < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumSeparation));

            _minimumSeparation = minimumSeparation;
        }

        public IReadOnlyList<ScriptIssue> Check(RunDefinition run)
        {
            var issues = new List<ScriptIssue>();

            foreach (var room in run.Rooms)
            for (var i = 0; i < room.Clues.Count; i++)
            for (var j = i + 1; j < room.Clues.Count; j++)
            {
                var left = room.Clues[i];
                var right = room.Clues[j];

                // 벽에 걸린 것과 바닥에 놓인 것은 가로 자리가 같아도 높이가 달라
                // 겹쳐 보이지 않는다. 종류가 다르면 넘어간다.
                if (left.Kind != right.Kind)
                    continue;

                var distance = left.AuthoredPosition.DistanceTo(right.AuthoredPosition);
                if (distance >= _minimumSeparation)
                    continue;

                issues.Add(new ScriptIssue(
                    ScriptIssueSeverity.Warning,
                    $"방 {room.Id}의 단서 {left.Id}와 {right.Id}의 가로 자리가 " +
                    $"{distance:0.##}만큼 떨어져 있어 {_minimumSeparation:0.##}보다 가깝다. " +
                    "겹쳐 보일 수 있다."));
            }

            return issues;
        }
    }
}
