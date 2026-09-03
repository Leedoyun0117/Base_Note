using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 기억색 1을 대가로 검열 키 하나를 푼다 — 멱등이고, 한 번에 같은 키의 모든
    // 구간이 함께 열린다.
    public class CensorUnlockProcessorTests
    {
        private sealed class FakeMaskFormatter : ICensorMaskFormatter
        {
            public string FormatMask(MemoryColor color) => $"<{color}>";
        }

        private static DialogueLineDefinition Line(string id, string text) =>
            new DialogueLineDefinition(new DialogueLineId(id), "화자", text, Array.Empty<ChoiceDefinition>());

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly MemoryColorWallet Wallet = new MemoryColorWallet();
            public readonly CensorUnlockLog Log = new CensorUnlockLog();
            public readonly CensorUnlockProcessor Processor;
            public readonly List<CensorKeyUnlockedEvent> Unlocked = new List<CensorKeyUnlockedEvent>();

            public Fixture(params DialogueLineDefinition[] lines)
            {
                var room = new RoomDefinition(
                    new MemoryRoomId("room-1"), Array.Empty<ClueDefinition>(),
                    new DialogueLineId(lines[0].Id.Value), lines);
                var run = new RunDefinition(new[] { room }, 3, 5);
                var map = new CensorTokenIndexColorMap(
                    new CensorTokenIndexSource(new CensoredTextParser()), run);
                Processor = new CensorUnlockProcessor(Wallet, Log, map, Bus);
                Bus.Subscribe<CensorKeyUnlockedEvent>(Unlocked.Add);
            }
        }

        [Test]
        public void 지갑에_필요한_색이_없으면_실패하고_렌더링은_계속_마스크된다()
        {
            var fx = new Fixture(Line("line-1", "그 시절 [[B:beach-house:해변의 작은 집]]이 그립다."));

            var result = fx.Processor.Unlock(new CensorKey("beach-house"));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(CensorUnlockFailureReason.InsufficientMemory, result.FailureReason);

            var renderer = new CensorRenderer(fx.Log, new FakeMaskFormatter());
            var parsed = new CensoredTextParser().Parse("그 시절 [[B:beach-house:해변의 작은 집]]이 그립다.");
            Assert.AreEqual("그 시절 <Blue>이 그립다.", renderer.Render(parsed));
        }

        [Test]
        public void 색을_들고_있으면_1_소모하고_키를_기록한다()
        {
            var fx = new Fixture(Line("line-1", "[[B:beach-house:해변의 작은 집]]"));
            fx.Wallet.Add(MemoryColor.Blue, 2);

            var result = fx.Processor.Unlock(new CensorKey("beach-house"));

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(MemoryColor.Blue, result.SpentColor);
            Assert.AreEqual(1, fx.Wallet.GetCount(MemoryColor.Blue));
            Assert.IsTrue(fx.Log.IsRevealed(new CensorKey("beach-house")));
            Assert.AreEqual(1, fx.Unlocked.Count);
        }

        [Test]
        public void 이미_풀린_키를_다시_풀려_하면_자원_소모_없이_성공_취급이다()
        {
            var fx = new Fixture(Line("line-1", "[[B:beach-house:해변의 작은 집]]"));
            fx.Wallet.Add(MemoryColor.Blue, 1);
            fx.Processor.Unlock(new CensorKey("beach-house"));

            var again = fx.Processor.Unlock(new CensorKey("beach-house"));

            Assert.IsTrue(again.Succeeded);
            Assert.IsNull(again.SpentColor);
            Assert.AreEqual(0, fx.Wallet.GetCount(MemoryColor.Blue));
            Assert.AreEqual(1, fx.Unlocked.Count); // 두 번째는 사건 없음
        }

        [Test]
        public void 판에_없는_키는_어느_색으로_풀리는지_알_수_없어_실패다()
        {
            var fx = new Fixture(Line("line-1", "[[B:beach-house:해변의 작은 집]]"));
            fx.Wallet.Add(MemoryColor.Blue, 1);

            Assert.AreEqual(
                CensorUnlockFailureReason.UnknownKey,
                fx.Processor.Unlock(new CensorKey("the-ring")).FailureReason);
        }

        [Test]
        public void 한_번의_해금으로_같은_키의_서로_다른_줄이_함께_원문으로_돌아온다()
        {
            var fx = new Fixture(
                Line("line-1", "그 집이라 부르던 [[B:beach-house:해변의 작은 집]]"),
                Line("line-2", "[[B:beach-house:거기]]는 이제 없다."));
            fx.Wallet.Add(MemoryColor.Blue, 1);

            fx.Processor.Unlock(new CensorKey("beach-house"));

            var renderer = new CensorRenderer(fx.Log, new FakeMaskFormatter());
            var parser = new CensoredTextParser();
            Assert.AreEqual(
                "그 집이라 부르던 해변의 작은 집",
                renderer.Render(parser.Parse("그 집이라 부르던 [[B:beach-house:해변의 작은 집]]")));
            Assert.AreEqual(
                "거기는 이제 없다.",
                renderer.Render(parser.Parse("[[B:beach-house:거기]]는 이제 없다.")));
        }
    }
}
