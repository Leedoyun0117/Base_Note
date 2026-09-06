using System;
using System.Collections.Generic;
using GameName.Core.Clues;
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

        [Header("아트 교체")]
        [Tooltip("켜면 절차적 방 도형(뒷벽·바닥·천장·좌우벽)의 렌더러를 끈다. " +
            "픽셀아트 레이어를 Space 아래 깔았을 때 그 뒤에 남는 임시 배경을 숨기는 용도다. " +
            "도형에는 콜라이더가 없고 이동·배치는 전부 RoomGeometry(순수 계산)가 정하므로 " +
            "렌더러를 꺼도 게임플레이에는 영향이 없다. 플레이어 박스는 끄지 않는다.")]
        [SerializeField] private bool _hideStructureRenderers;

        [Header("임시 색(아트 교체 전까지)")]
        [SerializeField] private Color _backWallColor = new Color(0.16f, 0.15f, 0.20f);
        [SerializeField] private Color _structureColor = new Color(0.34f, 0.32f, 0.40f);
        [SerializeField] private Color _playerColor = new Color(0.85f, 0.85f, 0.90f);
        // 단서 임시 도형은 배경 팔레트(어두운 청록)에 맞춘다 — 아트 나오기 전까지.
        // 몸통은 배경에 녹아드는 어두운 청록, 테두리만 밝게 띄워 "집을 수 있는 것"임을 알린다.
        [SerializeField] private Color _posterColor = new Color(0.20f, 0.34f, 0.32f);
        [SerializeField] private Color _floorObjectColor = new Color(0.13f, 0.25f, 0.24f);
        [SerializeField] private Color _outlineColor = new Color(0.72f, 0.98f, 0.92f);
        [SerializeField] private float _outlinePadding = 0.07f;

        // 그리는 순서. 뒷벽이 가장 뒤, 플레이어가 가장 앞이다. 강조 테두리는
        // 자기 본체 바로 뒤에 오도록 본체 순서에서 하나를 빼서 쓴다 — 앞에 두면
        // 마우스를 올렸을 때 테두리가 본체를 덮어 색이 통째로 바뀌어 버린다.
        private const int BackWallOrder = 0;
        private const int StructureOrder = 1;
        private const int ClueOrder = 5;
        private const int PlayerOrder = 6;

        private readonly List<GameObject> _contentObjects = new List<GameObject>();

        private MemoryRoomLayout _layout;
        private GameObject _roomObject;
        private Transform _contentRoot;
        private PlayerCharacter _player;

        public event Action<ClueId> ClueActivated;

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

            // 도형은 늘 만든다(테스트가 존재를 확인하고, 좌표 기준은 그대로다).
            // _hideStructureRenderers면 보이기만 끈다 — 픽셀아트가 뒤를 덮은 상태.
            var structure = new[]
            {
                SolidShapeFactory.Create("BackWall", roomRoot, RoomGeometry.BackWall(layout), _backWallColor, BackWallOrder),
                SolidShapeFactory.Create("Floor", roomRoot, RoomGeometry.Floor(layout), _structureColor, StructureOrder),
                SolidShapeFactory.Create("Ceiling", roomRoot, RoomGeometry.Ceiling(layout), _structureColor, StructureOrder),
                SolidShapeFactory.Create("LeftWall", roomRoot, RoomGeometry.LeftWall(layout), _structureColor, StructureOrder),
                SolidShapeFactory.Create("RightWall", roomRoot, RoomGeometry.RightWall(layout), _structureColor, StructureOrder),
            };

            if (_hideStructureRenderers)
            {
                foreach (var renderer in structure)
                    renderer.enabled = false;
            }

            var contentRootObject = new GameObject("Contents");
            contentRootObject.transform.SetParent(roomRoot, worldPositionStays: false);
            _contentRoot = contentRootObject.transform;

            // 플레이어 표식은 조작 가능한 아바타라 어두운 구석에서도 위치를
            // 놓치면 안 된다 — Unlit로 그려 조명과 무관하게 보이게 한다.
            var playerRenderer = SolidShapeFactory.Create(
                "Player", roomRoot, Vector2.zero, new Vector2(layout.PlayerWidth, layout.PlayerHeight),
                _playerColor, PlayerOrder, lit: false);
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
        public void SetContents(IReadOnlyList<ClueSceneItem> clues)
        {
            if (clues == null) throw new ArgumentNullException(nameof(clues));

            ClearContents();

            if (_roomObject != null)
                _roomObject.SetActive(true);

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
            sceneObject.Initialize(item.ClueId, created.Outline, created.Body, created.Collider, ClueOrder);
            // 가시 비율 밖 단서는 회색 + 콜라이더 비활성. 판정은 컨트롤러가 이미 했다.
            sceneObject.SetAccessible(item.Accessible);
            sceneObject.Activated += OnClueActivated;
        }

        // 강조 테두리와 본체를 자식으로 두고, 마우스 판정과 컴포넌트는 부모에
        // 붙인다. 부모의 크기를 1로 두어야 자식들의 크기가 서로 곱해지지 않는다.
        //
        // 콜라이더 크기를 인자로 따로 받지 않고 방금 만든 본체의 실제 크기에서
        // 그대로 가져오는 것이 이 함수의 핵심이다. 따로 받으면 보이는 사각형과
        // 마우스 판정 범위가 조용히 어긋날 수 있는데, 그 어긋남은 화면만 봐서는
        // 절대 드러나지 않는다("보이는데 눌리지 않는다"로만 나타난다).
        private (GameObject Root, SpriteRenderer Outline, SpriteRenderer Body, BoxCollider2D Collider) CreateInteractable(
            string name, Vector2 position, Vector2 size, Color color, int bodyOrder)
        {
            var root = new GameObject(name);
            root.transform.SetParent(_contentRoot, worldPositionStays: false);
            root.transform.localPosition = new Vector3(position.x, position.y, 0f);

            // 단서 도형은 "집을 수 있는 것"이라는 어포던스다 — 조명을 받으면
            // 어두운 구석에서 강조 테두리가 묻혀 신호가 죽는다. Unlit로 그린다.
            var outline = SolidShapeFactory.Create(
                "Outline", root.transform, Vector2.zero,
                size + new Vector2(_outlinePadding, _outlinePadding), _outlineColor, bodyOrder - 1, lit: false);
            var body = SolidShapeFactory.Create("Body", root.transform, Vector2.zero, size, color, bodyOrder, lit: false);

            var collider = root.AddComponent<BoxCollider2D>();
            collider.size = body.transform.localScale;

            _contentObjects.Add(root);
            return (root, outline, body, collider);
        }

        private void OnClueActivated(ClueId clueId) => ClueActivated?.Invoke(clueId);

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
