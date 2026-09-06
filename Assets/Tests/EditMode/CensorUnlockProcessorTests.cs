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
    // 추출한 기억 하나를 제시해 검열 키 하나를 푼다 — 판정 기준은 색이 아니라
    // 태그이고, 멱등이며, 한 번에 같은 키의 모든 구간이 함께 열린다.
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
            public readonly ExtractedMemoryStore Memories = new ExtractedMemoryStore();
            public readonly CensorUnlockLog Log = new CensorUnlockLog();
            public readonly CensorUnlockProcessor Processor;
            public readonly List<CensorKeyUnlockedEvent> Unlocked = new List<CensorKeyUnlockedEvent>();

            public Fixture(IReadOnlyList<CensorKeyTagRequirement> requirements, params DialogueLineDefinition[] lines)
            {
                var room = new RoomDefinition(
                    new MemoryRoomId("room-1"), Array.Empty<ClueDefinition>(),
                    new DialogueLineId(lines[0].Id.Value), lines);
                var run = new RunDefinition(
                    new[] { room }, 3, 5, censorKeyTagRequirements: requirements);
                var requiredTags = new CensorKeyRequiredTagMap(run);
                Processor = new CensorUnlockProcessor(Memories, Log, requiredTags, Bus);
                Bus.Subscribe<CensorKeyUnlockedEvent>(Unlocked.Add);
            }

            public void AddMemory(string clueId, MemoryColor color, params string[] tags) =>
                Memories.Add(new ExtractedMemory(
                    new ClueId(clueId), color, Array.ConvertAll(tags, t => new ClueTag(t))));
        }

        private static CensorKeyTagRequirement Requirement(string key, params string[] tags) =>
            new CensorKeyTagRequirement(new CensorKey(key), Array.ConvertAll(tags, t => new ClueTag(t)));

        [Test]
        public void 제시할_기억이_없으면_실패하고_렌더링은_계속_마스크된다()
        {
            var fx = new Fixture(
                new[] { Requirement("beach-house", "room1.beachHouse") },
                Line("line-1", "그 시절 [[B:beach-house:해변의 작은 집]]이 그립다."));

            var result = fx.Processor.Unlock(new CensorKey("beach-house"), new ClueId("clue-x"));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(CensorUnlockFailureReason.MemoryNotFound, result.FailureReason);

            var renderer = new CensorRenderer(fx.Log, new FakeMaskFormatter());
            var parsed = new CensoredTextParser().Parse("그 시절 [[B:beach-house:해변의 작은 집]]이 그립다.");
            Assert.AreEqual("그 시절 <Blue>이 그립다.", renderer.Render(parsed));
        }

        [Test]
        public void 태그가_맞는_기억을_제시하면_소모하고_키를_기록한다()
        {
            var fx = new Fixture(
                new[] { Requirement("beach-house", "room1.beachHouse") },
                Line("line-1", "[[B:beach-house:해변의 작은 집]]"));
            fx.AddMemory("clue-1", MemoryColor.Blue, "room1.beachHouse");

            var result = fx.Processor.Unlock(new CensorKey("beach-house"), new ClueId("clue-1"));

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(MemoryColor.Blue, result.SpentColor);
            Assert.IsFalse(fx.Memories.TryGet(new ClueId("clue-1"), out _), "제시한 기억은 소모되어 사라진다.");
            Assert.IsTrue(fx.Log.IsRevealed(new CensorKey("beach-house")));
            Assert.AreEqual(1, fx.Unlocked.Count);
        }

        [Test]
        public void 색이_맞아도_태그가_다르면_해금에_실패한다()
        {
            var fx = new Fixture(
                new[] { Requirement("beach-house", "room1.beachHouse") },
                Line("line-1", "[[B:beach-house:해변의 작은 집]]"));
            // 색은 힌트와 같은 Blue지만 태그가 요구 태그와 다르다.
            fx.AddMemory("clue-1", MemoryColor.Blue, "room1.somethingElse");

            var result = fx.Processor.Unlock(new CensorKey("beach-house"), new ClueId("clue-1"));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(CensorUnlockFailureReason.TagMismatch, result.FailureReason);
            Assert.IsTrue(fx.Memories.TryGet(new ClueId("clue-1"), out _), "실패한 제시는 기억을 소모하지 않는다.");
            Assert.IsFalse(fx.Log.IsRevealed(new CensorKey("beach-house")));
        }

        [Test]
        public void 이미_풀린_키를_다시_풀려_하면_기억_소모_없이_성공_취급이다()
        {
            var fx = new Fixture(
                new[] { Requirement("beach-house", "room1.beachHouse") },
                Line("line-1", "[[B:beach-house:해변의 작은 집]]"));
            fx.AddMemory("clue-1", MemoryColor.Blue, "room1.beachHouse");
            fx.Processor.Unlock(new CensorKey("beach-house"), new ClueId("clue-1"));

            fx.AddMemory("clue-2", MemoryColor.Blue, "room1.beachHouse");
            var again = fx.Processor.Unlock(new CensorKey("beach-house"), new ClueId("clue-2"));

            Assert.IsTrue(again.Succeeded);
            Assert.IsNull(again.SpentColor);
            Assert.IsTrue(fx.Memories.TryGet(new ClueId("clue-2"), out _), "멱등 성공은 기억을 건드리지 않는다.");
            Assert.AreEqual(1, fx.Unlocked.Count); // 두 번째는 사건 없음
        }

        [Test]
        public void 요구_태그가_저작되지_않은_키는_실패다()
        {
            var fx = new Fixture(
                new[] { Requirement("beach-house", "room1.beachHouse") },
                Line("line-1", "[[B:beach-house:해변의 작은 집]]"));
            fx.AddMemory("clue-1", MemoryColor.Blue, "room1.beachHouse");

            Assert.AreEqual(
                CensorUnlockFailureReason.UnknownKey,
                fx.Processor.Unlock(new CensorKey("the-ring"), new ClueId("clue-1")).FailureReason);
        }

        [Test]
        public void 한_번의_해금으로_같은_키의_서로_다른_줄이_함께_원문으로_돌아온다()
        {
            var fx = new Fixture(
                new[] { Requirement("beach-house", "room1.beachHouse") },
                Line("line-1", "그 집이라 부르던 [[B:beach-house:해변의 작은 집]]"),
                Line("line-2", "[[B:beach-house:거기]]는 이제 없다."));
            fx.AddMemory("clue-1", MemoryColor.Blue, "room1.beachHouse");

            fx.Processor.Unlock(new CensorKey("beach-house"), new ClueId("clue-1"));

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
