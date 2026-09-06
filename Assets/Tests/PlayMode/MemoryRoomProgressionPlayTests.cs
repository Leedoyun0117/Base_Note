using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.UI.MemoryRoom;
using GameName.UI.MemoryRoom.Space;
using GameName.UI.Overlays;
using GameName.UI.Session;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GameName.Tests.PlayMode
{
    // 저장된 씬을 실제로 띄워, 3단계에서 붙인 화면들이 Core 진행과 맞물려
    // 도는지 본다. 이 파일이 통과하려면 씬이 먼저 구성되어 있어야 한다
    // (메뉴 GameName > 기억 방 씬 구성).
    public class MemoryRoomProgressionPlayTests
    {
        private const string SceneName = "LDY_GameScene";
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");
        private static readonly MemoryRoomId Room2 = new MemoryRoomId("room-2");
        private static readonly MemoryRoomId Room3 = new MemoryRoomId("room-3");

        private GameSession _session;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            _session = Object.FindFirstObjectByType<GameSessionBootstrap>(FindObjectsInactive.Include).Session;
            Assert.IsNotNull(_session, "GameSession이 조립되지 않았다.");
            Assert.AreEqual(Room1, _session.CurrentRoomId, "첫 방이 room-1이 아니다.");
            yield return null;
        }

        private ChoiceId FirstCorrectChoice()
        {
            foreach (var choice in _session.Dialogue.VisibleChoices())
            {
                if (choice.IsCorrect)
                    return choice.Id;
            }

            Assert.Fail("현재 라인에 정답 선택지가 없다 — 빈 대사로도 흐름이 끝까지 가야 한다.");
            return default;
        }

        private ChoiceId FirstWrongChoice()
        {
            foreach (var choice in _session.Dialogue.VisibleChoices())
            {
                if (!choice.IsCorrect)
                    return choice.Id;
            }

            Assert.Fail("현재 라인에 오답 선택지가 없다.");
            return default;
        }

        // 정답만 골라 지금 방의 대화를 끝까지 소진한다. 방이 바뀌면 멈춘다 —
        // 소진 즉시 다음 방 대화가 이어지므로 경계에서 끊어야 방별로 확인할 수 있다.
        // ClueSelection 줄을 만나면 들고 있는 단서로 답하고(있으면), 없으면 넘어간다.
        private void ExhaustCurrentRoom()
        {
            var room = _session.CurrentRoomId;
            for (var guard = 0;
                 guard < 32 && _session.CurrentRoomId.Equals(room) && _session.Dialogue.CurrentLine != null;
                 guard++)
            {
                if (_session.Dialogue.CurrentLine.PromptKind == DialoguePromptKind.ClueSelection)
                {
                    var clues = _session.Dialogue.SelectableClues();
                    if (clues.Count > 0)
                        _session.Dialogue.SelectClue(clues[0].Key);
                    else
                        _session.Dialogue.SkipClueSelection();
                    continue;
                }

                _session.Dialogue.Select(FirstCorrectChoice());
            }
        }

        private static MemoryRoomSpaceView SpaceView() =>
            Object.FindFirstObjectByType<MemoryRoomSpaceView>(FindObjectsInactive.Include);

        private static VisualElement HudRoot() =>
            Object.FindFirstObjectByType<MemoryRoomBootstrap>(FindObjectsInactive.Include)
                .GetComponent<UIDocument>().rootVisualElement;

        [UnityTest]
        public IEnumerator 세_방을_대화_소진으로만_순서대로_통과한다()
        {
            var started = new List<MemoryRoomId>();
            var completed = false;
            _session.EventBus.Subscribe<RoomStartedEvent>(e => started.Add(e.RoomId));
            _session.EventBus.Subscribe<RunCompletedEvent>(_ => completed = true);

            ExhaustCurrentRoom(); // room-1 소진
            yield return null;
            Assert.AreEqual(Room2, _session.CurrentRoomId, "room-1 대화를 마쳤는데 room-2로 넘어가지 않았다.");

            ExhaustCurrentRoom(); // room-2 소진
            yield return null;
            Assert.AreEqual(Room3, _session.CurrentRoomId);

            ExhaustCurrentRoom(); // room-3 소진
            yield return null;

            Assert.IsTrue(completed, "세 방을 마쳤는데 런이 끝나지 않았다.");
            CollectionAssert.AreEqual(new[] { Room2, Room3 }, started, "방 전환이 순서대로 한 번씩만 일어나야 한다.");

            // TEMP 종료 표시 — 대화 패널에 "끝난다" 문구가 떠야 한다.
            var body = HudRoot().Q<VisualElement>("dialogue-body");
            var text = string.Join(" ", body.Children().OfType<Label>().Select(l => l.text));
            StringAssert.Contains("끝난다", text);
        }

        [UnityTest]
        public IEnumerator 신뢰_0으로도_다음_방으로_넘어간다()
        {
            var failed = false;
            var lowestTrust = _session.Trust.Current;
            _session.EventBus.Subscribe<RoomFailedEvent>(_ => failed = true);
            _session.EventBus.Subscribe<TrustChangedEvent>(e => lowestTrust = Mathf.Min(lowestTrust, e.Current));

            // room-1은 오답 루프 선택지가 있다. 신뢰 3 → 오답 3번이면 0.
            for (var i = 0; i < 3 && _session.CurrentRoomId.Equals(Room1); i++)
            {
                _session.Dialogue.Select(FirstWrongChoice());
                yield return null;
            }

            Assert.AreEqual(0, lowestTrust, "오답을 반복했는데 신뢰가 0에 닿지 않았다.");
            Assert.IsTrue(failed, "신뢰 0인데 방이 실패로 닫히지 않았다.");
            Assert.AreEqual(Room2, _session.CurrentRoomId, "실패한 방에서 다음 방으로 넘어가지 않았다.");
            // 다음 방에서는 신뢰가 시작값으로 리셋된다(런 자원과 달리 방 스코프).
            Assert.AreEqual(3, _session.Trust.Current);
        }

        [UnityTest]
        public IEnumerator 플레이어가_방을_바꾸는_출입구_오브젝트는_존재하지_않는다()
        {
            yield return null;

            var space = SpaceView();
            var names = space.GetComponentsInChildren<Transform>(includeInactive: true)
                .Select(t => t.name)
                .ToArray();

            foreach (var forbidden in new[] { "Exit", "Door", "Ladder", "Stair" })
            {
                Assert.IsFalse(
                    names.Any(n => n.Contains(forbidden)),
                    $"방에 '{forbidden}' 오브젝트가 있다 — 방 전환은 RunProgressor만 한다.");
            }
        }

        [UnityTest]
        public IEnumerator 신뢰가_낮아지면_가시_밖_단서의_콜라이더가_꺼진다()
        {
            // room-1의 모포(clue-r1-blanket)는 비율 0.18. 오답 2번(신뢰 1, 창
            // [0.25, 0.75])이면 밖으로 밀린다.
            _session.Dialogue.Select(FirstWrongChoice());
            _session.Dialogue.Select(FirstWrongChoice());
            yield return null;
            yield return null;

            Assert.AreEqual(1, _session.Trust.Current);

            var blanket = SpaceView()
                .GetComponentsInChildren<ClueSceneObject>(includeInactive: true)
                .Single(c => c.name.Contains("blanket"));

            Assert.IsFalse(blanket.IsAccessible, "가시 밖 단서가 아직 접근 가능으로 표시된다.");
            Assert.IsFalse(
                blanket.GetComponent<BoxCollider2D>().enabled,
                "가시 밖 단서의 콜라이더가 살아 있다 — 눈에 안 보이는데 집힌다.");

            // 그 자리를 물리로 찍어도 이 단서는 잡히지 않아야 한다.
            var world = blanket.transform.position;
            var hit = Physics2D.OverlapPoint(new Vector2(world.x, world.y));
            Assert.IsFalse(
                hit != null && hit.GetComponent<ClueSceneObject>() == blanket,
                "가시 밖 단서 자리를 찍었는데 그 단서가 잡혔다.");
        }

        [UnityTest]
        public IEnumerator 단서를_누르면_설명_창에_표시_이름이_뜬다()
        {
            var blanket = SpaceView()
                .GetComponentsInChildren<ClueSceneObject>(includeInactive: true)
                .Single(c => c.name.Contains("blanket"));

            blanket.Activate();
            yield return null;

            var host = Object.FindFirstObjectByType<OverlayPanelHost>(FindObjectsInactive.Include);
            var nameLabel = host.RootOf(OverlayPanel.ClueZoom).Q<Label>("clue-zoom-clue-name");
            Assert.IsNotNull(nameLabel, "설명 창에 이름 요소가 없다(씬 구성이 필요하다).");
            Assert.AreEqual("낡은 모포", nameLabel.text);
        }

        [UnityTest]
        public IEnumerator 방2_단서로_답하는_줄_정답_단서로_정답_분기로_간다()
        {
            ExhaustCurrentRoom(); // room-1 소진 → room-2
            yield return null;
            Assert.AreEqual(Room2, _session.CurrentRoomId);

            // 정답 단서(교복 리본)를 수집한다.
            Assert.IsTrue(
                _session.ClueCollectionProcessor.Collect(new ClueId("clue-r2-ribbon")).Succeeded, "리본 수집 실패.");

            // ClueSelection 줄(r2-q)까지 텍스트 선택지로 진행.
            for (var g = 0;
                 g < 12 && _session.Dialogue.CurrentLine != null
                        && _session.Dialogue.CurrentLine.PromptKind != DialoguePromptKind.ClueSelection;
                 g++)
            {
                _session.Dialogue.Select(FirstCorrectChoice());
            }

            Assert.AreEqual(
                DialoguePromptKind.ClueSelection, _session.Dialogue.CurrentLine.PromptKind, "r2-q에 도달하지 못했다.");
            Assert.IsTrue(
                _session.Dialogue.SelectableClues().Any(c => c.Key.Equals(new ClueId("clue-r2-ribbon"))),
                "들고 있는 리본이 답 목록에 없다.");

            _session.Dialogue.SelectClue(new ClueId("clue-r2-ribbon"));

            Assert.AreEqual(new DialogueLineId("r2-q-right"), _session.Dialogue.CurrentLineId, "정답 분기로 가지 않았다.");
        }

        [UnityTest]
        public IEnumerator 방2_단서로_답하는_줄_오답이면_서브체인_거쳐_메인_줄기로_합류한다()
        {
            ExhaustCurrentRoom(); // room-1 → room-2
            yield return null;

            Assert.IsTrue(
                _session.ClueCollectionProcessor.Collect(new ClueId("clue-r2-tape")).Succeeded, "테이프 수집 실패.");

            for (var g = 0;
                 g < 12 && _session.Dialogue.CurrentLine != null
                        && _session.Dialogue.CurrentLine.PromptKind != DialoguePromptKind.ClueSelection;
                 g++)
            {
                _session.Dialogue.Select(FirstCorrectChoice());
            }

            // 엉뚱한 물건(테이프)으로 답한다 → 오답 서브체인.
            _session.Dialogue.SelectClue(new ClueId("clue-r2-tape"));
            Assert.AreEqual(new DialogueLineId("r2-q-wrong-1"), _session.Dialogue.CurrentLineId);

            _session.Dialogue.Select(FirstCorrectChoice()); // wrong-1 → wrong-2
            _session.Dialogue.Select(FirstCorrectChoice()); // wrong-2 → r2-end (합류)

            Assert.AreEqual(new DialogueLineId("r2-end"), _session.Dialogue.CurrentLineId, "메인 줄기로 합류하지 않았다.");
        }

        [UnityTest]
        public IEnumerator 마스크_구간을_해금하면_화면에_그려진_텍스트가_원문으로_바뀐다()
        {
            // room-1의 모포(clue-r1-blanket, 파란색)를 수집·추출해 파랑 1을 얻는다.
            var blanketId = new ClueId("clue-r1-blanket");

            Assert.IsTrue(_session.ClueCollectionProcessor.Collect(blanketId).Succeeded, "모포 수집 실패.");
            Assert.IsTrue(_session.ExtractionProcessor.Extract(blanketId).Succeeded, "모포 추출 실패.");
            yield return null;

            var body = HudRoot().Q<VisualElement>("dialogue-body");
            Assert.IsNotNull(body, "대화 본문 요소가 없다(씬 구성이 필요하다).");

            var before = body.Children().OfType<Label>().Select(l => l.text).ToArray();
            Assert.IsFalse(
                before.Any(t => t.Contains("그 여름밤 옥상")),
                "해금 전인데 원문('그 여름밤 옥상')이 이미 화면에 그려져 있다.");

            Assert.IsTrue(
                _session.CensorUnlock.Unlock(new CensorKey("rooftop-blanket"), blanketId).Succeeded, "해금 실패.");
            yield return null;

            var after = body.Children().OfType<Label>().Select(l => l.text).ToArray();
            Assert.IsTrue(
                after.Any(t => t.Contains("그 여름밤 옥상")),
                "해금 후 원문('그 여름밤 옥상')이 화면에 그려져야 한다.");
        }
    }
}
