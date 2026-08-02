namespace GameName.UI.Shared
{
    // 분석 버튼에 비용을 함께 보여주기 위한 순수 표시용 포맷터. 값은 항상
    // 호출부가 IMentalityCostSettings에서 읽어 넘긴다 — 이 타입은 숫자를
    // 스스로 알지 못한다(매직 넘버 금지).
    public static class AnalysisCostLabel
    {
        public static string Basic(int cost) => $"일반 분석 (정신력 {cost})";
        public static string Advanced(int cost) => $"고급 분석 (정신력 {cost})";
    }
}
