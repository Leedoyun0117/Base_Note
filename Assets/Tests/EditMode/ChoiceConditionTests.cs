using GameName.Core.Clues;
using GameName.Core.Dialogue;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 선택지 조건이 어느 축으로 열리는지를 타입 수준에서 고정하는 테스트.
    //
    // 이 파일이 있는 이유는 이름 하나 때문이다. 검열이 색으로 풀리던 시절의
    // 이름(ColorRevealed)이 남아 있으면 "선택지는 색으로 열리고 대사는 키로
    // 열리는가"라는 혼동이 반드시 되살아난다. 조건이 CensorKey를 든다는 것이
    // 컴파일되지 않으면 이 테스트가 먼저 깨진다.
    public class ChoiceConditionTests
    {
        [Test]
        public void 조건이_없으면_아무것도_요구하지_않는다()
        {
            var condition = ChoiceCondition.None;

            Assert.AreEqual(ChoiceConditionKind.None, condition.Kind);
            Assert.IsNull(condition.RequiredCensorKey);
            Assert.IsNull(condition.RequiredClue);
        }

        [Test]
        public void 검열_조건은_색이_아니라_키를_든다()
        {
            var condition = ChoiceCondition.RequiresCensorKeyRevealed(new CensorKey("beach-house"));

            Assert.AreEqual(ChoiceConditionKind.CensorKeyRevealed, condition.Kind);
            Assert.AreEqual(new CensorKey("beach-house"), condition.RequiredCensorKey);
            Assert.IsNull(condition.RequiredClue);
        }

        [Test]
        public void 단서_사용_조건은_검열과_별개의_축이다()
        {
            var condition = ChoiceCondition.ClueUsed(new ClueId("clue-1"));

            Assert.AreEqual(ChoiceConditionKind.ClueUsed, condition.Kind);
            Assert.AreEqual(new ClueId("clue-1"), condition.RequiredClue);
            Assert.IsNull(condition.RequiredCensorKey);
        }

        [Test]
        public void 검열_조건은_대사와_똑같은_판정_경계로_답할_수_있다()
        {
            // 조건이 든 키를 ICensorResolver에 그대로 넘길 수 있다는 것이
            // "판정을 재사용한다"의 실제 의미다. 조건 전용 판정을 따로 두면
            // 대사에서는 이미 드러난 사실인데 선택지만 잠겨 있는 어긋남이 생긴다.
            var condition = ChoiceCondition.RequiresCensorKeyRevealed(new CensorKey("beach-house"));
            var resolver = new FakeResolver("beach-house");

            Assert.IsTrue(resolver.IsRevealed(condition.RequiredCensorKey.Value));
        }

        private sealed class FakeResolver : ICensorResolver
        {
            private readonly CensorKey _revealed;

            public FakeResolver(string revealed) => _revealed = new CensorKey(revealed);

            public bool IsRevealed(CensorKey key) => key == _revealed;
        }
    }
}
