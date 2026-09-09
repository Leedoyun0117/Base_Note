using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.UI.Session;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameName.UI.Tests.EditMode
{
    // "모든 최적 선택으로 완주했는가"를 콘솔에 알리는 테스트용 관찰자.
    //   · 텍스트 선택지 오답 → TrustChangedEvent 감소
    //   · ClueSelection 답이 완전적합이 아님 → ClueAnsweredEvent.Grade != Exact
    // 둘 중 하나라도 있었으면 "클리어"가 아니다.
    public class PerfectRunReporterTests
    {
        private static EventBus NewBusWithReporter()
        {
            var bus = new EventBus(new NoOpEventExceptionHandler());
            _ = new PerfectRunReporter(bus);
            return bus;
        }

        private const string Clear = "[TEST] 클리어 — 모든 최적 선택으로 완주했습니다.";
        private const string NotClear = "[TEST] 완주 — 최적 선택은 아니었습니다.";

        [Test]
        public void 흠집_없이_완주하면_클리어를_찍는다()
        {
            var bus = NewBusWithReporter();

            LogAssert.Expect(LogType.Log, Clear);
            bus.Publish(new RunCompletedEvent());
        }

        [Test]
        public void 신뢰가_한_번이라도_깎였으면_클리어가_아니다()
        {
            var bus = NewBusWithReporter();
            bus.Publish(new TrustChangedEvent(3, 2));

            LogAssert.Expect(LogType.Log, NotClear);
            bus.Publish(new RunCompletedEvent());
        }

        [Test]
        public void 단서_답이_완전적합이_아니었으면_클리어가_아니다()
        {
            var bus = NewBusWithReporter();
            bus.Publish(new ClueAnsweredEvent(MatchGrade.Partial));

            LogAssert.Expect(LogType.Log, NotClear);
            bus.Publish(new RunCompletedEvent());
        }

        [Test]
        public void 신뢰_상승이나_완전적합_단서는_흠집이_아니다()
        {
            var bus = NewBusWithReporter();
            bus.Publish(new TrustChangedEvent(2, 3)); // 방 전환 리셋 등
            bus.Publish(new ClueAnsweredEvent(MatchGrade.Exact));

            LogAssert.Expect(LogType.Log, Clear);
            bus.Publish(new RunCompletedEvent());
        }
    }
}
