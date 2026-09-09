using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.Hiromi;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using GameName.Core.Trust;
using GameName.UI.MemoryRoom;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // 상단 바 컨트롤러는 값을 하나도 계산하지 않는다 — 리더(ITrustReader,
    // IHiromiReader, IChanceReader, IExtractedMemoryStore)를 조회해 문구·막대로
    // 넘기고, 다시 그릴 계기만 이벤트로 안다. 여기서는 각 계기마다 표시가
    // 실제로 갱신되는지 본다. "다음 기억으로" 조작 자체는 가방 화면의
    // 레버(MemoryMoveLeverController) 몫이라 여기서는 다루지 않는다.
    //
    // 다른 스크린 컨트롤러 테스트(ClueZoomScreenTests 등)와 같은 방식이다 —
    // 가짜 View 없이 손으로 만든 VisualElement 트리에 진짜 View를 세운다.
    public class MemoryRoomHudControllerTests
    {
        private static readonly MemoryRoomId TheRoom = new MemoryRoomId("room-1");
        private const int MoveThreshold = 15;

        private static VisualElement MakeHudRoot()
        {
            var root = new VisualElement();
            foreach (var name in new[] { "hud-key-hints", "hud-message", "hud-trust", "hud-chance" })
                root.Add(new Label { name = name });

            root.Add(new Label { name = "hud-hiromi-value" });
            root.Add(new VisualElement { name = "hud-hiromi-track" });
            root.Add(new VisualElement { name = "hud-hiromi-fill" });
            root.Add(new VisualElement { name = "hud-hiromi-marker" });

            root.Add(new VisualElement { name = "hud-held-r" });
            root.Add(new VisualElement { name = "hud-held-g" });
            root.Add(new VisualElement { name = "hud-held-b" });

            return root;
        }

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly TrustGauge Trust;
            public readonly HiromiWallet Hiromi;
            public readonly ChanceTracker Chance;
            public readonly ExtractedMemoryStore Memories = new ExtractedMemoryStore();
            public readonly VisualElement Root;

            // ReSharper disable once NotAccessedField.Local — 구독을 살려 두기 위한 보관.
            private readonly MemoryRoomHudController _controller;

            public Fixture(int startingTrust = 3, int startingHiromi = 15, int startingChance = 2)
            {
                Trust = new TrustGauge(startingTrust, Bus);
                Hiromi = new HiromiWallet(startingHiromi, Bus);
                Chance = new ChanceTracker(startingChance, Bus);

                Root = MakeHudRoot();
                var view = new MemoryRoomHudView(Root);
                _controller = new MemoryRoomHudController(
                    view, Trust, Hiromi, MoveThreshold, Chance, Memories, Bus);
            }

            public string Text(string name) => Root.Q<Label>(name).text;
            public VisualElement Element(string name) => Root.Q<VisualElement>(name);
            public bool Held(string color) =>
                Root.Q<VisualElement>($"hud-held-{color}").ClassListContains("hud-held-dot--held");
        }

        [Test]
        public void 생성_직후_현재값을_한_번_그린다()
        {
            var fx = new Fixture(startingTrust: 3, startingHiromi: 15, startingChance: 2);

            StringAssert.Contains("3", fx.Text("hud-trust"));
            StringAssert.Contains("15", fx.Text("hud-hiromi-value"));
            StringAssert.Contains("2", fx.Text("hud-chance"));
            Assert.IsFalse(fx.Held("b"), "손에 든 기억이 없으면 색 점이 켜지지 않는다.");
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
        public void 히로민이_바뀌면_수치와_게이지_문턱_아래_클래스가_갱신된다()
        {
            var fx = new Fixture(startingHiromi: 15);

            fx.Hiromi.Spend(9); // 15 → 6, 문턱(15) 아래로 내려간다.

            StringAssert.Contains("6", fx.Text("hud-hiromi-value"));
            Assert.IsTrue(
                fx.Element("hud-hiromi-fill").ClassListContains("hud-hiromi-fill--below-threshold"),
                "문턱 아래로 내려가면 채움 색이 바뀌는 클래스가 붙어야 한다.");
        }

        [Test]
        public void 기회가_바뀌면_표시값이_갱신된다()
        {
            var fx = new Fixture(startingChance: 2);

            fx.Chance.Decrease();

            StringAssert.Contains("1", fx.Text("hud-chance"));
        }

        [Test]
        public void 그_색_기억을_얻으면_해당_색_점이_켜진다()
        {
            var fx = new Fixture();

            fx.Memories.Add(new ExtractedMemory(new ClueId("clue-1"), MemoryColor.Blue, System.Array.Empty<ClueTag>()));
            fx.Memories.Add(new ExtractedMemory(new ClueId("clue-2"), MemoryColor.Blue, System.Array.Empty<ClueTag>()));
            fx.Bus.Publish(new MemoryColorRevealedEvent(MemoryColor.Blue, new ClueId("clue-2")));

            Assert.IsTrue(fx.Held("b"), "파랑 기억을 들었으면 파랑 점이 켜진다.");
            Assert.IsFalse(fx.Held("r"), "빨강 기억은 없으므로 빨강 점은 꺼져 있다.");
        }

        [Test]
        public void 그_색_기억을_대화에_써_전부_소모하면_해당_색_점이_꺼진다()
        {
            var fx = new Fixture();
            fx.Memories.Add(new ExtractedMemory(new ClueId("c"), MemoryColor.Green, System.Array.Empty<ClueTag>()));
            fx.Bus.Publish(new MemoryColorRevealedEvent(MemoryColor.Green, new ClueId("c")));
            Assert.IsTrue(fx.Held("g"));

            fx.Memories.Remove(new ClueId("c"));
            fx.Bus.Publish(new ExtractedMemoryConsumedEvent(new ClueId("c"), MemoryColor.Green));

            Assert.IsFalse(fx.Held("g"), "그 색 기억이 다 나가면 점이 꺼진다.");
        }

        [Test]
        public void 방이_바뀌어도_신뢰_표시는_유지된다()
        {
            var fx = new Fixture(startingTrust: 3);
            fx.Trust.Decrease(2);
            StringAssert.Contains("1", fx.Text("hud-trust"));

            fx.Bus.Publish(new RoomStartedEvent(TheRoom, 1));

            // 인내심은 런 전체에 걸쳐 이어진다 — 방이 바뀌어도 그대로다.
            Assert.AreEqual(1, fx.Trust.Current);
            StringAssert.Contains("1", fx.Text("hud-trust"));
        }
    }
}
