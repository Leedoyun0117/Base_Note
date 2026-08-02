using GameName.Core.Commissions;
using GameName.Core.MemoryRooms;
using GameName.UI.Flow;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    public class ActiveScreenSelectorTests
    {
        private static readonly MemoryGraphNodeId Perfumery = new MemoryGraphNodeId("perfumery-room");
        private static readonly MemoryGraphNodeId AnalysisRoom = new MemoryGraphNodeId("analysis-room");
        private static readonly MemoryGraphNodeId MemoryRoom = new MemoryGraphNodeId("room-1");

        [Test]
        public void 대화_중이면_아무_화면도_고르지_않는다()
        {
            var result = ActiveScreenSelector.Select(CommissionStage.PreConversation, MemoryRoom, Perfumery, AnalysisRoom);

            Assert.AreEqual(ActiveScreen.None, result);
        }

        [Test]
        public void 현실로_복귀한_뒤에는_최종_조향_화면을_고른다()
        {
            var result = ActiveScreenSelector.Select(CommissionStage.ReturnedToReality, MemoryRoom, Perfumery, AnalysisRoom);

            Assert.AreEqual(ActiveScreen.FinalCrafting, result);
        }

        [Test]
        public void 완료된_뒤에는_완료_화면을_고른다()
        {
            var result = ActiveScreenSelector.Select(CommissionStage.Completed, MemoryRoom, Perfumery, AnalysisRoom);

            Assert.AreEqual(ActiveScreen.Completion, result);
        }

        [Test]
        public void 조향실_위치면_조향실_화면을_고른다()
        {
            var result = ActiveScreenSelector.Select(CommissionStage.InMemory, Perfumery, Perfumery, AnalysisRoom);

            Assert.AreEqual(ActiveScreen.Perfumery, result);
        }

        [Test]
        public void 분석실_위치면_분석실_화면을_고른다()
        {
            var result = ActiveScreenSelector.Select(CommissionStage.InMemory, AnalysisRoom, Perfumery, AnalysisRoom);

            Assert.AreEqual(ActiveScreen.AnalysisRoom, result);
        }

        [Test]
        public void 그_외_위치면_기억_방_화면을_고른다()
        {
            var result = ActiveScreenSelector.Select(CommissionStage.InMemory, MemoryRoom, Perfumery, AnalysisRoom);

            Assert.AreEqual(ActiveScreen.MemoryRoom, result);
        }
    }
}
