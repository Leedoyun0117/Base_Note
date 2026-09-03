using System.Collections.Generic;

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
            foreach (var line in room.DialogueLines)
            {
                if (line.Choices.Count == 0)
                    continue;

                var hasCorrect = false;
                foreach (var choice in line.Choices)
                {
                    if (!choice.IsCorrect)
                        continue;

                    hasCorrect = true;
                    break;
                }

                if (!hasCorrect)
                {
                    issues.Add(new ScriptIssue(
                        ScriptIssueSeverity.Error,
                        $"방 {room.Id}의 대사 {line.Id}에 정답 선택지가 하나도 없다."));
                }
            }

            return issues;
        }
    }
}
