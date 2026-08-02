using System.Collections.Generic;

namespace GameName.Core.Clues
{
    // 정답 향의 보조 감정 배분에 세기가 0 이하인 항목이 섞여 있으면 명백한
    // 오류다. EmotionBlendEntry는 구조체라 default 값(세기 0)이 정상적인
    // 생성자 검증을 우회해 배열에 섞여 들어올 수 있다 — 배합 검증기가
    // 플레이어 입력만 검사하고 정답 데이터는 검사하지 않으므로, 정답 데이터
    // 단계에서 이 구멍을 다시 한 번 막아 둔다.
    public sealed class AnswerHasNoZeroIntensityRule : IRoomDataConsistencyRule
    {
        public IReadOnlyList<RoomDataIssue> Check(MemoryRoomData data)
        {
            var issues = new List<RoomDataIssue>();

            foreach (var entry in data.Answer.CorrectScent.SupportingBlend.Entries)
            {
                if (entry.Intensity <= 0)
                {
                    issues.Add(new RoomDataIssue(
                        RoomDataIssueSeverity.Error,
                        $"정답 향의 {entry.Emotion} 세기가 {entry.Intensity}로, 0 이하다."));
                }
            }

            return issues;
        }
    }
}
