using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Mind;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 태그만 보는 raw 등급 위에 심리 상태 × 안정 축을 얹는다. 자유 폭(20) 이내면
    // 왜곡이 없고, 넘어설수록 칸 폭(30)마다 한 칸씩(최대 두 칸) 밀리거나 뒤집힌다.
    public class MemoryEffectResolverTests
    {
        private sealed class FixedGrader : ITagMatchGrader
        {
            private readonly MatchGrade _grade;
            public FixedGrader(MatchGrade grade) => _grade = grade;
            public MatchGrade Grade(IReadOnlyList<ClueTag> q, IReadOnlyList<ClueTag> a) => _grade;
        }

        private static MatchGrade Resolve(MatchGrade raw, PsychologyState psych, int pos) =>
            new MemoryEffectResolver(new FixedGrader(raw), distortionFreeBand: 20, distortionStep: 30)
                .Resolve(psych, pos, Array.Empty<ClueTag>(), Array.Empty<ClueTag>());

        [Test]
        public void 안정_한복판이면_어떤_심리든_raw_등급_그대로다()
        {
            foreach (var psych in new[] { PsychologyState.Optimism, PsychologyState.Mania, PsychologyState.Melancholy })
            {
                Assert.AreEqual(MatchGrade.Partial, Resolve(MatchGrade.Partial, psych, 0));
                Assert.AreEqual(MatchGrade.Partial, Resolve(MatchGrade.Partial, psych, 20), "자유 폭 경계까지는 왜곡 없음");
            }
        }

        [Test]
        public void 낙관은_흔들릴수록_등급을_올리고_우울은_내린다()
        {
            // |pos| 50 → 초과 30 → 한 칸.
            Assert.AreEqual(MatchGrade.High, Resolve(MatchGrade.Partial, PsychologyState.Optimism, 50));
            Assert.AreEqual(MatchGrade.Partial, Resolve(MatchGrade.High, PsychologyState.Melancholy, -50));

            // |pos| 100 → 초과 80 → 두 칸(상한).
            Assert.AreEqual(MatchGrade.Partial, Resolve(MatchGrade.None, PsychologyState.Optimism, 100));
            Assert.AreEqual(MatchGrade.None, Resolve(MatchGrade.Partial, PsychologyState.Melancholy, 100));
        }

        [Test]
        public void 광기는_흔들리면_등급을_뒤집는다()
        {
            Assert.AreEqual(MatchGrade.None, Resolve(MatchGrade.Exact, PsychologyState.Mania, 50));
            Assert.AreEqual(MatchGrade.Low, Resolve(MatchGrade.High, PsychologyState.Mania, 50));
            Assert.AreEqual(MatchGrade.Partial, Resolve(MatchGrade.Partial, PsychologyState.Mania, 50), "가운데는 뒤집어도 제자리");
            Assert.AreEqual(MatchGrade.Exact, Resolve(MatchGrade.None, PsychologyState.Mania, -80));
        }

        [Test]
        public void 광기도_안정_상태면_뒤집지_않는다()
        {
            Assert.AreEqual(MatchGrade.Exact, Resolve(MatchGrade.Exact, PsychologyState.Mania, 15));
        }

        [Test]
        public void 등급은_None_Exact_범위로_잘린다()
        {
            Assert.AreEqual(MatchGrade.Exact, Resolve(MatchGrade.High, PsychologyState.Optimism, 100));
            Assert.AreEqual(MatchGrade.None, Resolve(MatchGrade.Low, PsychologyState.Melancholy, 100));
        }
    }
}
