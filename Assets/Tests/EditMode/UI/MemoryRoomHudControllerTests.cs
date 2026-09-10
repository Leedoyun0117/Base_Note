using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Complexes;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.Core.Mind;
using GameName.UI.MemoryRoom;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // 상단 바 컨트롤러는 값을 하나도 계산하지 않는다 — 리더(ITurnReader,
    // IStabilityReader, IActiveComplexListReader)를 조회해 문구로 넘기고, 다시
    // 그릴 계기만 이벤트로 안다. 여기서는 각 계기마다 표시가 실제로 갱신되는지 본다.
    //
    // 가짜 View 없이 손으로 만든 VisualElement 트리에 진짜 View를 세운다.
    public class MemoryRoomHudControllerTests
    {
        private static readonly MemoryRoomId TheRound = new MemoryRoomId("round-1");

        private static VisualElement MakeHudRoot()
        {
            var root = new VisualElement();
            foreach (var name in new[] { "hud-key-hints", "hud-message", "hud-trust", "hud-hiromi-value", "hud-chance" })
                root.Add(new Label { name = name });

            return root;
        }

        private static RoomDefinition Round(string id, int turnsToSurvive) =>
            new RoomDefinition(new MemoryRoomId(id), Array.Empty<ClueDefinition>(), turnsToSurvive);

        private static ComplexDefinition Complex(
            string id, int priority = 0, int durationTurns = 3,
            string displayName = null, string description = null) =>
            new ComplexDefinition(
                new ComplexId(id), priority, durationTurns, ComplexKind.Transform,
                new List<TagTransformRule>(), displayName, description);

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly TurnCoordinator Turns;
            public readonly StabilityAxis Stability;
            public readonly ActiveComplexList ActiveComplexes;
            public readonly VisualElement Root;

            // ReSharper disable once NotAccessedField.Local — 구독을 살려 두기 위한 보관.
            private readonly MemoryRoomHudController _controller;

            public Fixture(int turnsToSurvive = 4, int startingStability = 0)
            {
                Turns = new TurnCoordinator(new[] { Round("round-1", turnsToSurvive) }, Bus);
                Stability = new StabilityAxis(startingStability, -100, 100, Bus);
                ActiveComplexes = new ActiveComplexList(4, Bus);

                Bus.Publish(new RoomStartedEvent(TheRound, 0));

                Root = MakeHudRoot();
                var view = new MemoryRoomHudView(Root);
                _controller = new MemoryRoomHudController(view, Turns, Stability, ActiveComplexes, Bus);
            }

            public string Text(string name) => Root.Q<Label>(name).text;
        }

        [Test]
        public void 생성_직후_현재값을_한_번_그린다()
        {
            var fx = new Fixture(turnsToSurvive: 4, startingStability: 0);

            StringAssert.Contains("0", fx.Text("hud-trust"));
            StringAssert.Contains("4", fx.Text("hud-trust"));
            StringAssert.Contains("0", fx.Text("hud-hiromi-value"));
            StringAssert.Contains("없음", fx.Text("hud-chance"));
        }

        [Test]
        public void 턴이_넘어가면_표시값이_ITurnReader_값과_일치한다()
        {
            var fx = new Fixture(turnsToSurvive: 4);

            fx.Turns.AdvanceTurn();

            Assert.AreEqual(1, fx.Turns.CurrentTurn);
            StringAssert.Contains("1", fx.Text("hud-trust"));
        }

        [Test]
        public void 안정_축이_바뀌면_수치가_갱신된다()
        {
            var fx = new Fixture();

            fx.Stability.Shift(15);

            StringAssert.Contains("15", fx.Text("hud-hiromi-value"));
        }

        [Test]
        public void 컴플렉스가_활성화되면_이름_남은_턴_설명이_다_뜬다()
        {
            var fx = new Fixture();

            fx.ActiveComplexes.TryActivate(Complex(
                "complex-sink", durationTurns: 3,
                displayName: "가라앉다", description: "결국 후회로 가라앉는 경향이 있습니다."));

            var text = fx.Text("hud-chance");
            StringAssert.Contains("가라앉다", text);
            StringAssert.Contains("3", text);
            StringAssert.Contains("후회로 가라앉는", text);
        }

        [Test]
        public void 컴플렉스가_소멸하면_목록에서_빠진다()
        {
            var fx = new Fixture();
            fx.ActiveComplexes.TryActivate(Complex("complex-a", durationTurns: 1));

            fx.Turns.AdvanceTurn(); // 지속 턴 1 → 0, 소멸.

            StringAssert.Contains("없음", fx.Text("hud-chance"));
        }

        [Test]
        public void 라운드가_바뀌어도_안정_표시는_유지된다()
        {
            var fx = new Fixture(startingStability: 0);
            fx.Stability.Shift(20);
            StringAssert.Contains("20", fx.Text("hud-hiromi-value"));

            fx.Bus.Publish(new RoomStartedEvent(new MemoryRoomId("round-2"), 1));

            // 안정 축은 런 전체에 걸쳐 이어진다 — 라운드가 바뀌어도 그대로다.
            Assert.AreEqual(20, fx.Stability.Position);
            StringAssert.Contains("20", fx.Text("hud-hiromi-value"));
        }
    }
}
