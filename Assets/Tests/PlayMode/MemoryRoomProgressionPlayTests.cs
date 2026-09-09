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

        // 데모 각 방에서, 대화 국면으로 넘어가는 데 쓸 단서 후보들. 방당
        // InvestigationsPerRoom(데모 기본 3)개를 집으면 조사 국면이 끝난다.
        private static readonly Dictionary<string, string[]> InvestigateWith =
            new Dictionary<string, string[]>
            {
                ["room-1"] = new[]
                {
                    "clue-r1-blanket", "clue-r1-picturebook", "clue-r1-cicada-net",
                    "clue-r1-drawing", "clue-r1-star-poster",
                },
                ["room-2"] = new[]
                {
                    "clue-r2-photo", "clue-r2-ribbon", "clue-r2-letter",
                    "clue-r2-tape", "clue-r2-band-poster",
                },
                ["room-3"] = new[]
                {
                    "clue-r3-watch", "clue-r3-keychain", "clue-r3-coin",
                    "clue-r3-xray-poster", "clue-r3-notice-poster",
                },
            };

        // 조사 국면을 끝내 대화 국면으로 넘어간다. preferred에 준 단서를 먼저
        // 집어(그 방의 답으로 쓰려는 것), 남은 조사 횟수를 다른 단서로 채운다.
        private void ReachDialoguePhase(params string[] preferred)
        {
            var pool = InvestigateWith[_session.CurrentRoomId.Value];
            var order = preferred.Concat(pool.Where(id => !preferred.Contains(id)));

            foreach (var id in order)
            {
                if (_session.RoomPhase.Current == RoomPhase.Dialogue)
                    break;
                _session.ClueCollectionProcessor.Collect(new ClueId(id));
            }
        }

        // 지금 방의 대화를 끝까지 소진한다 — 모든 줄이 "가진 물건으로 답하기"라,
        // 답을 모르는 테스트는 매 줄 넘어간다(SkipClueSelection). 대화가 끝나면
        // 방은 자동으로 안 닫힌다 — "다음으로" 버튼을 눌러야 하고, 그건 지금
        // 방의 RoomClearedEvent를 낸다.
        private void ExhaustCurrentRoom()
        {
            ReachDialoguePhase();
            var room = _session.CurrentRoomId;
            for (var guard = 0;
                 guard < 32 && _session.CurrentRoomId.Equals(room) && _session.Dialogue.CurrentLine != null;
                 guard++)
            {
                _session.Dialogue.SkipClueSelection();
            }

            if (_session.CurrentRoomId.Equals(room) && _session.Dialogue.CurrentLine == null)
                ClickAdvanceButton();
        }

        // 지금 방 대화가 lineId에 닿을 때까지 넘어간다.
        private void SkipUntilLine(string lineId)
        {
            for (var g = 0; g < 12 && _session.Dialogue.CurrentLineId?.Value != lineId
                                   && _session.Dialogue.CurrentLine != null; g++)
                _session.Dialogue.SkipClueSelection();
        }

        // 대화 패널에 뜬 "다음으로" 버튼을 누른다. 버튼이 실제로 떠 있는지 확인한
        // 뒤, 그 버튼이 하는 일(지금 방의 RoomClearedEvent)을 그대로 일으킨다 —
        // 버튼→이벤트 배선 자체는 DialoguePanelControllerTests가 검증한다.
        private void ClickAdvanceButton()
        {
            var choices = HudRoot().Q<VisualElement>("dialogue-choices");
            var next = choices.Children().OfType<Button>().FirstOrDefault(b => b.text == "다음으로");
            Assert.IsNotNull(next, "대화가 끝났는데 '다음으로' 버튼이 없다.");

            _session.EventBus.Publish(new RoomClearedEvent(_session.CurrentRoomId));
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
        public IEnumerator 한_방에서_오답만_내도_방은_무너지지_않고_대화가_끝까지_진행된다()
        {
            // 2차 모델: 오답 자체는 신뢰를 깎지 않는다(신뢰는 안정 축 이탈에만
            // 반응하고, 한 방의 피드백 델타만으로는 자유 폭을 못 넘는다). 오답
            // 서브체인을 지나 대화가 정상적으로 끝나야 한다.
            var failed = false;
            _session.EventBus.Subscribe<RoomFailedEvent>(_ => failed = true);

            ReachDialoguePhase("clue-r1-star-poster"); // 어느 질문에도 안 맞는 물건

            for (var i = 0; i < 12 && _session.Dialogue.CurrentLine != null; i++)
            {
                var clues = _session.Dialogue.SelectableClues();
                if (clues.Count > 0)
                    _session.Dialogue.SelectClue(clues[0].Id);
                else
                    _session.Dialogue.SkipClueSelection();
                yield return null;
            }

            Assert.IsFalse(failed, "오답만 냈는데 방이 무너졌다 — 2차 모델에서 오답은 신뢰를 깎지 않는다.");
            Assert.IsTrue(_session.Trust.Current > 0, "오답만으로 신뢰가 0이 됐다.");
            Assert.IsNull(_session.Dialogue.CurrentLine, "대화가 끝까지 진행되지 않았다.");
            Assert.AreEqual(Room1, _session.CurrentRoomId, "'다음으로'를 안 눌렀는데 방이 넘어갔다.");
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
        [Ignore("2차 모델에서 신뢰를 낮추는 경로가 '안정 축 누적 이탈'로 바뀌어 " +
                "한 방 안에서는 재현 불가(오답은 신뢰를 안 깎는다). 신뢰↔가시 비율↔단서 " +
                "접근성 배선 자체는 CenteredClueAccessPolicyTests·StepVisibilityPolicyTests·" +
                "MemoryRoomScreenTrustBoundaryTests가 커버. 콜라이더 육안 검증은 에디터에서.")]
        public IEnumerator 신뢰가_낮아지면_가시_밖_단서의_콜라이더가_꺼진다()
        {
            foreach (var id in new[] { "clue-r1-picturebook", "clue-r1-drawing" })
                Assert.IsTrue(
                    _session.ClueCollectionProcessor.Collect(new ClueId(id)).Succeeded, id + " 수집 실패");

            _session.Dialogue.SelectClue(new ClueId("clue-r1-picturebook"));
            yield return null;
            _session.Dialogue.SelectClue(new ClueId("clue-r1-drawing"));
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
        public IEnumerator 방2_정답_물건을_내밀면_정답_분기로_간다()
        {
            ExhaustCurrentRoom(); // room-1 소진 → room-2
            yield return null;
            Assert.AreEqual(Room2, _session.CurrentRoomId);

            // r2-b "그때 쥐고 있던 것"의 정답은 리본/편지(room2.heldItem) —
            // 조사 국면에서 리본을 손에 넣어 두고 대화로 넘어간다.
            ReachDialoguePhase("clue-r2-ribbon");

            SkipUntilLine("r2-b");
            Assert.AreEqual(new DialogueLineId("r2-b"), _session.Dialogue.CurrentLineId, "r2-b에 도달하지 못했다.");

            _session.Dialogue.SelectClue(new ClueId("clue-r2-ribbon"));

            Assert.AreEqual(new DialogueLineId("r2-c"), _session.Dialogue.CurrentLineId, "정답 분기로 가지 않았다.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 방2_엉뚱한_물건을_내밀면_오답_분기로_합류한다()
        {
            ExhaustCurrentRoom(); // room-1 → room-2
            yield return null;
            Assert.AreEqual(Room2, _session.CurrentRoomId);

            ReachDialoguePhase("clue-r2-band-poster");

            SkipUntilLine("r2-b");
            _session.Dialogue.SelectClue(new ClueId("clue-r2-band-poster")); // room2.band ≠ heldItem → 오답

            Assert.AreEqual(new DialogueLineId("r2-c"), _session.Dialogue.CurrentLineId, "오답도 r2-c로 합류한다.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 방1_대사_원문이_화면에_그대로_그려진다()
        {
            ReachDialoguePhase(); // 조사 국면을 끝내야 대화가 그려진다
            yield return null;

            var body = HudRoot().Q<VisualElement>("dialogue-body");
            Assert.IsNotNull(body, "대화 본문 요소가 없다(씬 구성이 필요하다).");

            var texts = body.Children().OfType<Label>().Select(l => l.text).ToArray();
            Assert.IsTrue(
                texts.Any(t => t.Contains("그 여름밤 옥상")),
                "방1 첫 대사 원문이 화면에 그려져야 한다.");
            yield return null;
        }
    }
}
