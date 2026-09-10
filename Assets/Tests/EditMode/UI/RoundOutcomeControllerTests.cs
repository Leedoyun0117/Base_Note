using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.UI.MemoryRoom;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // 라운드 클리어 / 판 종료 문구 컨트롤러. 규칙은 판단하지 않고 사건을 문구로만
    // 옮긴다 — 여기서는 각 사건에 상단 안내 줄이 실제로 바뀌는지, 그리고 마지막
    // 라운드에서 "클리어" 문구가 "모든 라운드" 문구를 덮지 않는지 본다.
    public class RoundOutcomeControllerTests
    {
        private static VisualElement MakeRoot()
        {
            var root = new VisualElement();
            root.Add(new Label { name = "hud-message" });
            return root;
        }

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly VisualElement Root;

            // ReSharper disable once NotAccessedField.Local — 구독 유지용.
            private readonly RoundOutcomeController _controller;

            public Fixture()
            {
                Root = MakeRoot();
                _controller = new RoundOutcomeController(new MemoryRoomHudView(Root), Bus);
            }

            public string Message => Root.Q<Label>("hud-message").text;
        }

        [Test]
        public void 라운드를_버티면_클리어_문구가_뜬다()
        {
            var fx = new Fixture();

            fx.Bus.Publish(new RoundSurvivedEvent());

            StringAssert.Contains("클리어", fx.Message);
        }

        [Test]
        public void 다음_라운드가_시작되면_클리어_문구가_지워진다()
        {
            var fx = new Fixture();
            fx.Bus.Publish(new RoundSurvivedEvent());

            fx.Bus.Publish(new RoomStartedEvent(new MemoryRoomId("round-2"), 1));

            Assert.IsEmpty(fx.Message);
        }

        [Test]
        public void 판이_끝나면_종료_문구가_뜨고_그_뒤_클리어_문구로_덮이지_않는다()
        {
            var fx = new Fixture();

            fx.Bus.Publish(new RunCompletedEvent());
            fx.Bus.Publish(new RoundSurvivedEvent());
            fx.Bus.Publish(new RoomStartedEvent(new MemoryRoomId("round-2"), 1));

            StringAssert.Contains("모든 라운드", fx.Message);
        }
    }
}
