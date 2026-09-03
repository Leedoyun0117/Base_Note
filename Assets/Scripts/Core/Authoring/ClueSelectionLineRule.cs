using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Dialogue;

namespace GameName.Core.Authoring
{
    // ClueSelection 줄의 저작이 성립하는지 본다.
    //
    // 이 줄은 텍스트 선택지 대신 "가진 단서로 답하라"이고, 정답 판정을
    // RequiredClueIds에 기댄다. 그것이 비어 있거나 그 방에 없는 단서를 가리키면
    // 어떤 단서를 골라도 오답이 되어(또는 그 반대로) 플레이어가 이유를 알 수
    // 없이 막힌다 — 저작 시점이 아니면 잡을 곳이 없다.
    //
    // CorrectNext/IncorrectNext가 실재하는 라인인지는 NextLineExistsRule이 본다.
    // 오답 서브체인이 결국 메인 줄기로 합류하는지는 자동으로 보기 어려워 수동
    // 확인 대상으로 남긴다.
    public sealed class ClueSelectionLineRule : IDialogueScriptRule
    {
        public IReadOnlyList<ScriptIssue> Check(RunDefinition run)
        {
            var issues = new List<ScriptIssue>();

            foreach (var room in run.Rooms)
            {
                var roomClues = new HashSet<ClueId>();
                foreach (var clue in room.Clues)
                    roomClues.Add(clue.Id);

                foreach (var line in room.EnumerateAuthoredLines())
                {
                    if (line.PromptKind != DialoguePromptKind.ClueSelection)
                        continue;

                    if (line.RequiredClueIds.Count == 0)
                    {
                        issues.Add(new ScriptIssue(
                            ScriptIssueSeverity.Error,
                            $"방 {room.Id}의 대사 {line.Id}는 단서로 답하는 줄인데 정답 단서(RequiredClueIds)가 하나도 없다."));
                    }

                    foreach (var required in line.RequiredClueIds)
                    {
                        if (roomClues.Contains(required))
                            continue;

                        issues.Add(new ScriptIssue(
                            ScriptIssueSeverity.Error,
                            $"방 {room.Id}의 대사 {line.Id}가 정답 단서로 {required}를 요구하지만, " +
                            "그 방에 그런 단서가 없어 정답을 낼 방법이 없다."));
                    }

                    if (!line.CorrectNext.HasValue || !line.IncorrectNext.HasValue)
                    {
                        issues.Add(new ScriptIssue(
                            ScriptIssueSeverity.Error,
                            $"방 {room.Id}의 대사 {line.Id}는 단서로 답하는 줄인데 정답/오답 분기가 지정되지 않았다."));
                    }
                }
            }

            return issues;
        }
    }
}
