using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.MemoryRooms;
using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 기억 방을 실제 오브젝트로 그리는 자리. 게임 규칙은 하나도 계산하지 않고,
    // 컨트롤러가 준 지시(무엇을 어디에)를 그대로 오브젝트로 만든다.
    //
    // ── 방 세 개를 어떻게 두는가 ────────────────────────────────────────
    // 방마다 오브젝트를 따로 두고 껐다 켜는 대신, 방 껍데기(벽/바닥/천장/
    // 플레이어) 하나를 재사용하고 내용물(단서·출입구)만 지금 방에 맞게 다시
    // 채운다. 근거는 세 가지다.
    //   1) 세 방의 규격이 같은 데이터(MemoryRoomLayout) 하나에서 나온다.
    //      껍데기를 복제하면 같은 사실이 세 벌 생기고 조용히 어긋날 수 있다.
    //   2) 방 사이 연결은 그래프가 정하지 공간적 인접이 아니다. 방을 나란히
    //      늘어놓으면 걸어서 옆 방에 갈 수 있다는 거짓 신호를 준다.
    //   3) 어느 순간에도 다른 방의 단서 오브젝트가 존재하지 않으므로, 마우스가
    //      다른 방 단서를 집는 사고가 구조적으로 불가능하다.
    //
    // ── 내용물의 수명은 여기 한 곳에서만 바뀐다 ─────────────────────────
    // 밖에서 부를 수 있는 상태 변경은 딱 둘이다: SetContents(지금 방을 이
    // 내용으로 그린다)와 HideRoom(방을 통째로 치운다). 예전에는 "감추기"가
    // 오브젝트를 끄기만 하고 비우지는 않아서, 방을 벗어난 동안 이전 방의 단서가
    // 콜라이더째 살아 있다가 방이 다시 켜지는 순간 새 방 단서와 함께 되살아났다.
    // 위 3번 근거가 실제로는 지켜지지 않고 있었던 것이다.
    //
    // 스프라이트 교체 지점은 SolidShapeFactory 주석 참고 — 아트가 들어오면
    // 여기서 부위별 스프라이트를 넘기도록 인자만 늘리면 된다.
    public sealed class MemoryRoomSpaceView : MonoBehaviour
    {
        [SerializeField] private Transform _spaceRoot;
        [SerializeField] private ScenePointerInput _pointerInput;

        [Header("임시 색(아트 교체 전까지)")]
        [SerializeField] private Color _backWallColor = new Color(0.16f, 0.15f, 0.20f);
        [SerializeField] private Color _structureColor = new Color(0.34f, 0.32f, 0.40f);
        [SerializeField] private Color _playerColor = new Color(0.85f, 0.85f, 0.90f);
        [SerializeField] private Color _posterColor = new Color(0.80f, 0.62f, 0.35f);
        [SerializeField] private Color _floorObjectColor = new Color(0.45f, 0.68f, 0.72f);
        [SerializeField] private Color _exitColor = new Color(0.55f, 0.50f, 0.30f);
        [SerializeField] private Color _outlineColor = Color.white;
        [SerializeField] private float _outlinePadding = 0.14f;

        // 그리는 순서. 뒷벽이 가장 뒤, 플레이어가 가장 앞이다. 강조 테두리는
        // 자기 본체 바로 뒤에 오도록 본체 순서에서 하나를 빼서 쓴다 — 앞에 두면
        // 마우스를 올렸을 때 테두리가 본체를 덮어 색이 통째로 바뀌어 버린다.
        private const int BackWallOrder = 0;
        private const int StructureOrder = 1;
        private const int ExitOrder = 3;
        private const int ClueOrder = 5;
        private const int PlayerOrder = 6;

        private readonly List<GameObject> _contentObjects = new List<GameObject>();

        private MemoryRoomLayout _layout;
        private GameObject _roomObject;
        private Transform _contentRoot;
        private PlayerCharacter _player;

        public event Action<ClueId> ClueActivated;
        public event Action<MemoryGraphNodeId> ExitActivated;
        public event Action<MemoryGraphNodeId, bool> ExitHoverChanged;

        public float PlayerX => _player == null ? 0f : _player.CurrentX;

        // 방 껍데기(벽/바닥/천장/플레이어)를 만든다. 화면이 껐다 켜질 때마다
        // 다시 불리므로, 이전에 만든 것을 먼저 지워 벽이 겹겹이 쌓이지 않게 한다.
        public void Build(MemoryRoomLayout layout)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));

            if (_spaceRoot == null)
                _spaceRoot = transform;

            ClearContents();
            SceneObjectLifetime.Destroy(_roomObject);

            // 방 전체를 자식 오브젝트 하나에 담는다 — 이 컴포넌트가 붙은
            // GameObject를 직접 껐다 켜면 컴포넌트 자신도 함께 꺼져 버린다.
            _roomObject = new GameObject("Room");
            _roomObject.transform.SetParent(_spaceRoot, worldPositionStays: false);
            var roomRoot = _roomObject.transform;

            SolidShapeFactory.Create("BackWall", roomRoot, RoomGeometry.BackWall(layout), _backWallColor, BackWallOrder);
            SolidShapeFactory.Create("Floor", roomRoot, RoomGeometry.Floor(layout), _structureColor, StructureOrder);
            SolidShapeFactory.Create("Ceiling", roomRoot, RoomGeometry.Ceiling(layout), _structureColor, StructureOrder);
            SolidShapeFactory.Create("LeftWall", roomRoot, RoomGeometry.LeftWall(layout), _structureColor, StructureOrder);
            SolidShapeFactory.Create("RightWall", roomRoot, RoomGeometry.RightWall(layout), _structureColor, StructureOrder);

            var contentRootObject = new GameObject("Contents");
            contentRootObject.transform.SetParent(roomRoot, worldPositionStays: false);
            _contentRoot = contentRootObject.transform;

            var playerRenderer = SolidShapeFactory.Create(
                "Player", roomRoot, Vector2.zero, new Vector2(layout.PlayerWidth, layout.PlayerHeight),
                _playerColor, PlayerOrder);
            _player = playerRenderer.gameObject.AddComponent<PlayerCharacter>();
            _player.Configure(layout, startX: 0f);
        }

        // 확대 화면이 떠 있는 동안에는 뒤에 깔린 방을 건드릴 수 없어야 한다.
        public void SetInteractionEnabled(bool enabled)
        {
            if (_pointerInput != null)
                _pointerInput.InputEnabled = enabled;
        }

        // 지금 방을 이 내용으로 그린다. 이 호출이 끝나는 시점에 이전 내용물은
        // 이미 씬에서 사라져 있다(SceneObjectLifetime 주석 참고).
        public void SetContents(IReadOnlyList<ClueSceneItem> clues, IReadOnlyList<RoomExitSceneItem> exits)
        {
            if (clues == null) throw new ArgumentNullException(nameof(clues));
            if (exits == null) throw new ArgumentNullException(nameof(exits));

            ClearContents();

            if (_roomObject != null)
                _roomObject.SetActive(true);

            foreach (var exit in exits)
                CreateExit(exit);

            foreach (var clue in clues)
                CreateClue(clue);
        }

        // 방을 통째로 치운다. 끄기만 하지 않고 내용물을 실제로 비우는 것이
        // 요점이다 — 계단이나 분석실에 서 있는 동안 이전 방의 단서가 살아
        // 있으면, 방이 다시 켜지는 순간 새 방 단서와 겹쳐 되살아난다.
        public void HideRoom()
        {
            ClearContents();

            if (_roomObject != null)
                _roomObject.SetActive(false);
        }

        private void CreateClue(ClueSceneItem item)
        {
            // 종류에 따라 갈리는 것은 이 두 줄이 전부다: 크기와 색(자리는 이미
            // 컨트롤러가 계산해 왔다). 그 아래로는 포스터든 바닥 물건이든 완전히
            // 같은 경로를 지난다 — 갈래가 늘어나는 순간 한쪽에만 콜라이더가
            // 빠지는 사고가 생긴다.
            var size = CluePlacementLayout.SizeOf(_layout, item.Kind);
            var color = item.Kind == ClueKind.Poster ? _posterColor : _floorObjectColor;

            var created = CreateInteractable(
                "Clue_" + item.ClueId, item.Position, new Vector2(size, size), color, ClueOrder);

            var sceneObject = created.Root.AddComponent<ClueSceneObject>();
            sceneObject.Initialize(item.ClueId, created.Outline, ClueOrder);
            sceneObject.Activated += OnClueActivated;
        }

        private void CreateExit(RoomExitSceneItem item)
        {
            var size = RoomExitLayout.SizeOf(_layout, item.Kind);

            // 이름에 목적지를 적어 둔다 — 씬에 글자를 그릴 폰트가 아직 없어서,
            // 하이어라키에서라도 어느 문이 어디로 가는지 보이게 하기 위함이다.
            var created = CreateInteractable("Exit_" + item.Label, item.Position, size, _exitColor, ExitOrder);

            var sceneObject = created.Root.AddComponent<RoomExitSceneObject>();
            sceneObject.Initialize(item.TargetNodeId, created.Outline, ExitOrder);
            sceneObject.Activated += OnExitActivated;
            sceneObject.HoverChanged += OnExitHoverChanged;
        }

        // 강조 테두리와 본체를 자식으로 두고, 마우스 판정과 컴포넌트는 부모에
        // 붙인다. 부모의 크기를 1로 두어야 자식들의 크기가 서로 곱해지지 않는다.
        //
        // 콜라이더 크기를 인자로 따로 받지 않고 방금 만든 본체의 실제 크기에서
        // 그대로 가져오는 것이 이 함수의 핵심이다. 따로 받으면 보이는 사각형과
        // 마우스 판정 범위가 조용히 어긋날 수 있는데, 그 어긋남은 화면만 봐서는
        // 절대 드러나지 않는다("보이는데 눌리지 않는다"로만 나타난다).
        private (GameObject Root, SpriteRenderer Outline) CreateInteractable(
            string name, Vector2 position, Vector2 size, Color color, int bodyOrder)
        {
            var root = new GameObject(name);
            root.transform.SetParent(_contentRoot, worldPositionStays: false);
            root.transform.localPosition = new Vector3(position.x, position.y, 0f);

            var outline = SolidShapeFactory.Create(
                "Outline", root.transform, Vector2.zero,
                size + new Vector2(_outlinePadding, _outlinePadding), _outlineColor, bodyOrder - 1);
            var body = SolidShapeFactory.Create("Body", root.transform, Vector2.zero, size, color, bodyOrder);

            var collider = root.AddComponent<BoxCollider2D>();
            collider.size = body.transform.localScale;

            _contentObjects.Add(root);
            return (root, outline);
        }

        private void OnClueActivated(ClueId clueId) => ClueActivated?.Invoke(clueId);
        private void OnExitActivated(MemoryGraphNodeId nodeId) => ExitActivated?.Invoke(nodeId);

        private void OnExitHoverChanged(MemoryGraphNodeId nodeId, bool hovered) =>
            ExitHoverChanged?.Invoke(nodeId, hovered);

        private void ClearContents()
        {
            // 지워질 오브젝트를 가리키고 있던 강조 상태를 먼저 놓아준다.
            if (_pointerInput != null)
                _pointerInput.ClearHover();

            foreach (var contentObject in _contentObjects)
                SceneObjectLifetime.Destroy(contentObject);

            _contentObjects.Clear();
        }

        // 여기서는 오브젝트를 따로 지우지 않는다 — 내용물과 방은 전부 이
        // 컴포넌트가 붙은 오브젝트의 자식이라 함께 사라지고, 파괴 도중에 다시
        // 파괴를 부르는 것은 Unity가 반기지 않는 호출이다.
        private void OnDestroy()
        {
            _contentObjects.Clear();
            _roomObject = null;
            _contentRoot = null;
            _player = null;
        }
    }
}
