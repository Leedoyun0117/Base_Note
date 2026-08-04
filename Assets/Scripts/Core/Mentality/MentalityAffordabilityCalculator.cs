namespace GameName.Core.Mentality
{
    // MentalityAffordability 계산 — ClueAnalyzer.Analyze/AmpouleCraftingProcessor.Craft가
    // 실제로 쓰는 판정(CanAct부터 확인하고, 그다음 비용을 감당할 수 있는지 본다)과
    // 정확히 같은 조건을 화면이 미리 보여줄 수 있도록 한 곳에 모았다. 이 계산
    // 자체가 최종 판단은 아니다 — 실제 소모/차단은 여전히 각 처리기가 한다.
    public static class MentalityAffordabilityCalculator
    {
        public static MentalityAffordability Calculate(IMentalityGauge gauge, IMentalityCostSettings settings)
        {
            bool CanAfford(int cost) => gauge.CanAct && gauge.CurrentValue >= cost;

            return new MentalityAffordability(
                canBasicAnalyze: CanAfford(settings.BasicAnalysisCost),
                canAdvancedAnalyze: CanAfford(settings.AdvancedAnalysisCost),
                canCraftAmpoule: CanAfford(settings.AmpouleCraftingCost));
        }
    }
}
