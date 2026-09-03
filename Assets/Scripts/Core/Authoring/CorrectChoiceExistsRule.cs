using System.Collections.Generic;
using GameName.Core.Dialogue;

namespace GameName.Core.Authoring
{
    // 선택지가 달린 대사마다 정답 선택지가 최소 하나 있는지 본다.
    //
    // 전부 오답인 대사는 신뢰도를 깎는 것 말고는 아무 데도 가지 못하는 막다른
    // 길이다. 그런 자리를 일부러 만드는 기획은 없으므로, 나오면 IsCorrect 체크를
    // 빠뜨린 것이다.
    //
    // 선택지가 아예 없는 대사는 그냥 넘어간다 — 혼잣말이나 마지막 줄처럼
    // 정상적으로 선택지가 없는 대사가 있고, 무엇보다 대사를 아직 붙이지 않은
    // 껍데기 데이터가 이 규칙에 걸려서는 안 되기 때문이다.
    public sealed class CorrectChoiceExistsRule : IDialogueScriptRule
    {
        public IReadOnlyList<ScriptIssue> Check(RunDefinition run)
        {
            var issues = new List<ScriptIssue>();

            foreach (var room in run.Rooms)
            {
                foreach (var line in room.DialogueLines)
                {
                    if (!IsDeadEnd(line))
                        continue;

                    issues.Add(new ScriptIssue(
                        ScriptIssueSeverity.Error,
                        $"방 {room.Id}의 대사 {line.Id}에 정답 선택지가 하나도 없다."));
                }

                // 분기 풀은 후보 단위가 아니라 풀 단위로 본다 — 시드가 어느 후보를
                // 뽑든 정답으로 나아갈 길이 하나는 있어야 하지만, 후보마다 전부
                // 정답 선택지를 요구하지는 않는다("최소 1개").
                foreach (var pool in room.BranchPools)
                {
                    if (pool.Candidates.Count == 0)
                        continue;

                    var anyViable = false;
                    foreach (var candidate in pool.Candidates)
                    {
                        if (IsDeadEnd(candidate))
                            continue;

                        anyViable = true;
                        break;
                    }

                    if (!anyViable)
                    {
                        issues.Add(new ScriptIssue(
                            ScriptIssueSeverity.Error,
                            $"방 {room.Id}의 분기 풀 {pool.Candidates[0].Id}의 후보 어느 것도 " +
                            "정답 선택지로 이어지지 않는다 — 어떤 시드로 뽑혀도 막다른 길이다."));
                    }
                }
            }

            return issues;
        }

        // 선택지가 있는데 전부 오답인 줄. 선택지가 아예 없는 줄(혼잣말·마지막 줄)은
        // 정상이므로 막다른 길로 치지 않는다.
        private static bool IsDeadEnd(DialogueLineDefinition line)
        {
            if (line.Choices.Count == 0)
                return false;

            foreach (var choice in line.Choices)
            {
                if (choice.IsCorrect)
                    return false;
            }

            return true;
        }
    }
}
