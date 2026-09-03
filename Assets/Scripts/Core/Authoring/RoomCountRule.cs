using System;
using System.Collections.Generic;

namespace GameName.Core.Authoring
{
    // 한 판의 방 개수가 기획이 정한 수와 맞는지 본다.
    //
    // 개수를 여기 박지 않고 주입받는 이유: 세 개라는 것은 지금의 기획 수치이지
    // 데이터 구조의 성질이 아니다. 이 규칙을 조립하는 자리만 고치면 개수가
    // 바뀐다.
    public sealed class RoomCountRule : IDialogueScriptRule
    {
        private readonly int _expectedRoomCount;

        public RoomCountRule(int expectedRoomCount)
        {
            if (expectedRoomCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(expectedRoomCount));

            _expectedRoomCount = expectedRoomCount;
        }

        public IReadOnlyList<ScriptIssue> Check(RunDefinition run)
        {
            if (run.Rooms.Count == _expectedRoomCount)
                return Array.Empty<ScriptIssue>();

            return new[]
            {
                new ScriptIssue(
                    ScriptIssueSeverity.Error,
                    $"한 판은 방 {_expectedRoomCount}개로 이뤄져야 하는데 {run.Rooms.Count}개다.")
            };
        }
    }
}
