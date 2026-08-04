using GameName.Core.Events;
using GameName.Core.Mentality;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 잔량 표시(비활성화 판단)가 IMentalityCostSettings 값만으로 결정되는지
    // 검증한다 — 매직 넘버가 아니라 설정을 그대로 읽는지가 핵심이다.
    public class MentalityAffordabilityCalculatorTests
    {
        private static MentalityGauge MakeGauge(
            int initialMentality, int basicCost, int advancedCost, int craftCost, out MentalityCostSettings settings)
        {
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            settings = new MentalityCostSettings(
                initialMentality: initialMentality, maxMentality: 100,
                memoryRoomMoveCost: 1, basicAnalysisCost: basicCost, advancedAnalysisCost: advancedCost,
                ampouleCraftingCost: craftCost, memoryRoomFullRestorationRecovery: 20);
            return new MentalityGauge(settings, eventBus);
        }

        [Test]
        public void 잔량이_모든_비용_이상이면_전부_가능하다()
        {
            var gauge = MakeGauge(100, 20, 30, 5, out var settings);

            var affordability = MentalityAffordabilityCalculator.Calculate(gauge, settings);

            Assert.IsTrue(affordability.CanBasicAnalyze);
            Assert.IsTrue(affordability.CanAdvancedAnalyze);
            Assert.IsTrue(affordability.CanCraftAmpoule);
        }

        [Test]
        public void 잔량이_조향_비용보다_작으면_조향만_불가능하다()
        {
            var gauge = MakeGauge(100, 20, 30, 5, out var settings);
            gauge.Consume(97); // 잔량 3

            var affordability = MentalityAffordabilityCalculator.Calculate(gauge, settings);

            Assert.IsFalse(affordability.CanBasicAnalyze);
            Assert.IsFalse(affordability.CanAdvancedAnalyze);
            Assert.IsFalse(affordability.CanCraftAmpoule);
        }

        [Test]
        public void 잔량이_조향_비용과_같으면_조향은_가능하고_분석은_불가능하다()
        {
            var gauge = MakeGauge(100, 20, 30, 5, out var settings);
            gauge.Consume(95); // 잔량 5

            var affordability = MentalityAffordabilityCalculator.Calculate(gauge, settings);

            Assert.IsFalse(affordability.CanBasicAnalyze);
            Assert.IsFalse(affordability.CanAdvancedAnalyze);
            Assert.IsTrue(affordability.CanCraftAmpoule);
        }

        [Test]
        public void 정신력이_0이면_비용이_0으로_조정되어도_전부_불가능하다()
        {
            // CanAct(0보다 큰가) 확인이 비용 조건과 별개로 항상 적용된다는
            // 규칙(ClueAnalyzer/AmpouleCraftingProcessor와 동일)을 검증한다.
            var gauge = MakeGauge(0, 0, 0, 0, out var settings);

            var affordability = MentalityAffordabilityCalculator.Calculate(gauge, settings);

            Assert.IsFalse(affordability.CanBasicAnalyze);
            Assert.IsFalse(affordability.CanAdvancedAnalyze);
            Assert.IsFalse(affordability.CanCraftAmpoule);
        }
    }
}
