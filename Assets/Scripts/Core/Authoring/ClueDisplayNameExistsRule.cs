using System.Collections.Generic;

namespace GameName.Core.Authoring
{
    // 방에 놓인 단서마다 표시 이름이 붙어 있는지 본다.
    //
    // 경고에서 멈추는 이유는 CluePositionOverlapRule과 같다 — 이름이 비어도
    // 플레이는 된다(화면이 "벽에 붙은 포스터" 같은 종류 이름으로 대신 채운다).
    // 다만 미끼가 여럿인 방에서 이름 없는 단서가 섞이면 무엇을 고르는지
    // 알기 어려워지므로, 저작이 빠졌다는 신호는 남긴다.
    public sealed class ClueDisplayNameExistsRule : IDialogueScriptRule
    {
        public IReadOnlyList<ScriptIssue> Check(RunDefinition run)
        {
            var issues = new List<ScriptIssue>();

            foreach (var room in run.Rooms)
            foreach (var clue in room.Clues)
            {
                if (!string.IsNullOrWhiteSpace(clue.DisplayName))
                    continue;

                issues.Add(new ScriptIssue(
                    ScriptIssueSeverity.Warning,
                    $"방 {room.Id}의 단서 {clue.Id}에 표시 이름이 없다. " +
                    "화면에는 종류 이름으로 대신 표시된다."));
            }

            return issues;
        }
    }
}
