using System.Collections.Generic;
using GameName.Core.Complexes;
using GameName.Core.Events;
using GameName.Core.Mind;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 단서 해석이 끝나면 최종 태그의 감정 축만 보고 안정 축을 민다 — 원본
    // 태그가 아니라 컴플렉스 체인을 통과한 값이 기준이다.
    public class ClueInterpretationStabilityListenerTests
    {
        private static EventBus Bus() => new EventBus(new NoOpEventExceptionHandler());

        private static readonly Dictionary<string, int> Shifts = new Dictionary<string, int>
        {
            { "그리움", -3 },
            { "분노", 8 },
        };

        private static ClueInterpretedEvent Interpreted(params StoryTag[] finalTags) =>
            new ClueInterpretedEvent(finalTags, new List<ComplexChainStep>(), finalTags);

        [Test]
        public void 최종_감정_태그의_이동량_합만큼_축을_민다()
        {
            var bus = Bus();
            var axis = new StabilityAxis(0, -100, 100, bus);
            _ = new ClueInterpretationStabilityListener(axis, Shifts, bus);

            bus.Publish(Interpreted(
                StoryTag.Person("유키"), StoryTag.Emotion("분노"), StoryTag.Emotion("그리움")));

            Assert.AreEqual(5, axis.Position);
        }

        [Test]
        public void 표에_없는_감정은_0으로_친다()
        {
            var bus = Bus();
            var axis = new StabilityAxis(0, -100, 100, bus);
            _ = new ClueInterpretationStabilityListener(axis, Shifts, bus);

            bus.Publish(Interpreted(StoryTag.Emotion("무감각")));

            Assert.AreEqual(0, axis.Position);
        }

        [Test]
        public void 감정_축이_아닌_태그는_보지_않는다()
        {
            var bus = Bus();
            var axis = new StabilityAxis(0, -100, 100, bus);
            _ = new ClueInterpretationStabilityListener(axis, Shifts, bus);

            // Person 축에 "분노"라는 값이 있어도 감정 이동은 일어나지 않는다.
            bus.Publish(Interpreted(StoryTag.Person("분노")));

            Assert.AreEqual(0, axis.Position);
        }
    }
}
