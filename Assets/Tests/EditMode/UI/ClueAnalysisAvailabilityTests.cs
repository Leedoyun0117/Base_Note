using GameName.Core.Analysis;
using GameName.UI.Shared;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    public class ClueAnalysisAvailabilityTests
    {
        [Test]
        public void 분석한_적_없으면_일반과_고급_모두_가능하다()
        {
            Assert.IsTrue(ClueAnalysisAvailability.IsBasicAvailable(null));
            Assert.IsTrue(ClueAnalysisAvailability.IsAdvancedAvailable(null));
        }

        [Test]
        public void 일반_분석_후에는_일반이_막히고_고급은_허용된다()
        {
            Assert.IsFalse(ClueAnalysisAvailability.IsBasicAvailable(AnalysisDepth.Basic));
            Assert.IsTrue(ClueAnalysisAvailability.IsAdvancedAvailable(AnalysisDepth.Basic));
        }

        [Test]
        public void 고급_분석_후에는_일반과_고급_모두_막힌다()
        {
            Assert.IsFalse(ClueAnalysisAvailability.IsBasicAvailable(AnalysisDepth.Advanced));
            Assert.IsFalse(ClueAnalysisAvailability.IsAdvancedAvailable(AnalysisDepth.Advanced));
        }
    }
}
