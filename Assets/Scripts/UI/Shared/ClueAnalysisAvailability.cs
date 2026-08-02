using GameName.Core.Analysis;

namespace GameName.UI.Shared
{
    // 분석 버튼을 미리 활성/비활성으로 보여주기 위한 순수 판정.
    // ClueAnalyzer.Analyze의 실제 규칙(depth <= previousDepth면 거부)과 정확히
    // 같은 결과를 내야 하지만, 최종 판단은 여전히 Analyze() 호출 자체가 한다 —
    // 이 판정은 화면이 미리 보여주는 용도일 뿐, 규칙의 두 번째 원본이 아니다.
    public static class ClueAnalysisAvailability
    {
        public static bool IsBasicAvailable(AnalysisDepth? bestDepth) => bestDepth == null;

        public static bool IsAdvancedAvailable(AnalysisDepth? bestDepth) =>
            bestDepth == null || bestDepth.Value != AnalysisDepth.Advanced;
    }
}
