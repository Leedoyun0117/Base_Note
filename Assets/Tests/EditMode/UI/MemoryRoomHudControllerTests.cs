using GameName.Core.Events;
using GameName.Core.Extraction;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using GameName.Core.Trust;
using GameName.UI.MemoryRoom;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // 상단 바 컨트롤러는 값을 하나도 계산하지 않는다 — 리더(ITrustReader,
    // IExtractionBudget, IMemoryColorWallet)를 조회해 문구로 넘기고, 다시 그릴
    // 계기만 이벤트로 안다. 여기서는 각 계기마다 표시가 실제로 갱신되는지 본다.
    //
    // 다른 스크린 컨트롤러 테스트(ClueZoomScreenTests 등)와 같은 방식이다 —
    // 가짜 View 없이 손으로 만든 VisualElement 트리에 진짜 View를 세운다.
    public class MemoryRoomHudControllerTests
    {
        private static readonly MemoryRoomId TheRoom = new MemoryRoomId("room-1");

        private static VisualElement MakeHudRoot()
        {
            var root = new VisualElement();
            foreach (var name in new[]
                     {
                         "hud-key-hints", "hud-message", "hud-trust", "hud-extraction",
                         "hud-wallet-r", "hud-wallet-g", "hud-wallet-b",
                     })
            {
                root.Add(new Label { name = name });
            }

            return root;
        }

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly TrustGauge Trust;
            public readonly ExtractionBudget Budget;
            public readonly MemoryColorWallet Wallet = new MemoryColorWallet();
            public readonly VisualElement Root;

            // ReSharper disable once NotAccessedField.Local — 구독을 살려 두기 위한 보관.
            private readonly MemoryRoomHudController _controller;

            public Fixture(int startingTrust = 3, int budget = 5)
            {
                Trust = new TrustGauge(startingTrust, Bus);
                Budget = new ExtractionBudget(budget);

                Root = MakeHudRoot();
                var view = new MemoryRoomHudView(Root);
                _controller = new MemoryRoomHudController(view, Trust, Budget, Wallet, Bus);
            }

            public string Text(string name) => Root.Q<Label>(name).text;
        }

        [Test]
        public void 생성_직후_현재값을_한_번_그린다()
        {
            var fx = new Fixture(startingTrust: 3, budget: 5);

            StringAssert.Contains("3", fx.Text("hud-trust"));
            StringAssert.Contains("5", fx.Text("hud-extraction"));
            StringAssert.Contains("0", fx.Text("hud-wallet-b"));
        }

        [Test]
        public void 신뢰가_바뀌면_표시값이_ITrustReader_값과_일치한다()
        {
            var fx = new Fixture(startingTrust: 3);

            fx.Trust.Decrease(1);

            Assert.AreEqual(2, fx.Trust.Current);
            StringAssert.Contains("2", fx.Text("hud-trust"));
        }

        [Test]
        public void 추출_사건이_오면_남은_자원_표시가_사건이_실은_값으로_갱신된다()
        {
            var fx = new Fixture(budget: 5);

            fx.Bus.Publish(new ClueExtractedEvent(new GameName.Core.Clues.ClueId("clue-1"), remainingExtractions: 2));

            StringAssert.Contains("2", fx.Text("hud-extraction"));
        }

        [Test]
        public void 색이_드러나면_색별_기억제_보유_수가_지갑_값으로_갱신된다()
        {
            var fx = new Fixture();

            fx.Wallet.Add(MemoryColor.Blue, 2);
            fx.Bus.Publish(new MemoryColorRevealedEvent(MemoryColor.Blue, new GameName.Core.Clues.ClueId("clue-1")));

            StringAssert.Contains("2", fx.Text("hud-wallet-b"));
            StringAssert.Contains("0", fx.Text("hud-wallet-r"));
        }

        [Test]
        public void 검열_해금으로_지갑이_줄어도_보유_수가_갱신된다()
        {
            var fx = new Fixture();
            fx.Wallet.Add(MemoryColor.Green, 1);
            fx.Bus.Publish(new MemoryColorRevealedEvent(MemoryColor.Green, new GameName.Core.Clues.ClueId("c")));
            StringAssert.Contains("1", fx.Text("hud-wallet-g"));

            fx.Wallet.Remove(MemoryColor.Green, 1);
            fx.Bus.Publish(new CensorKeyUnlockedEvent(new GameName.Core.Dialogue.CensorKey("k"), MemoryColor.Green));

            StringAssert.Contains("0", fx.Text("hud-wallet-g"));
        }

        [Test]
        public void 방이_바뀌면_신뢰_표시가_시작값으로_리셋된다()
        {
            var fx = new Fixture(startingTrust: 3);
            fx.Trust.Decrease(2);
            StringAssert.Contains("1", fx.Text("hud-trust"));

            fx.Bus.Publish(new RoomStartedEvent(TheRoom, 1));

            Assert.AreEqual(3, fx.Trust.Current, "TrustGauge가 방 시작에 리셋되어야 한다.");
            StringAssert.Contains("3", fx.Text("hud-trust"));
        }
    }
}
