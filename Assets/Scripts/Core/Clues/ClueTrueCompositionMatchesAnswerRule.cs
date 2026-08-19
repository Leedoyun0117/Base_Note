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
    //
    // 이 규칙은 저작 시점의 배치만 검사한다. 플레이어가 단서를 다른 방에
    // 버리면 그 단서는 그 방 소속이 되므로, 여기서 "정답과 어울린다"고 판정한
    // 짝짓기가 플레이 중에는 얼마든지 어긋날 수 있다 — 재배정된 단서에는 이
    // 검사가 다시 돌지 않는다(그럴 자리 자체가 없다). 그래서 이 경고는
    // "기획 데이터가 처음부터 이상하지는 않은가"를 봐주는 저작 도구일 뿐이고,
    // 런타임 규칙으로 승격시켜서는 안 된다.
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
