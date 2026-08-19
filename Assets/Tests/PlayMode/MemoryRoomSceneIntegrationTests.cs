using System.Collections;
using System.Linq;
using GameName.Core.Clues;
using GameName.Core.MemoryRooms;
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
    // 실제 씬을 실제로 띄워서 확인하는 테스트.
    //
    // EditMode 테스트는 규칙과 계산만 본다 — 오브젝트가 진짜로 생기는지, 씬
    // 배선이 실제로 이어져 있는지, 화면이 켜지고 꺼지는지는 그쪽에서 증명할 수
    // 없다. 그 구멍을 메우는 것이 이 파일의 목적이다. 그래서 여기서는 가짜를
    // 하나도 쓰지 않고 저장된 씬을 그대로 연다.
    //
    // 이 테스트가 통과하려면 씬이 먼저 구성되어 있어야 한다(메뉴
    // GameName > 기억 방 씬 구성). 구성되지 않은 씬에서 실패하는 것은 정상이며,
    // 그 실패 자체가 "씬 구성이 빠졌다"는 신호다.
    public class MemoryRoomSceneIntegrationTests
    {
        // 이름으로 연다 — PlayMode 테스트는 에디터 전용 API를 쓸 수 없으므로,
        // 이 씬이 Build Settings에 등록되어 있어야 한다(등록되어 있다).
        private const string SceneName = "LDY_GameScene";
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");

        private GameSession _session;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);

            // 씬 로드는 다음 프레임에 끝나고, 그 뒤 Awake/OnEnable이 한 바퀴
            // 돌아야 하므로 두 프레임을 기다린다.
            yield return null;
            yield return null;

            var bootstrap = Object.FindFirstObjectByType<GameSessionBootstrap>(FindObjectsInactive.Include);
            Assert.IsNotNull(bootstrap, "GameSessionBootstrap이 씬에 없다.");
            _session = bootstrap.Session;
            Assert.IsNotNull(_session, "GameSession이 조립되지 않았다.");
        }

        // 대화를 끝까지 넘긴 뒤 기억으로 들어가고, 첫 기억 방까지 이동한다.
        // 화면 전환(SceneScreenSwitcher)은 이벤트로 저절로 따라온다.
        private IEnumerator EnterFirstMemoryRoom()
        {
            // 대본이 선형이라 옵션 0만 계속 고르면 끝난다. 그래도 무한 루프에
            // 빠지지 않도록 상한을 둔다 — 대본이 바뀌어 진행되지 않게 되면
            // 테스트가 멈추는 대신 실패해야 한다.
            var guard = 0;
            while (!_session.DialogueProgressor.IsFinished && guard++ < 100)
                _session.DialogueProgressor.Advance(0);

            Assert.IsTrue(_session.DialogueProgressor.IsFinished, "대화가 끝나지 않았다.");

            Assert.IsTrue(_session.CommissionSession.TryAdvanceToMemory(), "기억으로 진입하지 못했다.");
            yield return null;

            var moveResult = _session.MovementProcessor.Move(MemoryGraphNodeId.OfRoom(Room1));
            Assert.IsTrue(moveResult.Succeeded, "첫 기억 방으로 이동하지 못했다.");
            yield return null;
        }

        private static MemoryRoomSpaceView FindSpaceView() =>
            Object.FindFirstObjectByType<MemoryRoomSpaceView>(FindObjectsInactive.Include);

        private static ClueSceneObject[] FindClueObjects(MemoryRoomSpaceView view) =>
            view.GetComponentsInChildren<ClueSceneObject>(includeInactive: true);

        private static MemoryRoomLayout LoadLayout()
        {
            var bootstrap = Object.FindFirstObjectByType<GameName.UI.MemoryRoom.MemoryRoomBootstrap>(
                FindObjectsInactive.Include);
            var field = typeof(GameName.UI.MemoryRoom.MemoryRoomBootstrap)
                .GetField("_layoutAsset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var asset = (MemoryRoomLayoutAsset)field.GetValue(bootstrap);
            Assert.IsNotNull(asset, "MemoryRoomBootstrap에 방 치수 에셋이 연결되어 있지 않다.");
            return asset.ToLayout();
        }

        [UnityTest]
        public IEnumerator 씬을_띄우면_기억_방_화면과_방_오브젝트가_실제로_생긴다()
        {
            yield return EnterFirstMemoryRoom();

            var view = FindSpaceView();
            Assert.IsNotNull(view, "MemoryRoomSpaceView가 씬에 없다(씬 구성이 필요하다).");

            var room = view.transform.Find("Room");
            Assert.IsNotNull(room, "방 껍데기 오브젝트가 만들어지지 않았다.");
            Assert.IsTrue(room.gameObject.activeInHierarchy, "기억 방 안인데 방이 보이지 않는다.");

            // 벽·바닥·천장·플레이어가 전부 실제 렌더러로 존재해야 한다.
            foreach (var part in new[] { "BackWall", "Floor", "Ceiling", "LeftWall", "RightWall", "Player" })
            {
                var child = room.Find(part);
                Assert.IsNotNull(child, $"{part}이 만들어지지 않았다.");
                Assert.IsNotNull(child.GetComponent<SpriteRenderer>(), $"{part}에 렌더러가 없다.");
            }
        }

        [UnityTest]
        public IEnumerator 방에_단서_두_종이_실제_오브젝트로_놓인다()
        {
            yield return EnterFirstMemoryRoom();

            var clues = FindClueObjects(FindSpaceView());
            Assert.AreEqual(2, clues.Length, "방에 놓인 단서 오브젝트 개수가 다르다.");

            // 마우스로 집을 수 있으려면 판정용 콜라이더가 있어야 한다.
            foreach (var clue in clues)
                Assert.IsNotNull(clue.GetComponent<BoxCollider2D>(), "단서에 마우스 판정 콜라이더가 없다.");
        }

        [UnityTest]
        public IEnumerator 포스터는_벽_높이에_바닥_물건은_바닥에_실제로_놓인다()
        {
            yield return EnterFirstMemoryRoom();

            var layout = LoadLayout();
            var clues = FindClueObjects(FindSpaceView());
            var heights = clues.Select(c => c.transform.localPosition.y).OrderBy(y => y).ToArray();

            var floorHeight = RoomGeometry.FloorTopY(layout) + layout.FloorObjectSize / 2f;
            var posterHeight = RoomGeometry.FloorTopY(layout) + layout.PosterMountHeight;

            Assert.AreEqual(floorHeight, heights[0], 0.001f, "바닥 물건이 바닥에 놓이지 않았다.");
            Assert.AreEqual(posterHeight, heights[1], 0.001f, "포스터가 벽 높이에 걸리지 않았다.");
        }

        [UnityTest]
        public IEnumerator 방에서_나가는_지점이_실제_오브젝트로_생긴다()
        {
            yield return EnterFirstMemoryRoom();

            var exits = FindSpaceView().GetComponentsInChildren<RoomExitSceneObject>(includeInactive: true);

            // 데모 의뢰 1의 방 1은 계단·분석실로 열려 있고 방 2로 사다리가 있다.
            Assert.AreEqual(3, exits.Length, "드나드는 지점 개수가 그래프와 다르다.");
        }

        [UnityTest]
        public IEnumerator 집은_단서는_방에서_사라지고_버리면_다시_나타난다()
        {
            yield return EnterFirstMemoryRoom();

            var view = FindSpaceView();
            var beforeCount = FindClueObjects(view).Length;

            // 실제 습득 경로(ClueCollector)를 그대로 쓴다.
            var clueId = ReadClueId(FindClueObjects(view)[0]);
            Assert.IsTrue(_session.ClueCollector.Collect(clueId).Succeeded, "단서를 집지 못했다.");

            // 방을 나갔다 돌아오면 방이 다시 그려진다.
            _session.MovementProcessor.Move(_session.MemoryEntryNodeId);
            yield return null;
            _session.MovementProcessor.Move(MemoryGraphNodeId.OfRoom(Room1));
            yield return null;

            Assert.AreEqual(beforeCount - 1, FindClueObjects(view).Length, "집은 단서가 방에서 사라지지 않았다.");

            // 버리면 이벤트로 방이 즉시 다시 그려진다.
            var carried = _session.Inventory.Items.OfType<ClueInfo>().First();
            Assert.IsTrue(_session.ClueDropProcessor.Drop(carried).Succeeded, "단서를 버리지 못했다.");
            yield return null;

            Assert.AreEqual(beforeCount, FindClueObjects(view).Length, "버린 단서가 방에 다시 나타나지 않았다.");
        }

        [UnityTest]
        public IEnumerator 단서를_누르면_확대_화면이_실제로_뜨고_방_조작이_막힌다()
        {
            yield return EnterFirstMemoryRoom();

            var view = FindSpaceView();
            var pointerInput = view.GetComponent<ScenePointerInput>();
            Assert.IsNotNull(pointerInput, "ScenePointerInput이 연결되어 있지 않다.");
            Assert.IsTrue(pointerInput.InputEnabled, "확대 화면을 열기 전에는 방을 조작할 수 있어야 한다.");

            FindClueObjects(view)[0].Activate();
            yield return null;

            Assert.AreEqual(DisplayStyle.Flex, ZoomDisplay(), "단서를 눌렀는데 확대 화면이 뜨지 않았다.");
            Assert.IsFalse(pointerInput.InputEnabled, "확대 화면이 떠 있는데 뒤의 방을 조작할 수 있다.");
        }

        [UnityTest]
        public IEnumerator 오버레이는_처음에_모두_숨겨져_있고_한_번에_하나만_뜬다()
        {
            yield return EnterFirstMemoryRoom();

            var host = Object.FindFirstObjectByType<OverlayPanelHost>(FindObjectsInactive.Include);
            Assert.IsNotNull(host, "OverlayPanelHost가 씬에 없다(씬 구성이 필요하다).");

            foreach (OverlayPanel panel in System.Enum.GetValues(typeof(OverlayPanel)))
                Assert.AreEqual(DisplayStyle.None, DisplayOf(host, panel), $"{panel}이 처음부터 떠 있다.");

            host.Show(OverlayPanel.Inventory);
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, DisplayOf(host, OverlayPanel.Inventory));

            host.Show(OverlayPanel.Journal);
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, DisplayOf(host, OverlayPanel.Journal));
            Assert.AreEqual(DisplayStyle.None, DisplayOf(host, OverlayPanel.Inventory), "두 오버레이가 함께 떠 있다.");
        }

        [UnityTest]
        public IEnumerator 인벤토리_화면의_칸_개수는_Core_용량을_따른다()
        {
            yield return EnterFirstMemoryRoom();

            var host = Object.FindFirstObjectByType<OverlayPanelHost>(FindObjectsInactive.Include);
            host.Show(OverlayPanel.Inventory);
            yield return null;

            var grid = host.RootOf(OverlayPanel.Inventory).Q<VisualElement>("inventory-slot-grid");
            Assert.AreEqual(_session.Inventory.Capacity, grid.childCount);
        }

        [UnityTest]
        public IEnumerator 기억_방을_벗어나면_방이_보이지_않는다()
        {
            yield return EnterFirstMemoryRoom();

            var room = FindSpaceView().transform.Find("Room");
            Assert.IsTrue(room.gameObject.activeInHierarchy);

            _session.MovementProcessor.Move(_session.MemoryEntryNodeId);
            yield return null;

            Assert.IsFalse(room.gameObject.activeInHierarchy, "계단에 있는데도 기억 방이 그려져 있다.");
        }

        private static DisplayStyle ZoomDisplay()
        {
            var host = Object.FindFirstObjectByType<OverlayPanelHost>(FindObjectsInactive.Include);
            return DisplayOf(host, OverlayPanel.ClueZoom);
        }

        private static DisplayStyle DisplayOf(OverlayPanelHost host, OverlayPanel panel) =>
            host.RootOf(panel).style.display.value;

        // 씬 오브젝트는 식별자를 밖으로 내주지 않는다(그게 이번 설계의 경계다).
        // 테스트만 그 비공개 필드를 리플렉션으로 읽는다 — 검증을 위해 런타임
        // 표면을 넓히지 않기 위해서다.
        private static ClueId ReadClueId(ClueSceneObject sceneObject)
        {
            var field = typeof(ClueSceneObject).GetField(
                "_clueId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (ClueId)field.GetValue(sceneObject);
        }
    }
}
