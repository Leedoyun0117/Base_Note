using GameName.Core.Analysis;
using GameName.UI.Shared;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    public class ClueAnalysisAvailabilityTests
    {
        [Test]
        public void 분석한_적_없고_정신력이_충분하면_일반과_고급_모두_가능하다()
        {
            Assert.IsTrue(ClueAnalysisAvailability.IsBasicAvailable(null, canAfford: true));
            Assert.IsTrue(ClueAnalysisAvailability.IsAdvancedAvailable(null, canAfford: true));
        }

        [Test]
        public void 일반_분석_후에는_일반이_막히고_고급은_허용된다()
        {
            Assert.IsFalse(ClueAnalysisAvailability.IsBasicAvailable(AnalysisDepth.Basic, canAfford: true));
            Assert.IsTrue(ClueAnalysisAvailability.IsAdvancedAvailable(AnalysisDepth.Basic, canAfford: true));
        }

        [Test]
        public void 고급_분석_후에는_일반과_고급_모두_막힌다()
        {
            Assert.IsFalse(ClueAnalysisAvailability.IsBasicAvailable(AnalysisDepth.Advanced, canAfford: true));
            Assert.IsFalse(ClueAnalysisAvailability.IsAdvancedAvailable(AnalysisDepth.Advanced, canAfford: true));
        }

        [Test]
        public void 분석한_적_없어도_정신력이_부족하면_일반과_고급_모두_막힌다()
        {
            Assert.IsFalse(ClueAnalysisAvailability.IsBasicAvailable(null, canAfford: false));
            Assert.IsFalse(ClueAnalysisAvailability.IsAdvancedAvailable(null, canAfford: false));
        }
    }
}
