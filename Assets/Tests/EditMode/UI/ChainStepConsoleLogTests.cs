using System.Collections.Generic;
using GameName.Core.Complexes;
using GameName.Core.Events;
using GameName.UI.MemoryRoom;
using NUnit.Framework;
using UnityEngine;

namespace GameName.UI.Tests.EditMode
{
    // [해석] 콘솔 로그가 최종 태그에 감정 축이 없을 때(거부/삭제로 사라졌을
    // 때) 경고 줄을 덧붙이는지 — 라운드3 거부(complex-deny)처럼 안정 축이
    // 조용히 안 움직이는 상황을 콘솔에서 바로 알아챌 수 있어야 한다.
    public class ChainStepConsoleLogTests
    {
        private static EventBus Bus() => new EventBus(new NoOpEventExceptionHandler());

        private static ClueInterpretedEvent Interpreted(params StoryTag[] finalTags) =>
            new ClueInterpretedEvent(finalTags, new List<ComplexChainStep>(), finalTags);

        private static string CaptureLog(EventBus bus, ClueInterpretedEvent e)
        {
            string captured = null;
            void OnLog(string message, string stackTrace, LogType type) => captured = message;

            Application.logMessageReceived += OnLog;
            try
            {
                using (new ChainStepConsoleLog(bus))
                    bus.Publish(e);
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
            }

            return captured;
        }

        [Test]
        public void 최종_태그에_감정_축이_없으면_경고_줄을_찍는다()
        {
            var log = CaptureLog(Bus(), Interpreted(StoryTag.Person("유키")));

            StringAssert.Contains("안정 축 이동 없음", log);
        }

        [Test]
        public void 최종_태그에_감정_축이_있으면_경고_줄을_안_찍는다()
        {
            var log = CaptureLog(Bus(), Interpreted(StoryTag.Emotion("후회")));

            StringAssert.DoesNotContain("안정 축 이동 없음", log);
        }
    }
}
