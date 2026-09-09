using GameName.Core.Clues;
using GameName.Core.Dialogue;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 선택지 조건이 어느 축으로 열리는지를 타입 수준에서 고정하는 테스트.
    public class ChoiceConditionTests
    {
        [Test]
        public void 조건이_없으면_아무것도_요구하지_않는다()
        {
            var condition = ChoiceCondition.None;

            Assert.AreEqual(ChoiceConditionKind.None, condition.Kind);
            Assert.IsNull(condition.RequiredClue);
        }

        [Test]
        public void 단서_사용_조건은_그_단서_id를_든다()
        {
            var condition = ChoiceCondition.ClueUsed(new ClueId("clue-1"));

            Assert.AreEqual(ChoiceConditionKind.ClueUsed, condition.Kind);
            Assert.AreEqual(new ClueId("clue-1"), condition.RequiredClue);
        }
    }
}
