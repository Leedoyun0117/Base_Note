using System.Linq;
using GameName.Core.Dialogue;
using GameName.Core.Memories;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 파서가 문법만 알고 해금은 모른다는 것, 그리고 저작 오타에 게임이 멈추지
    // 않는다는 것을 고정하는 테스트.
    public class CensoredTextParserTests
    {
        private CensoredTextParser _parser;

        [SetUp]
        public void SetUp() => _parser = new CensoredTextParser();

        [Test]
        public void 토큰이_없는_원문은_조각_하나로_남는다()
        {
            var parsed = _parser.Parse("그냥 평범한 말이다.");

            Assert.AreEqual(1, parsed.Segments.Count);
            Assert.IsFalse(parsed.Segments[0].IsCensored);
            Assert.AreEqual("그냥 평범한 말이다.", parsed.Segments[0].Text);
        }

        [Test]
        public void 검열_토큰은_색과_키와_원문을_들고_따로_잘린다()
        {
            var parsed = _parser.Parse("우리가 [[B:beach-house:해변의 작은 집]]에서 보냈던 시절이 그리워.");

            Assert.AreEqual(3, parsed.Segments.Count);
            Assert.AreEqual("우리가 ", parsed.Segments[0].Text);

            Assert.IsTrue(parsed.Segments[1].IsCensored);
            Assert.AreEqual(MemoryColor.Blue, parsed.Segments[1].Color);
            Assert.AreEqual(new CensorKey("beach-house"), parsed.Segments[1].Key);
            Assert.AreEqual("해변의 작은 집", parsed.Segments[1].Text);

            Assert.AreEqual("에서 보냈던 시절이 그리워.", parsed.Segments[2].Text);
        }

        [Test]
        public void 가려지지_않은_조각은_색도_키도_들지_않는다()
        {
            var plain = _parser.Parse("그냥 평범한 말이다.").Segments[0];

            Assert.IsNull(plain.Color);
            Assert.IsNull(plain.Key);
        }

        [Test]
        public void 토큰이_문장_맨_앞이나_맨_뒤에_와도_빈_조각을_만들지_않는다()
        {
            var parsed = _parser.Parse("[[R:that-person:그 사람]]이 남긴 것은 [[G:this-letter:이 편지]]");

            CollectionAssert.AreEqual(
                new[] { true, false, true },
                parsed.Segments.Select(s => s.IsCensored).ToArray());
        }

        [Test]
        public void 토큰이_여러_개면_전부_따로_잘리고_필요한_색이_모인다()
        {
            var parsed = _parser.Parse(
                "[[R:that-summer:그 여름]]에 [[B:beach-house:그 집]]에서 [[R:that-person:그 사람]]을 만났다.");

            Assert.AreEqual(3, parsed.Segments.Count(s => s.IsCensored));

            // 같은 색이 두 번 나와도 필요한 색은 두 가지다 — 중복은 접힌다.
            CollectionAssert.AreEquivalent(
                new[] { MemoryColor.Red, MemoryColor.Blue }, parsed.RequiredColors);
        }

        [Test]
        public void 같은_키를_쓴_구간들은_문장이_달라도_같은_키를_들고_나온다()
        {
            // 표현이 다른 두 자리가 한 사실을 가리킬 수 있다는 것이 키를 두는
            // 이유다. 파서는 그 둘을 같은 키로 내보내기만 하고, 함께 푸는 것은
            // 렌더러의 일이다.
            var parsed = _parser.Parse("[[B:beach-house:그 집]]이라고 부르던 [[B:beach-house:해변의 작은 집]]");

            var keys = parsed.Segments.Where(s => s.IsCensored).Select(s => s.Key.Value).ToArray();

            Assert.AreEqual(2, keys.Length);
            Assert.AreEqual(keys[0], keys[1]);
        }

        [Test]
        public void 가려진_말_안의_콜론은_구분자로_보지_않는다()
        {
            var parsed = _parser.Parse("[[B:the-note:그날 적은 것: 잊지 말 것]]");

            var censored = parsed.Segments.Single(s => s.IsCensored);
            Assert.AreEqual(new CensorKey("the-note"), censored.Key);
            Assert.AreEqual("그날 적은 것: 잊지 말 것", censored.Text);
        }

        [Test]
        public void 중첩된_토큰은_안쪽까지_파고들지_않고_첫_닫힘에서_끊긴다()
        {
            var parsed = _parser.Parse("[[B:outer:바깥 [[R:inner:안쪽]]]]");

            var censored = parsed.Segments.Where(s => s.IsCensored).ToArray();
            Assert.AreEqual(1, censored.Length);
            Assert.AreEqual(MemoryColor.Blue, censored[0].Color);
            Assert.AreEqual(new CensorKey("outer"), censored[0].Key);
            Assert.AreEqual("바깥 [[R:inner:안쪽", censored[0].Text);
        }

        [TestCase("닫히지 않은 [[B:beach-house:토큰")]
        [TestCase("키가 빠진 [[B:해변의 작은 집]]")]
        [TestCase("키가 비어 있는 [[B::해변의 작은 집]]")]
        [TestCase("모르는 색 [[X:beach-house:해변의 작은 집]]")]
        [TestCase("가릴 말이 없는 [[B:beach-house:]]")]
        [TestCase("여는 표시만 [[ 있다")]
        public void 깨진_토큰은_예외가_아니라_글자_그대로_남는다(string authored)
        {
            var parsed = _parser.Parse(authored);

            Assert.IsFalse(parsed.Segments.Any(s => s.IsCensored));
            Assert.AreEqual(authored, string.Concat(parsed.Segments.Select(s => s.Text)));
        }

        [Test]
        public void 깨진_토큰_뒤에_오는_멀쩡한_토큰은_그대로_살아남는다()
        {
            var parsed = _parser.Parse("[[X:broken:못 읽는 것]] 다음에 [[G:this-letter:읽히는 것]]");

            var censored = parsed.Segments.Where(s => s.IsCensored).ToArray();
            Assert.AreEqual(1, censored.Length);
            Assert.AreEqual(MemoryColor.Green, censored[0].Color);
            Assert.AreEqual(new CensorKey("this-letter"), censored[0].Key);
            Assert.AreEqual("읽히는 것", censored[0].Text);
        }

        [TestCase((string)null)]
        [TestCase("")]
        public void 빈_원문은_조각이_없는_결과가_된다(string authored)
        {
            Assert.AreEqual(0, _parser.Parse(authored).Segments.Count);
        }
    }
}
