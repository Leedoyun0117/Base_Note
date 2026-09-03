using System.Linq;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 검열을 보는 규칙이 셋으로 늘어난 지금, 그 셋이 원문을 각자 다시 파싱하지
    // 않는다는 것을 고정하는 테스트. 이것이 깨지면 규칙을 하나 더할 때마다 저작
    // 데이터 전체를 한 번씩 더 훑게 된다.
    public class CensorTokenIndexSourceTests
    {
        private sealed class CountingParser : ICensoredTextParser
        {
            private readonly CensoredTextParser _inner = new CensoredTextParser();

            public int Calls { get; private set; }

            public CensoredText Parse(string authoredText)
            {
                Calls++;
                return _inner.Parse(authoredText);
            }
        }

        private static RunDefinition MakeRun(string lineId) =>
            new RunDefinition(
                new[]
                {
                    new RoomDefinition(
                        new MemoryRoomId("room-1"),
                        new[]
                        {
                            new ClueDefinition(
                                new ClueId("clue-1"), ClueKind.Poster, "단서",
                                new CluePositionRatio(0.2f), MemoryColor.Blue),
                        },
                        new DialogueLineId(lineId),
                        new[]
                        {
                            new DialogueLineDefinition(
                                new DialogueLineId(lineId), "화자",
                                "[[B:beach-house:해변의 작은 집]]",
                                new[]
                                {
                                    new ChoiceDefinition(
                                        new ChoiceId("choice-1"),
                                        "[[B:the-ring:그 반지]] 이야기를 꺼낸다",
                                        true, null, ChoiceCondition.None),
                                }),
                        }),
                },
                startingTrust: 50,
                extractionBudget: 5);

        [Test]
        public void 같은_판을_여러_번_물어도_원문은_한_번만_파싱된다()
        {
            var parser = new CountingParser();
            var source = new CensorTokenIndexSource(parser);
            var run = MakeRun("line-1");

            source.For(run);
            var callsAfterFirst = parser.Calls;
            source.For(run);
            source.For(run);

            Assert.AreEqual(callsAfterFirst, parser.Calls);
        }

        [Test]
        public void 다른_판을_물으면_새로_훑는다()
        {
            var parser = new CountingParser();
            var source = new CensorTokenIndexSource(parser);

            source.For(MakeRun("line-1"));
            var callsAfterFirst = parser.Calls;
            source.For(MakeRun("line-2"));

            Assert.AreEqual(callsAfterFirst * 2, parser.Calls);
        }

        [Test]
        public void 대사와_선택지_양쪽의_토큰을_모은다()
        {
            var index = new CensorTokenIndexSource(new CensoredTextParser()).For(MakeRun("line-1"));

            CollectionAssert.AreEquivalent(
                new[] { new CensorKey("beach-house"), new CensorKey("the-ring") },
                index.Uses.Select(u => u.Key).ToArray());

            Assert.IsTrue(index.Contains(new CensorKey("the-ring")));
            Assert.IsFalse(index.Contains(new CensorKey("없는-키")));
        }

        [Test]
        public void 토큰마다_어느_방에서_쓰였는지를_함께_든다()
        {
            var index = new CensorTokenIndexSource(new CensoredTextParser()).For(MakeRun("line-1"));

            // 방 단위로 판단하는 규칙(그 방 단서가 이 색을 내주는가)이 사람이
            // 읽는 문장에서 방을 도로 파내지 않아도 되는 이유가 이 값이다.
            Assert.IsTrue(index.Uses.All(u => u.RoomId == new MemoryRoomId("room-1")));
        }
    }
}
