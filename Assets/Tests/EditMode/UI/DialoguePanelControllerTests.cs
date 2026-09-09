using System;
using System.Collections.Generic;
using System.Linq;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using GameName.Core.Trust;
using GameName.UI.MemoryRoom.Dialogue;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // 대화 패널 컨트롤러의 규칙:
    //   · 라인이 바뀌면 그 라인의 화자·원문을 View에 넘긴다.
    //   · 마스크 구간 클릭 → 제시할 수 있는 추출된 기억이 있으면 목록 팝업을 띄우고,
    //     하나를 고르면 CensorUnlockProcessor를 태운다(태그가 안 맞으면 실패 안내만).
    //     기억이 하나도 없으면 안내만 하고 대화는 막지 않는다.
    //   · 그려진 선택지 목록은 DialogueProgressor가 필터링한 목록과 일치한다.
    //   · 다음 대사가 없는 선택 후에는 종료 상태(빈 라인·빈 선택지)를 반영한다.
    //
    // View는 인터페이스(IDialoguePanelView)로 대체한다 — 클릭·이벤트가 많아
    // UIDocument를 세우는 것보다 페이크가 규칙을 더 또렷하게 드러낸다.
    public class DialoguePanelControllerTests
    {
        private static readonly MemoryRoomId TheRoom = new MemoryRoomId("room-1");

        private sealed class FakeView : IDialoguePanelView
        {
            public event Action<CensorKey> MaskClicked;
            public event Action<ChoiceId> ChoiceClicked;
            public event Action<ClueId> ClueAnswerClicked;
            public event Action SkipClueAnswerClicked;
            public event Action<ClueId> MemoryPresented;
            public event Action UnlockCancelled;

            public string LastSpeaker;
            public string LastAuthoredText;
            public IReadOnlyList<KeyValuePair<ChoiceId, string>> LastChoices =
                Array.Empty<KeyValuePair<ChoiceId, string>>();
            public IReadOnlyList<KeyValuePair<ClueId, string>> LastClueSelection;
            public IReadOnlyList<KeyValuePair<ClueId, string>> LastMemoryOptions;
            public string LastNotice;
            public bool PromptVisible;
            public int PromptShownCount;

            public void SetLine(string speaker, string authoredText)
            {
                LastSpeaker = speaker;
                LastAuthoredText = authoredText;
            }

            public void SetChoices(IReadOnlyList<KeyValuePair<ChoiceId, string>> choices)
            {
                LastChoices = choices;
                LastClueSelection = null;
            }

            public void SetClueSelection(IReadOnlyList<KeyValuePair<ClueId, string>> clues)
            {
                LastClueSelection = clues;
            }

            public void SetNotice(string message) => LastNotice = message;

            public void ShowUnlockPrompt(string message, IReadOnlyList<KeyValuePair<ClueId, string>> memoryOptions)
            {
                LastMemoryOptions = memoryOptions;
                PromptVisible = true;
                PromptShownCount++;
            }

            public void HideUnlockPrompt() => PromptVisible = false;

            public void RaiseMaskClicked(CensorKey key) => MaskClicked?.Invoke(key);
            public void RaiseChoiceClicked(ChoiceId id) => ChoiceClicked?.Invoke(id);
            public void RaiseClueAnswerClicked(ClueId id) => ClueAnswerClicked?.Invoke(id);
            public void RaiseSkipClueAnswer() => SkipClueAnswerClicked?.Invoke();
            public void RaiseMemoryPresented(ClueId id) => MemoryPresented?.Invoke(id);
            public void RaiseUnlockCancelled() => UnlockCancelled?.Invoke();
        }

        private static ChoiceDefinition Choice(string id, bool correct, string next = null) =>
            new ChoiceDefinition(
                new ChoiceId(id), "", correct,
                next == null ? (DialogueLineId?)null : new DialogueLineId(next), ChoiceCondition.None);

        private static DialogueLineDefinition Line(string id, string text, params ChoiceDefinition[] choices) =>
            new DialogueLineDefinition(new DialogueLineId(id), "화자", text, choices);

        private static ClueDefinition ClueDef(string id, params string[] tags) =>
            new ClueDefinition(new ClueId(id), ClueKind.FloorObject, id + " 이름",
                new CluePositionRatio(0.5f), MemoryColor.Red,
                Array.ConvertAll(tags, t => new ClueTag(t)));

        private static string ColorName(MemoryColor color) => color.ToString();

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly ExtractedMemoryStore Memories = new ExtractedMemoryStore();
            public readonly CensorUnlockLog CensorLog = new CensorUnlockLog();
            public readonly ClueStateStore ClueState;
            public readonly DialogueProgressor Progressor;
            public readonly DialoguePanelController Controller;
            public readonly FakeView View = new FakeView();

            public Fixture() : this(DefaultRoom())
            {
            }

            public Fixture(RoomDefinition room)
            {
                var run = new RunDefinition(
                    new[] { room }, startingTrust: 3, startingHiromi: 3,
                    censorKeyTagRequirements: new[]
                    {
                        new CensorKeyTagRequirement(new CensorKey("k1"), new[] { new ClueTag("beachHouse") }),
                    });

                var trust = new TrustGauge(3, Bus);
                ClueState = new ClueStateStore(run.Rooms, Bus);
                var colorMap = new CensorTokenIndexColorMap(
                    new CensorTokenIndexSource(new CensoredTextParser()), run);
                var requiredTags = new CensorKeyRequiredTagMap(run);
                var censorUnlock = new CensorUnlockProcessor(Memories, CensorLog, requiredTags, Bus);
                var tracker = new MemoryRoomClueTracker(BuildPlacements(room));

                Progressor = new DialogueProgressor(run.Rooms, CensorLog, ClueState, trust, Bus);

                // 방이 시작되어 시작 라인으로 들어간 뒤에 패널을 만든다 — 생산
                // 코드와 같은 순서(세션 조립 후 화면 부착)다.
                Bus.Publish(new RoomStartedEvent(room.Id, 0));

                Controller = new DialoguePanelController(
                    View, Progressor, censorUnlock, Memories, tracker, colorMap, ColorName, room.Id, Bus);
            }

            private static IReadOnlyList<CluePlacement> BuildPlacements(RoomDefinition room)
            {
                var list = new List<CluePlacement>();
                foreach (var clue in room.Clues)
                    list.Add(new CluePlacement(room.Id, clue));
                return list;
            }

            private static RoomDefinition DefaultRoom() =>
                new RoomDefinition(TheRoom, Array.Empty<ClueDefinition>(), new DialogueLineId("line-1"),
                    new[]
                    {
                        Line("line-1", "우리가 [[B:k1:해변의 집]]에서 보냈지.", Choice("go", true, "line-2")),
                        Line("line-2", "", Choice("end", true, null)),
                    });
        }

        private static RoomDefinition ClueSelectionRoom() =>
            new RoomDefinition(TheRoom,
                new[] { ClueDef("clue-a", "clue-a"), ClueDef("clue-b") },
                new DialogueLineId("q"),
                new[]
                {
                    DialogueLineDefinition.ClueSelection(
                        new DialogueLineId("q"), "화자", "무엇을 들고 있었어?",
                        new[] { new ClueTag("clue-a") },
                        new DialogueLineId("right"), new DialogueLineId("wrong")),
                    Line("right", "", Choice("r", true)),
                    Line("wrong", "", Choice("w", true)),
                });

        [Test]
        public void 생성_직후_현재_라인의_화자와_원문을_View에_넘긴다()
        {
            var fx = new Fixture();

            Assert.AreEqual("화자", fx.View.LastSpeaker);
            Assert.AreEqual("우리가 [[B:k1:해변의 집]]에서 보냈지.", fx.View.LastAuthoredText);
        }

        [Test]
        public void 그려진_선택지_목록은_DialogueProgressor_필터_결과와_일치한다()
        {
            var fx = new Fixture();

            var expected = new List<string>();
            foreach (var choice in fx.Progressor.VisibleChoices())
                expected.Add(choice.Id.Value);

            var actual = new List<string>();
            foreach (var pair in fx.View.LastChoices)
                actual.Add(pair.Key.Value);

            CollectionAssert.AreEqual(expected, actual);
            CollectionAssert.AreEqual(new[] { "go" }, actual);
        }

        [Test]
        public void 마스크_클릭_제시할_기억이_있으면_팝업이_뜨고_알맞은_기억_제시로_해금된다()
        {
            var fx = new Fixture();
            var memoryClue = new ClueId("mem-1");
            fx.Memories.Add(new ExtractedMemory(memoryClue, MemoryColor.Blue, new[] { new ClueTag("beachHouse") }));

            fx.View.RaiseMaskClicked(new CensorKey("k1"));
            Assert.IsTrue(fx.View.PromptVisible, "제시할 기억이 있으면 팝업이 떠야 한다.");
            Assert.IsNotNull(fx.View.LastMemoryOptions);
            CollectionAssert.Contains(fx.View.LastMemoryOptions.Select(o => o.Key), memoryClue);
            Assert.IsFalse(fx.CensorLog.IsRevealed(new CensorKey("k1")), "고르기 전에는 풀리면 안 된다.");

            fx.View.RaiseMemoryPresented(memoryClue);

            Assert.IsTrue(fx.CensorLog.IsRevealed(new CensorKey("k1")));
            Assert.IsFalse(fx.Memories.TryGet(memoryClue, out _), "제시한 기억은 소모되어야 한다.");
            Assert.IsFalse(fx.View.PromptVisible, "해금 후 팝업은 닫혀야 한다.");
            // 렌더는 여전히 원문 토큰을 넘긴다 — 실제 원문화는 View의 몫이다.
            Assert.AreEqual("우리가 [[B:k1:해변의 집]]에서 보냈지.", fx.View.LastAuthoredText);
        }

        [Test]
        public void 마스크_클릭_태그가_다른_기억을_제시하면_풀리지_않고_실패_안내만_한다()
        {
            var fx = new Fixture();
            var memoryClue = new ClueId("mem-1");
            // 색은 힌트와 같은 Blue지만 태그가 k1의 요구 태그와 다르다.
            fx.Memories.Add(new ExtractedMemory(memoryClue, MemoryColor.Blue, new[] { new ClueTag("somethingElse") }));

            fx.View.RaiseMaskClicked(new CensorKey("k1"));
            fx.View.RaiseMemoryPresented(memoryClue);

            Assert.IsFalse(fx.CensorLog.IsRevealed(new CensorKey("k1")));
            Assert.IsTrue(fx.Memories.TryGet(memoryClue, out _), "실패한 제시는 기억을 소모하지 않는다.");
            Assert.IsFalse(string.IsNullOrEmpty(fx.View.LastNotice), "실패 안내 문구가 있어야 한다.");
        }

        [Test]
        public void 마스크_클릭_제시할_기억이_없으면_해금하지_않고_안내만_하며_대화는_막히지_않는다()
        {
            var fx = new Fixture(); // 손에 든 기억 없음

            fx.View.RaiseMaskClicked(new CensorKey("k1"));

            Assert.AreEqual(0, fx.View.PromptShownCount, "제시할 기억이 없으면 팝업이 뜨면 안 된다.");
            Assert.IsFalse(fx.CensorLog.IsRevealed(new CensorKey("k1")));
            Assert.IsFalse(string.IsNullOrEmpty(fx.View.LastNotice), "안내 문구가 있어야 한다.");

            // 대화는 그대로 진행 가능해야 한다.
            fx.View.RaiseChoiceClicked(new ChoiceId("go"));
            Assert.AreEqual(new DialogueLineId("line-2"), fx.Progressor.CurrentLineId);
        }

        [Test]
        public void 다음_대사가_없는_선택_후_패널에_다음으로_버튼만_남는다()
        {
            var fx = new Fixture();

            fx.View.RaiseChoiceClicked(new ChoiceId("go"));   // line-1 → line-2
            fx.View.RaiseChoiceClicked(new ChoiceId("end"));  // line-2 → 대화 종료

            Assert.IsNull(fx.Progressor.CurrentLine);
            Assert.AreEqual(string.Empty, fx.View.LastSpeaker);
            Assert.AreEqual(string.Empty, fx.View.LastAuthoredText);
            Assert.AreEqual(1, fx.View.LastChoices.Count, "방을 떠나는 버튼만 남아야 한다.");
            Assert.AreEqual("다음으로", fx.View.LastChoices[0].Value);
        }

        [Test]
        public void 다음으로_버튼을_누르면_지금_방의_RoomClearedEvent가_나간다()
        {
            var fx = new Fixture();
            var cleared = new List<RoomClearedEvent>();
            fx.Bus.Subscribe<RoomClearedEvent>(cleared.Add);

            fx.View.RaiseChoiceClicked(new ChoiceId("go"));
            fx.View.RaiseChoiceClicked(new ChoiceId("end"));      // 대화 종료 → "다음으로"
            fx.View.RaiseChoiceClicked(fx.View.LastChoices[0].Key); // "다음으로" 클릭

            Assert.AreEqual(1, cleared.Count);
            Assert.AreEqual(TheRoom, cleared[0].RoomId);
        }

        [Test]
        public void ClueSelection_줄에서는_텍스트_선택지_대신_들고_있는_단서_목록을_그린다()
        {
            var fx = new Fixture(ClueSelectionRoom());
            fx.ClueState.SetState(new ClueId("clue-a"), ClueState.Collected);
            // 컨트롤러가 다시 그리도록 라인 진입 이벤트를 흉내 낸다.
            fx.Bus.Publish(new DialogueLineEnteredEvent(new DialogueLineId("q")));

            Assert.IsNotNull(fx.View.LastClueSelection);
            var ids = fx.View.LastClueSelection.Select(p => p.Key.Value).ToList();
            CollectionAssert.AreEqual(new[] { "clue-a" }, ids);
        }

        [Test]
        public void ClueSelection_줄에_머무는_동안_단서를_집으면_답_목록이_갱신된다()
        {
            var fx = new Fixture(ClueSelectionRoom());
            CollectionAssert.IsEmpty(fx.View.LastClueSelection, "처음엔 손에 든 단서가 없다.");

            fx.ClueState.SetState(new ClueId("clue-a"), ClueState.Collected);
            fx.Bus.Publish(new ClueCollectedEvent(new ClueId("clue-a"), TheRoom));

            Assert.AreEqual(1, fx.View.LastClueSelection.Count, "집은 단서가 답 목록에 나타나야 한다.");
        }

        [Test]
        public void ClueSelection_단서_답_클릭은_SelectClue를_태운다()
        {
            var fx = new Fixture(ClueSelectionRoom());
            fx.ClueState.SetState(new ClueId("clue-a"), ClueState.Collected);

            fx.View.RaiseClueAnswerClicked(new ClueId("clue-a"));

            Assert.AreEqual(new DialogueLineId("right"), fx.Progressor.CurrentLineId);
            Assert.AreEqual(ClueState.UsedInDialogue, fx.ClueState.GetState(new ClueId("clue-a")));
        }

        [Test]
        public void 런이_끝나면_종료_문구와_모은_색_요약을_패널에_띄운다()
        {
            var fx = new Fixture();
            fx.Memories.Add(new ExtractedMemory(new ClueId("mem-1"), MemoryColor.Blue, Array.Empty<ClueTag>()));
            fx.Memories.Add(new ExtractedMemory(new ClueId("mem-2"), MemoryColor.Blue, Array.Empty<ClueTag>()));

            fx.Bus.Publish(new RunCompletedEvent());

            StringAssert.Contains("끝난다", fx.View.LastAuthoredText);
            StringAssert.Contains("B 2", fx.View.LastAuthoredText);
            CollectionAssert.IsEmpty(fx.View.LastChoices);

            // 그 뒤 어떤 이벤트가 와도 종료 표시를 덮지 않는다.
            fx.Bus.Publish(new RoomStartedEvent(TheRoom, 0));
            StringAssert.Contains("끝난다", fx.View.LastAuthoredText);
        }
    }
}
