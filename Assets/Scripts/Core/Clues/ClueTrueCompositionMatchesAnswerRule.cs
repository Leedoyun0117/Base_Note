using System.Collections.Generic;

namespace GameName.Core.Clues
{
    // 단서의 "실제" 감정 구성(TrueComposition)에 정답(바탕 감정 + 보조 배분)에
    // 없는 감정이 등장하면 의심스러운 데이터다. ApparentComposition이 아니라
    // TrueComposition을 검사하는 이유는, 거짓 단서는 설계상 겉보기 구성이
    // 실제와 달라도 되기 때문이다(그게 거짓말의 정의다) — 검사 대상은 항상
    // "사실"이어야 한다.
    // 다만 이런 단서를 완전히 금지할지는 기획에서 아직 정하지 않았으므로
    // 오류가 아니라 경고로 남긴다.
    public sealed class ClueTrueCompositionMatchesAnswerRule : IRoomDataConsistencyRule
    {
        public IReadOnlyList<RoomDataIssue> Check(MemoryRoomData data)
        {
            var issues = new List<RoomDataIssue>();
            var answerScent = data.Answer.CorrectScent;

            foreach (var clue in data.Clues)
            {
                foreach (var emotion in clue.TrueComposition.Emotions)
                {
                    var belongsToAnswer =
                        emotion == answerScent.BaseEmotion || answerScent.SupportingBlend.Contains(emotion);

                    if (!belongsToAnswer)
                    {
                        issues.Add(new RoomDataIssue(
                            RoomDataIssueSeverity.Warning,
                            $"단서 {clue.Id}의 실제 구성에 정답에 없는 감정({emotion})이 등장한다."));
                    }
                }
            }

            return issues;
        }
    }
}
