using System.Collections.Generic;
using GameName.Core.Dialogue;
using GameName.Core.Memories;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 같은 파싱 결과 하나가 해금 상태에 따라 다른 문자열이 된다는 것,
    // 푸는 단위가 색이 아니라 키라는 것, 그리고 가려진 자리에 들어가는 글자가
    // 렌더러 밖에서 온다는 것을 고정한다.
    public class CensorRendererTests
    {
        private sealed class FakeResolver : ICensorResolver
        {
            private readonly HashSet<CensorKey> _revealed = new HashSet<CensorKey>();

            public void Reveal(string key) => _revealed.Add(new CensorKey(key));
            public bool IsRevealed(CensorKey key) => _revealed.Contains(key);
        }

        // 실제 표기는 저작 데이터에서 오므로, 테스트는 "무엇이 들어왔는지"만
        // 보이면 된다. 색 이름을 여기 적지 않는 것이 그 사실을 드러낸다.
        private sealed class FakeMaskFormatter : ICensorMaskFormatter
        {
            public string FormatMask(MemoryColor color) => $"<{color}>";
        }

        private const string Authored =
            "우리가 [[B:beach-house:해변의 작은 집]]에서 보냈던 [[R:that-summer:그 여름]]이 그리워.";

        private static CensoredText Parse(string authored) => new CensoredTextParser().Parse(authored);

        [Test]
        public void 풀리지_않은_구간은_그_구간의_색_표기로_덮인다()
        {
            var renderer = new CensorRenderer(new FakeResolver(), new FakeMaskFormatter());

            Assert.AreEqual(
                "우리가 <Blue>에서 보냈던 <Red>이 그리워.",
                renderer.Render(Parse(Authored)));
        }

        [Test]
        public void 풀린_키의_구간만_원문으로_돌아온다()
        {
            var resolver = new FakeResolver();
            resolver.Reveal("beach-house");

            var renderer = new CensorRenderer(resolver, new FakeMaskFormatter());

            Assert.AreEqual(
                "우리가 해변의 작은 집에서 보냈던 <Red>이 그리워.",
                renderer.Render(Parse(Authored)));
        }

        [Test]
        public void 같은_키를_쓰는_구간은_문장이_달라도_함께_풀린다()
        {
            var resolver = new FakeResolver();
            resolver.Reveal("beach-house");

            var renderer = new CensorRenderer(resolver, new FakeMaskFormatter());

            Assert.AreEqual(
                "그 집이라고 부르던 해변의 작은 집",
                renderer.Render(Parse("[[B:beach-house:그 집]]이라고 부르던 [[B:beach-house:해변의 작은 집]]")));
        }

        [Test]
        public void 같은_색이라도_키가_다르면_따로_풀린다()
        {
            // 키를 두는 이유가 그대로 드러나는 자리다. 색으로 풀었다면 파란
            // 단서 하나에 두 사실이 함께 열렸을 것이다.
            var resolver = new FakeResolver();
            resolver.Reveal("beach-house");

            var renderer = new CensorRenderer(resolver, new FakeMaskFormatter());

            Assert.AreEqual(
                "해변의 작은 집과 <Blue>",
                renderer.Render(Parse("[[B:beach-house:해변의 작은 집]]과 [[B:the-ring:그 반지]]")));
        }

        [Test]
        public void 같은_파싱_결과를_해금_상태만_바꿔_다시_그릴_수_있다()
        {
            var resolver = new FakeResolver();
            var renderer = new CensorRenderer(resolver, new FakeMaskFormatter());
            var parsed = Parse(Authored);

            var before = renderer.Render(parsed);
            resolver.Reveal("beach-house");
            resolver.Reveal("that-summer");
            var after = renderer.Render(parsed);

            Assert.AreNotEqual(before, after);
            Assert.AreEqual("우리가 해변의 작은 집에서 보냈던 그 여름이 그리워.", after);
        }

        [Test]
        public void 검열이_없는_대사는_해금_상태와_무관하게_그대로다()
        {
            var renderer = new CensorRenderer(new FakeResolver(), new FakeMaskFormatter());

            Assert.AreEqual("그냥 평범한 말이다.", renderer.Render(Parse("그냥 평범한 말이다.")));
        }
    }
}
