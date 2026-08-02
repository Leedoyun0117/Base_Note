using GameName.Core.Judging;

namespace GameName.UI.Shared
{
    // 시향 피드백 단계를 숫자/코드가 아니라 악기 이름으로 보여주기 위한 순수
    // 표시용 매핑. 기록지 화면이 판정 결과를 플레이어에게 보여줄 때 쓴다.
    internal static class FeedbackStageDisplay
    {
        public static string Label(FeedbackStage stage)
        {
            switch (stage)
            {
                case FeedbackStage.Silence: return "무반응";
                case FeedbackStage.Piano: return "피아노";
                case FeedbackStage.PianoAndViolin: return "피아노 + 바이올린";
                case FeedbackStage.PianoAndViolinAndDrum: return "피아노 + 바이올린 + 드럼";
                default: return stage.ToString();
            }
        }
    }
}
