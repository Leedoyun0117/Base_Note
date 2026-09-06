using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Dialogue;

namespace GameName.Core.Authoring
{
    // ClueSelection 줄의 저작이 성립하는지 본다.
    //
    // 이 줄은 텍스트 선택지 대신 "가진 단서로 답하라"이고, 정답 판정을
    // RequiredTags에 기댄다. 그것이 비어 있거나 그 시점까지 얻을 수 있는 어떤
    // 단서도 갖지 않은 태그를 가리키면 어떤 단서를 골라도 오답이 되어 플레이어가
    // 이유를 알 수 없이 막힌다 — 저작 시점이 아니면 잡을 곳이 없다.
    //
    // 방 0..i 누적으로 보는 이유: 손에 든 단서(Collected)는 방이 바뀌어도 비우지
    // 않으므로(InventoryProjection), 방 i의 이 줄은 방 0..i 어디서 주운 단서로도
    // 답할 수 있다. 방 i 단서만 인정하면 앞 방 단서로 답하도록 저작한 정상적인
    // 줄이 오류로 잡힌다.
    //
    // CorrectNext/IncorrectNext가 실재하는 라인인지는 NextLineExistsRule이 본다.
    // 오답 서브체인이 결국 메인 줄기로 합류하는지는 자동으로 보기 어려워 수동
    // 확인 대상으로 남긴다.
    public sealed class ClueSelectionLineRule : IDialogueScriptRule
    {
        public IReadOnlyList<ScriptIssue> Check(RunDefinition run)
        {
            var issues = new List<ScriptIssue>();

            // 그 방 라인을 보기 전에 그 방 단서까지 누적한다 — 같은 방에서 주운
            // 단서로 그 방 줄에 답하는 것이 기본이므로 포함이 맞다.
            var tagsSoFar = new HashSet<ClueTag>();

            foreach (var room in run.Rooms)
            {
                foreach (var clue in room.Clues)
                foreach (var tag in clue.Tags)
                    tagsSoFar.Add(tag);

                foreach (var line in room.EnumerateAuthoredLines())
                {
                    if (line.PromptKind != DialoguePromptKind.ClueSelection)
                        continue;

                    if (line.RequiredTags.Count == 0)
                    {
                        issues.Add(new ScriptIssue(
                            ScriptIssueSeverity.Error,
                            $"방 {room.Id}의 대사 {line.Id}는 단서로 답하는 줄인데 정답 태그(RequiredTags)가 하나도 없다."));
                    }

                    foreach (var required in line.RequiredTags)
                    {
                        if (tagsSoFar.Contains(required))
                            continue;

                        issues.Add(new ScriptIssue(
                            ScriptIssueSeverity.Error,
                            $"방 {room.Id}의 대사 {line.Id}가 정답 태그로 {required}를 요구하지만, " +
                            "그 방까지 얻을 수 있는 어떤 단서도 이 태그를 갖지 않아 정답을 낼 방법이 없다."));
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
