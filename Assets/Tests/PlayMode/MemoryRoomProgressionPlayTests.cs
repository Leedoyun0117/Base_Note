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

        // 지금 방의 대화를 끝까지 소진한다 — 모든 줄이 "가진 물건으로 답하기"라,
        // 답을 모르는 테스트는 매 줄 넘어간다(SkipClueSelection: 신뢰 안 깎이고
        // 다음 줄로). 대화가 끝나면 방은 자동으로 안 닫힌다 — "다음으로" 버튼을
        // 눌러야 하고, 그건 지금 방의 RoomClearedEvent를 낸다.
        private void ExhaustCurrentRoom()
        {
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
        public IEnumerator 틀린_물건을_세_번_내밀면_방이_무너지고_런이_끝난다()
        {
            var failed = false;
            var completed = false;
            _session.EventBus.Subscribe<RoomFailedEvent>(_ => failed = true);
            _session.EventBus.Subscribe<RunCompletedEvent>(_ => completed = true);

            // 정답이 아닌 물건 3개를 손에 든다(가방 3칸).
            foreach (var id in new[] { "clue-r1-picturebook", "clue-r1-drawing", "clue-r1-star-poster" })
                Assert.IsTrue(
                    _session.ClueCollectionProcessor.Collect(new ClueId(id)).Succeeded, id + " 수집 실패");

            for (var i = 0; i < 6 && _session.CurrentRoomId.Equals(Room1) && _session.Trust.Current > 0; i++)
            {
                var clues = _session.Dialogue.SelectableClues();
                if (clues.Count == 0)
                    break;
                _session.Dialogue.SelectClue(clues[0].Key);
                yield return null;
            }

            Assert.AreEqual(0, _session.Trust.Current, "엉뚱한 물건을 세 번 냈는데 신뢰가 0이 아니다.");
            Assert.IsTrue(failed, "신뢰 0인데 방이 무너지지 않았다.");
            Assert.IsTrue(completed, "방이 무너졌는데 런이 끝나지 않았다.");
            Assert.AreEqual(Room1, _session.CurrentRoomId, "실패했는데 다음 방으로 넘어갔다.");

            var body = HudRoot().Q<VisualElement>("dialogue-body");
            var text = string.Join(" ", body.Children().OfType<Label>().Select(l => l.text));
            StringAssert.Contains("끝난다", text);
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
            // 모포(clue-r1-blanket, 비율 0.18)는 방에 놔두고, 다른 물건 2개를
            // 엉뚱하게 내밀어 신뢰를 1로 만든다(창 [0.25, 0.75] → 0.18은 밖).
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

            // r2-b "그때 쥐고 있던 것"의 정답은 리본/편지(room2.heldItem).
            Assert.IsTrue(
                _session.ClueCollectionProcessor.Collect(new ClueId("clue-r2-ribbon")).Succeeded, "리본 수집 실패.");

            SkipUntilLine("r2-b");
            Assert.AreEqual(new DialogueLineId("r2-b"), _session.Dialogue.CurrentLineId, "r2-b에 도달하지 못했다.");

            _session.Dialogue.SelectClue(new ClueId("clue-r2-ribbon"));

            Assert.AreEqual(new DialogueLineId("r2-c"), _session.Dialogue.CurrentLineId, "정답 분기로 가지 않았다.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 방2_엉뚱한_물건을_내밀면_오답_분기로_가고_신뢰가_깎인다()
        {
            ExhaustCurrentRoom(); // room-1 → room-2
            yield return null;
            Assert.AreEqual(Room2, _session.CurrentRoomId);
            var trustBefore = _session.Trust.Current;

            Assert.IsTrue(
                _session.ClueCollectionProcessor.Collect(new ClueId("clue-r2-band-poster")).Succeeded, "포스터 수집 실패.");

            SkipUntilLine("r2-b");
            _session.Dialogue.SelectClue(new ClueId("clue-r2-band-poster")); // room2.band ≠ heldItem → 오답

            Assert.AreEqual(new DialogueLineId("r2-c"), _session.Dialogue.CurrentLineId, "오답도 r2-c로 합류한다.");
            Assert.AreEqual(trustBefore - 1, _session.Trust.Current, "엉뚱한 물건을 내밀면 신뢰가 깎인다.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 방1_대사_원문이_화면에_그대로_그려진다()
        {
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
