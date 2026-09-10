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
        private static readonly MemoryRoomId Round1 = new MemoryRoomId("round-1");

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

        // RunProgressor가 조립 끝에 첫 라운드로 진입시킨다 — 데모의 첫 라운드는 round-1이다.
        private IEnumerator EnterFirstMemoryRoom()
        {
            Assert.AreEqual(Round1, _session.CurrentRoomId, "첫 라운드가 round-1이 아니다.");
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
        public IEnumerator 방에_놓인_단서가_실제_오브젝트로_두_종_모두_생긴다()
        {
            yield return EnterFirstMemoryRoom();

            var clues = FindClueObjects(FindSpaceView());
            Assert.AreEqual(5, clues.Length, "room-1에 놓인 단서 오브젝트 개수가 저작 데이터와 다르다.");

            // 마우스로 집을 수 있으려면 판정용 콜라이더가 있어야 한다.
            foreach (var clue in clues)
                Assert.IsNotNull(clue.GetComponent<BoxCollider2D>(), "단서에 마우스 판정 콜라이더가 없다.");

            // 포스터는 벽 높이, 바닥 물건은 바닥 — 둘 다 놓여 있어야 한다.
            var heights = clues.Select(c => c.transform.localPosition.y).Distinct().ToArray();
            Assert.GreaterOrEqual(heights.Length, 2, "포스터와 바닥 물건이 서로 다른 높이로 놓이지 않았다.");
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

            // 가장 낮은 것은 바닥 물건, 가장 높은 것은 포스터여야 한다.
            Assert.AreEqual(floorHeight, heights[0], 0.001f, "바닥 물건이 바닥에 놓이지 않았다.");
            Assert.AreEqual(posterHeight, heights[heights.Length - 1], 0.001f, "포스터가 벽 높이에 걸리지 않았다.");
        }

        [UnityTest]
        public IEnumerator 읽은_단서는_방에서_바로_사라진다()
        {
            yield return EnterFirstMemoryRoom();

            var view = FindSpaceView();
            var beforeCount = FindClueObjects(view).Length;

            // 실제 읽기 경로(ClueUseProcessor)를 그대로 쓴다. 성공하면
            // ClueUsedEvent가 발행되어 방이 즉시 다시 그려진다.
            var clueId = ReadClueId(FindClueObjects(view)[0]);
            Assert.IsTrue(_session.ClueUse.Use(clueId).Succeeded, "단서를 읽지 못했다.");
            yield return null;

            Assert.AreEqual(beforeCount - 1, FindClueObjects(view).Length, "읽은 단서가 방에서 사라지지 않았다.");
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

            host.Show(OverlayPanel.ClueZoom);
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, DisplayOf(host, OverlayPanel.ClueZoom));
            Assert.AreEqual(DisplayStyle.None, DisplayOf(host, OverlayPanel.Inventory), "두 오버레이가 함께 떠 있다.");
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
