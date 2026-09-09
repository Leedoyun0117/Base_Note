using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Hiromi;
using GameName.Core.Mind;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 답변 한 번마다 회복하는 히로민 = Base + round(max(0, BonusBand - |안정 위치|) / BonusDivisor).
    // 여기 수치는 RunDefinition 기본값(3 / 20 / 4)을 그대로 쓴다.
    public class HiromiDialogueEarningListenerTests
    {
        private const int Base = 3;
        private const int Band = 20;
        private const int Divisor = 4;

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly HiromiWallet Hiromi;
            public readonly StabilityAxis Stability;

            // ReSharper disable once NotAccessedField.Local — 구독을 살려 두기 위한 보관.
            private readonly HiromiDialogueEarningListener _listener;

            public Fixture(int stabilityPosition)
            {
                Hiromi = new HiromiWallet(0, Bus);
                Stability = new StabilityAxis(stabilityPosition, -100, 100, Bus);
                _listener = new HiromiDialogueEarningListener(Hiromi, Stability, Base, Band, Divisor, Bus);
            }

            public void Answer() => Bus.Publish(new ClueAnsweredEvent(MatchGrade.None));
            public void PickTextChoice() =>
                Bus.Publish(new ChoiceSelectedEvent(new DialogueLineId("l"), new ChoiceId("c")));
        }

        [Test]
        public void 안정_한복판이면_기본_더하기_최대_보너스를_번다()
        {
            var fx = new Fixture(stabilityPosition: 0);

            fx.Answer();

            // within = 20, bonus = round(20 / 4) = 5 → 3 + 5 = 8
            Assert.AreEqual(8, fx.Hiromi.Remaining);
        }

        [Test]
        public void 안정에서_멀어질수록_보너스가_반올림되며_줄어든다()
        {
            Assert.AreEqual(6, Earned(10), "within 10 → round(10/4)=3 → 6");
            Assert.AreEqual(4, Earned(-18), "within 2 → round(2/4)=1 → 4");
        }

        [Test]
        public void 보너스_폭을_벗어나면_기본만_번다()
        {
            Assert.AreEqual(Base, Earned(20));
            Assert.AreEqual(Base, Earned(-55));
        }

        [Test]
        public void 텍스트_선택지_하나_고르는_것도_같은_회복을_준다()
        {
            var fx = new Fixture(stabilityPosition: 0);

            fx.PickTextChoice();

            Assert.AreEqual(8, fx.Hiromi.Remaining);
        }

        private static int Earned(int stabilityPosition)
        {
            var fx = new Fixture(stabilityPosition);
            fx.Answer();
            return fx.Hiromi.Remaining;
        }
    }
}
