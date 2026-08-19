using System.Collections;
using System.Linq;
using System.Reflection;
using GameName.Core.Clues;
using GameName.Core.Judging;
using GameName.Core.MemoryRooms;
using GameName.UI.MemoryRoom.Space;
using GameName.UI.Session;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameName.Tests.PlayMode
{
    // 방을 옮겼을 때 씬 오브젝트가 실제로 어떻게 바뀌는지 확인한다.
    //
    // 기존 테스트에는 이 구간이 통째로 비어 있었다. 방 하나 안에서 일어나는
    // 일(단서가 놓이는가, 눌리는가, 집으면 사라지는가)은 모두 검사했지만, 방을
    // 옮긴 뒤의 상태 — 이전 방 오브젝트가 남지 않았는가, 새 방 단서가 전부
    // 집히는가 — 는 아무도 보지 않았다. 증상 3과 4가 정확히 그 구간에서 나왔다.
    //
    // 프레임을 넘기지 않고 확인하는 것도 의도다. 이전 구현은 파괴가 프레임
    // 끝까지 미뤄져서, 한 프레임만 기다려 주면 어떤 검사든 통과했다. 그래서
    // 프레임을 기다리는 테스트는 이 버그를 영원히 잡지 못한다.
    public class MemoryRoomTransitionTests
    {
        private const string SceneName = "LDY_GameScene";
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");
        private static readonly MemoryRoomId Room2 = new MemoryRoomId("room-2");

        private GameSession _session;

        [UnitySetUp]
        public IEnumerator EnterFirstMemoryRoom()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            _session = Object.FindFirstObjectByType<GameSessionBootstrap>(FindObjectsInactive.Include).Session;

            var guard = 0;
            while (!_session.DialogueProgressor.IsFinished && guard++ < 100)
                _session.DialogueProgressor.Advance(0);

            Assert.IsTrue(_session.CommissionSession.TryAdvanceToMemory(), "기억으로 진입하지 못했다.");
            yield return null;

            Assert.IsTrue(
                _session.MovementProcessor.Move(MemoryGraphNodeId.OfRoom(Room1)).Succeeded,
                "첫 기억 방으로 이동하지 못했다.");
            yield return null;
        }

        private static MemoryRoomSpaceView SpaceView() =>
            Object.FindFirstObjectByType<MemoryRoomSpaceView>(FindObjectsInactive.Include);

        private static ClueSceneObject[] CluesInScene() =>
            SpaceView().GetComponentsInChildren<ClueSceneObject>(includeInactive: true);

        // 씬 오브젝트는 식별자를 밖으로 내주지 않는다(그게 이번 설계의 경계다).
        // 테스트만 그 비공개 필드를 리플렉션으로 읽는다.
        private static ClueId ClueIdOf(ClueSceneObject sceneObject) =>
            (ClueId)typeof(ClueSceneObject)
                .GetField("_clueId", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(sceneObject);

        // 방 1 위의 사다리는 방 1이 복원되어야 열린다. 시향을 통째로 재현하는
        // 대신 복원 사실만 직접 보고한다 — 이 테스트가 확인하려는 것은 시향
        // 규칙이 아니라 방을 옮긴 뒤의 씬 상태다.
        private void UnlockLadderFromRoom1() =>
            _session.RestorationTracker.ReportJudgement(
                Room1, new ScentJudgementResult(true, FeedbackStage.PianoAndViolinAndDrum, 1d));

        private void MoveToRoom2()
        {
            UnlockLadderFromRoom1();

            var result = _session.MovementProcessor.Move(MemoryGraphNodeId.OfRoom(Room2));
            Assert.IsTrue(result.Succeeded, $"방 2로 이동하지 못했다({result.FailureReason}).");
        }

        [UnityTest]
        public IEnumerator 방을_옮기면_이전_방의_단서_오브젝트가_남지_않는다()
        {
            var before = CluesInScene().Select(ClueIdOf).ToArray();
            Assert.IsNotEmpty(before, "방 1에 단서 오브젝트가 없다.");

            MoveToRoom2();

            // 프레임을 넘기지 않는다 — 옮긴 그 순간부터 이전 방의 단서는 마우스
            // 판정에 잡히면 안 된다.
            var after = CluesInScene().Select(ClueIdOf).ToArray();

            CollectionAssert.IsEmpty(
                after.Intersect(before).ToArray(), "이전 방의 단서 오브젝트가 그대로 남아 있다.");

            yield return null;

            CollectionAssert.IsEmpty(
                CluesInScene().Select(ClueIdOf).Intersect(before).ToArray(),
                "한 프레임 뒤에도 이전 방의 단서 오브젝트가 남아 있다.");
        }

        [UnityTest]
        public IEnumerator 방을_옮기면_새_방의_단서가_전부_집힌다()
        {
            MoveToRoom2();
            yield return null;

            var clues = CluesInScene();
            Assert.IsNotEmpty(clues, "방 2에 단서 오브젝트가 하나도 놓이지 않았다.");

            foreach (var clue in clues)
            {
                var result = _session.ClueCollector.Collect(ClueIdOf(clue));
                Assert.IsTrue(
                    result.Succeeded, $"{clue.name}을(를) 집지 못했다({result.FailureReason}).");
            }
        }

        [UnityTest]
        public IEnumerator 방이_아닌_곳으로_나가면_단서_오브젝트가_남지_않는다()
        {
            Assert.IsNotEmpty(CluesInScene(), "방 1에 단서 오브젝트가 없다.");

            Assert.IsTrue(_session.MovementProcessor.Move(_session.MemoryEntryNodeId).Succeeded);

            Assert.AreEqual(
                0, CluesInScene().Length, "계단에 있는데 이전 방의 단서 오브젝트가 살아 있다.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator 버린_단서는_실제_오브젝트로_방에_나타난다()
        {
            var target = CluesInScene().First();
            var clueId = ClueIdOf(target);
            Assert.IsTrue(_session.ClueCollector.Collect(clueId).Succeeded, "단서를 집지 못했다.");

            var carried = _session.Inventory.Items.OfType<ClueInfo>().First(c => c.Id.Equals(clueId));
            Assert.IsTrue(_session.ClueDropProcessor.Drop(carried).Succeeded, "단서를 버리지 못했다.");

            // 버리자마자 씬에 있어야 한다 — 프레임을 넘겨야 나타난다면 그것도
            // 버그다(그 사이 마우스는 없는 단서를 가리키고 있다).
            var placed = CluesInScene().Select(ClueIdOf).ToArray();
            CollectionAssert.Contains(placed, clueId, "버린 단서가 방에 오브젝트로 나타나지 않았다.");

            yield return null;
        }

        // 증상 4. 버린 단서가 이미 놓여 있던 단서 위에 얹히면 안 된다.
        [UnityTest]
        public IEnumerator 버린_단서는_이미_놓인_단서와_겹치지_않는다()
        {
            var target = CluesInScene().First();
            var clueId = ClueIdOf(target);
            Assert.IsTrue(_session.ClueCollector.Collect(clueId).Succeeded, "단서를 집지 못했다.");

            MoveToRoom2();
            yield return null;

            var carried = _session.Inventory.Items.OfType<ClueInfo>().First(c => c.Id.Equals(clueId));
            Assert.IsTrue(_session.ClueDropProcessor.Drop(carried).Succeeded, "단서를 버리지 못했다.");
            yield return null;

            var colliders = CluesInScene().Select(c => c.GetComponent<BoxCollider2D>()).ToArray();
            Assert.Greater(colliders.Length, 1, "겹침을 확인하려면 단서가 둘 이상 있어야 한다.");

            for (var i = 0; i < colliders.Length; i++)
            {
                for (var j = i + 1; j < colliders.Length; j++)
                {
                    Assert.IsFalse(
                        Overlaps(colliders[i], colliders[j]),
                        $"{colliders[i].name}과(와) {colliders[j].name}이 겹쳐 놓였다.");
                }
            }
        }

        // 나란히 붙은 두 판정 영역은 경계선을 공유한다. Bounds.Intersects는 그
        // 맞닿은 상태도 겹친 것으로 치므로, 부동소수 오차만큼 줄여 놓고 본다.
        private static bool Overlaps(Collider2D left, Collider2D right)
        {
            const float touchTolerance = 0.001f;

            var shrunk = left.bounds;
            shrunk.Expand(-touchTolerance);
            return shrunk.Intersects(right.bounds);
        }
    }
}
