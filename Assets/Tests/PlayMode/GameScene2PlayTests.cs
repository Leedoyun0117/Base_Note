using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.UI.MemoryRoom;
using GameName.UI.Session;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GameName.Tests.PlayMode
{
    // GameScene2 — "앞 두 방만 도는" 씬이 처음부터 끝까지 흐르는지 본다.
    //
    // 이 파일이 통과하려면 씬이 먼저 구성되어 있어야 한다
    // (메뉴 GameName ▸ GameScene2 2방 플레이 구성 → GameName ▸ 기억 방 씬 구성).
    // 씬이 빌드 목록에 없으면(구성 전이면) 무시된다 — 아직 안 만든 씬 때문에
    // 회귀로 잡히지 않게.
    public class GameScene2PlayTests
    {
        private const string SceneName = "LDY_GameScene2";
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");
        private static readonly MemoryRoomId Room2 = new MemoryRoomId("room-2");
        private static readonly MemoryRoomId Room3 = new MemoryRoomId("room-3");

        private GameSession _session;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneName))
            {
                Assert.Ignore(
                    $"{SceneName}이 빌드 설정에 없다 — GameScene2 씬 구성 전이면 정상이다.");
                yield break;
            }

            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            _session = Object.FindFirstObjectByType<GameSessionBootstrap>(FindObjectsInactive.Include)?.Session;
            Assert.IsNotNull(_session, "GameSession이 조립되지 않았다(씬 구성이 필요하다).");
            yield return null;
        }

        // 데모 각 방에서 조사 국면을 끝내는 데 쓸 단서 후보들 — 방당
        // InvestigationsPerRoom(데모 기본 3)개를 집으면 대화 국면으로 넘어간다.
        // 앞 두 방만 도는 씬이라 room-1·room-2만 필요하다.
        // (MemoryRoomProgressionPlayTests와 같은 목록.)
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
            };

        // 조사 국면을 끝내 대화 국면으로 넘어간다. 대화 국면 전이면 대사 줄이
        // 없어(CurrentLine == null) 대화를 소진할 수 없다.
        private void ReachDialoguePhase()
        {
            foreach (var id in InvestigateWith[_session.CurrentRoomId.Value])
            {
                if (_session.RoomPhase.Current == RoomPhase.Dialogue)
                    break;
                _session.ClueCollectionProcessor.Collect(new ClueId(id));
            }
        }

        // 지금 방을 조사 → 대화 소진 → "다음으로"까지 끝낸다. 모든 줄이 "가진
        // 물건으로 답하기"라, 답을 모르는 테스트는 매 줄 넘어간다(SkipClueSelection:
        // 신뢰 안 깎이고 다음 줄로). 대화가 끝나면 방은 자동으로 안 닫힌다 —
        // "다음으로" 버튼(=지금 방의 RoomClearedEvent)을 눌러야 한다.
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

        private static VisualElement HudRoot() =>
            Object.FindFirstObjectByType<MemoryRoomBootstrap>(FindObjectsInactive.Include)
                .GetComponent<UIDocument>().rootVisualElement;

        [UnityTest]
        public IEnumerator 두_방을_대화_소진으로_통과하면_런이_끝난다()
        {
            if (_session == null)
                yield break; // SetUp이 Assert.Ignore한 경우.

            var started = new List<MemoryRoomId>();
            var completed = false;
            _session.EventBus.Subscribe<RoomStartedEvent>(e => started.Add(e.RoomId));
            _session.EventBus.Subscribe<RunCompletedEvent>(_ => completed = true);

            Assert.AreEqual(Room1, _session.CurrentRoomId, "첫 방이 room-1이 아니다.");

            ExhaustCurrentRoom(); // room-1 조사 + 대화 소진
            yield return null;
            Assert.AreEqual(Room2, _session.CurrentRoomId, "room-1을 마쳤는데 room-2로 넘어가지 않았다.");

            ExhaustCurrentRoom(); // room-2 조사 + 대화 소진 → 마지막 방이므로 런 종료
            yield return null;

            Assert.IsTrue(completed, "두 방을 마쳤는데 런이 끝나지 않았다.");
            Assert.AreNotEqual(Room3, _session.CurrentRoomId, "방 3으로 넘어갔다 — 2방 제한이 걸리지 않았다.");
            CollectionAssert.AreEqual(new[] { Room2 }, started, "방 전환은 room-2로 한 번만 일어나야 한다.");

            var body = HudRoot().Q<VisualElement>("dialogue-body");
            var text = string.Join(" ", body.Children().OfType<Label>().Select(l => l.text));
            StringAssert.Contains("끝난다", text);
        }
    }
}
