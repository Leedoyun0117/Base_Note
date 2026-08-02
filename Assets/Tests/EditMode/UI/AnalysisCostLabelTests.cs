using GameName.UI.Shared;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    public class AnalysisCostLabelTests
    {
        [Test]
        public void 일반_분석_라벨은_전달받은_비용을_그대로_보여준다()
        {
            Assert.AreEqual("일반 분석 (정신력 20)", AnalysisCostLabel.Basic(20));
        }

        [Test]
        public void 고급_분석_라벨은_전달받은_비용을_그대로_보여준다()
        {
            Assert.AreEqual("고급 분석 (정신력 30)", AnalysisCostLabel.Advanced(30));
        }
    }
}
