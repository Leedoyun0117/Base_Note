using System.Collections;
using GameName.Core.MemoryRooms;
using GameName.UI.MemoryRoom.Space;
using GameName.UI.Overlays;
using GameName.UI.Session;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GameName.Tests.PlayMode
{
    // 실제 키보드·마우스 입력까지 확인하는 테스트.
    //
    // 앞의 통합 테스트는 컨트롤러를 직접 불러 "규칙이 이어져 있는가"를 봤다.
    // 여기서는 그 앞단, 즉 "사람이 실제로 키를 누르고 마우스를 움직였을 때
    // 그 입력이 게임까지 도달하는가"를 본다 — 이 프로젝트는 Active Input
    // Handling이 Input System 단독이라, 입력 경로를 잘못 짜면 아무 반응이
    // 없으면서도 컴파일과 로직 테스트는 전부 통과해 버린다. 그 구멍을 막는다.
    //
    // InputTestFixture가 실제 장치 대신 가상 키보드·마우스를 만들어 준다.
    public class MemoryRoomInputTests : InputTestFixture
    {
        private const string SceneName = "LDY_GameScene";
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");

        private Keyboard _keyboard;
        private Mouse _mouse;
        private GameSession _session;

        // 가상 장치는 반드시 InputTestFixture가 입력 시스템을 초기화한 뒤에
        // 붙여야 한다. [UnitySetUp]에서 붙이면 초기화보다 먼저 실행되어 장치에
        // 상태 버퍼가 잡히지 않는다("does not have any associated state").
        public override void Setup()
        {
            base.Setup();

            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
        }

        [UnitySetUp]
        public IEnumerator LoadSceneAndEnterRoom()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            _session = Object.FindFirstObjectByType<GameSessionBootstrap>(FindObjectsInactive.Include).Session;

            yield return null;
        }

        private static OverlayPanelHost Host() =>
            Object.FindFirstObjectByType<OverlayPanelHost>(FindObjectsInactive.Include);

        private static DisplayStyle DisplayOf(OverlayPanel panel) =>
            Host().RootOf(panel).style.display.value;

        // 키를 누른 상태로 한 프레임을 흘려 보내야 컴포넌트의 Update가
        // wasPressedThisFrame을 본다. 그 다음 프레임에 떼어 준다.
        private IEnumerator TapKey(KeyControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        [UnityTest]
        public IEnumerator I_키로_인벤토리_화면이_열리고_닫힌다()
        {
            Assert.AreEqual(DisplayStyle.None, DisplayOf(OverlayPanel.Inventory), "처음부터 떠 있다.");

            yield return TapKey(_keyboard.iKey);
            Assert.AreEqual(DisplayStyle.Flex, DisplayOf(OverlayPanel.Inventory), "I 키에 반응하지 않았다.");

            yield return TapKey(_keyboard.iKey);
            Assert.AreEqual(DisplayStyle.None, DisplayOf(OverlayPanel.Inventory), "I 키로 닫히지 않았다.");
        }

        [UnityTest]
        public IEnumerator 방향키로_플레이어가_좌우로_걷는다()
        {
            var player = Object.FindFirstObjectByType<PlayerCharacter>(FindObjectsInactive.Include);
            Assert.IsNotNull(player, "플레이어 오브젝트가 없다.");

            var startX = player.CurrentX;

            Press(_keyboard.rightArrowKey);
            for (var i = 0; i < 10; i++)
                yield return null;
            Release(_keyboard.rightArrowKey);
            yield return null;

            var afterRight = player.CurrentX;
            Assert.Greater(afterRight, startX, "오른쪽 키를 눌렀는데 움직이지 않았다.");

            Press(_keyboard.leftArrowKey);
            for (var i = 0; i < 10; i++)
                yield return null;
            Release(_keyboard.leftArrowKey);
            yield return null;

            Assert.Less(player.CurrentX, afterRight, "왼쪽 키를 눌렀는데 움직이지 않았다.");
        }

        [UnityTest]
        public IEnumerator 마우스를_단서_위에_올리면_테두리가_켜지고_벗어나면_꺼진다()
        {
            var view = Object.FindFirstObjectByType<MemoryRoomSpaceView>(FindObjectsInactive.Include);
            var clue = view.GetComponentInChildren<ClueSceneObject>(includeInactive: true);
            Assert.IsNotNull(clue, "단서 오브젝트가 없다.");
            Assert.IsFalse(clue.IsOutlineVisible, "가리키기 전부터 테두리가 켜져 있다.");

            var camera = Camera.main;
            var onClue = camera.WorldToScreenPoint(clue.transform.position);

            Set(_mouse.position, new Vector2(onClue.x, onClue.y));
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsTrue(clue.IsOutlineVisible, "단서 위인데 테두리가 켜지지 않았다.");

            // 방 밖(화면 구석)으로 빼면 꺼져야 한다.
            Set(_mouse.position, new Vector2(1f, 1f));
            yield return null;

            Assert.IsFalse(clue.IsOutlineVisible, "단서를 벗어났는데 테두리가 남아 있다.");
        }

        // 바닥 물건이 클릭되지 않던 증상을 막는 회귀 테스트. 포스터만 확인하면
        // 높이가 다른 바닥 물건이 UI나 다른 콜라이더에 가려지는 경우를 놓친다.
        [UnityTest]
        public IEnumerator 포스터와_바닥_물건_모두_마우스_오버가_동작한다()
        {
            var view = Object.FindFirstObjectByType<MemoryRoomSpaceView>(FindObjectsInactive.Include);
            var clues = view.GetComponentsInChildren<ClueSceneObject>(includeInactive: true);
            Assert.AreEqual(2, clues.Length, "방에 단서 두 종이 있어야 한다.");

            // 높이로 종류를 가른다 — 포스터는 벽, 물건은 바닥이다.
            System.Array.Sort(clues, (a, b) => a.transform.localPosition.y.CompareTo(b.transform.localPosition.y));
            var floorObject = clues[0];
            var poster = clues[1];

            foreach (var clue in new[] { floorObject, poster })
            {
                var onClue = Camera.main.WorldToScreenPoint(clue.transform.position);
                Set(_mouse.position, new Vector2(onClue.x, onClue.y));
                yield return new WaitForFixedUpdate();
                yield return null;

                Assert.IsTrue(clue.IsOutlineVisible, $"{clue.name} 위인데 테두리가 켜지지 않았다.");
            }
        }

        [UnityTest]
        public IEnumerator 바닥_물건을_클릭하면_확대_화면이_뜬다()
        {
            var view = Object.FindFirstObjectByType<MemoryRoomSpaceView>(FindObjectsInactive.Include);
            var clues = view.GetComponentsInChildren<ClueSceneObject>(includeInactive: true);
            System.Array.Sort(clues, (a, b) => a.transform.localPosition.y.CompareTo(b.transform.localPosition.y));

            var onClue = Camera.main.WorldToScreenPoint(clues[0].transform.position);
            Set(_mouse.position, new Vector2(onClue.x, onClue.y));
            yield return new WaitForFixedUpdate();
            yield return null;

            Press(_mouse.leftButton);
            yield return null;
            Release(_mouse.leftButton);
            yield return null;

            Assert.AreEqual(
                DisplayStyle.Flex, DisplayOf(OverlayPanel.ClueZoom), "바닥 물건을 클릭했는데 확대 화면이 뜨지 않았다.");
        }

        [UnityTest]
        public IEnumerator 단서를_마우스로_클릭하면_확대_화면이_뜬다()
        {
            var view = Object.FindFirstObjectByType<MemoryRoomSpaceView>(FindObjectsInactive.Include);
            var clue = view.GetComponentInChildren<ClueSceneObject>(includeInactive: true);

            var camera = Camera.main;
            var onClue = camera.WorldToScreenPoint(clue.transform.position);

            Set(_mouse.position, new Vector2(onClue.x, onClue.y));
            yield return new WaitForFixedUpdate();
            yield return null;

            Press(_mouse.leftButton);
            yield return null;
            Release(_mouse.leftButton);
            yield return null;

            Assert.AreEqual(DisplayStyle.Flex, DisplayOf(OverlayPanel.ClueZoom), "클릭했는데 확대 화면이 뜨지 않았다.");
        }
    }
}
