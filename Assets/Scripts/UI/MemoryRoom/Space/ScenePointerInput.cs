using System.Collections.Generic;
using GameName.UI.Overlays;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom.Space
{
    // 마우스가 씬의 어떤 오브젝트 위에 있는지 찾아 강조/클릭을 전달한다.
    //
    // Unity의 OnMouseEnter/OnMouseDown 콜백을 쓰지 않는다 — 그 콜백들은 예전
    // 입력 백엔드에 묶여 있어서, 이 프로젝트처럼 Active Input Handling이
    // Input System 단독인 설정에서는 아예 호출되지 않는다. 그래서 직접
    // 화면 좌표를 월드로 바꿔 2D 콜라이더를 찍는다.
    //
    // 강조 상태를 이 한 곳에서만 관리하는 이유: 오브젝트마다 스스로 "내가
    // 지금 마우스 아래인가"를 판단하면 방을 다시 그리는 순간 강조가 남아 있는
    // 오브젝트가 생긴다. 여기서는 항상 직전에 강조했던 것 하나만 기억했다가
    // 꺼 주므로 그런 찌꺼기가 생기지 않는다.
    public sealed class ScenePointerInput : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        // 포인터를 가로챌 수 있는 UI 문서들. 이 목록이 비어 있으면 UI는 클릭을
        // 전혀 막지 않는다 — 연결을 빠뜨리면 "UI를 눌렀는데 뒤의 단서까지
        // 같이 눌리는" 증상으로 드러난다.
        [SerializeField] private UIDocument[] _uiDocuments;

        private IPointerInteractable _hovered;

        // 한 지점에 겹친 콜라이더를 전부 받아 두는 자리. 프레임마다 새 목록을
        // 만들지 않도록 재사용한다.
        private readonly List<Collider2D> _hits = new List<Collider2D>();
        private ContactFilter2D _pickFilter;

        // 씬 조작을 막는 두 겹 중 거친 쪽. 전체 화면 오버레이(기록지/인벤토리/
        // 확대 화면)가 떠 있는 동안 컨트롤러가 꺼 준다 — "지금은 다른 화면을
        // 보고 있다"는 명시적 상태다.
        //
        // 고운 쪽은 아래 UIPointerOcclusion이다: 오버레이가 없더라도 포인터
        // 바로 아래에 조작 가능한 UI가 있으면 그 프레임만 씬을 건드리지 않는다.
        // 두 겹으로 나눈 이유는, HUD가 화면 가장자리에 얇게 깔려 있는 동안에도
        // 방의 나머지 전부는 계속 조작할 수 있어야 하기 때문이다.
        public bool InputEnabled { get; set; } = true;

        private void Awake()
        {
            if (_camera == null)
                _camera = Camera.main;

            // 기본 판정 조건 그대로 쓰되(레이어 제한 없음, 트리거 포함) 콜라이더를
            // 하나가 아니라 전부 받는다 — 이유는 Pick 주석에 적었다.
            //
            // NoFilter()를 쓰지 않는 이유는 Unity 6에서 폐기되었기 때문이다. 기본
            // 생성자가 이미 모든 걸러내기를 끈 상태이므로, 트리거를 포함시키는
            // 것 하나만 켜면 같은 뜻이 된다.
            _pickFilter = new ContactFilter2D { useTriggers = true };
        }

        private void OnDisable()
        {
            SetHovered(null);
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || _camera == null)
                return;

            if (!InputEnabled)
            {
                SetHovered(null);
                return;
            }

            var screenPosition = mouse.position.ReadValue();

            // UI 위에서는 씬을 건드리지 않는다 — 버튼을 눌렀는데 뒤의 단서까지
            // 함께 집히는 일을 막는다.
            if (UIPointerOcclusion.IsPointerOverPickableElement(_uiDocuments, screenPosition))
            {
                SetHovered(null);
                return;
            }

            var worldPoint = (Vector2)_camera.ScreenToWorldPoint(screenPosition);
            var target = Pick(worldPoint);

            SetHovered(target);

            if (target != null && mouse.leftButton.wasPressedThisFrame)
                target.Activate();
        }

        // 그 지점에 겹친 것 중 무엇을 가리킨 것으로 볼지 고른다.
        //
        // 예전에는 Physics2D.OverlapPoint로 "하나만" 받아 왔다. 그런데 방 안에는
        // 겹치는 판정 영역이 실제로 존재한다 — 사다리는 방 높이 전체를 덮는 세로
        // 막대이고, 바닥 물건은 바닥에 붙어 있어 앞으로 바닥이나 벽에 콜라이더가
        // 생기는 순간 그 안에 들어간다. 하나만 받으면 어느 것이 올지 보장이 없어
        // "보이는데 눌리지 않는" 증상이 된다. 그래서 전부 받아 여기서 고른다.
        //
        // 고르는 기준은 "화면에서 앞에 보이는 것"이다. 사람이 클릭할 때 기대하는
        // 것이 그것이고, 앞뒤의 유일한 진실은 그리는 순서(DrawOrder)다.
        //
        // 한동안은 판정 영역이 가장 작은 것을 골랐다. 작은 것이 큰 것 위에
        // 얹혀 있다는 짐작이었는데, 크기가 완전히 같은 둘이 겹치면 뒤엣것이
        // 무조건 탈락해 영영 누를 수 없었다(방 2의 사다리 두 개가 정확히 그
        // 경우였다). 짐작으로 만든 규칙은 짐작이 깨지는 자리에서 조용히 틀린다.
        // 크기는 이제 앞뒤가 같을 때만 보는 보조 기준으로 남긴다 — 같은 순서로
        // 그려지는 것들 사이에서는 작은 쪽이 더 구체적인 대상일 확률이 높다.
        private IPointerInteractable Pick(Vector2 worldPoint)
        {
            _hits.Clear();
            Physics2D.OverlapPoint(worldPoint, _pickFilter, _hits);

            IPointerInteractable picked = null;
            var pickedOrder = int.MinValue;
            var pickedArea = float.MaxValue;

            foreach (var hit in _hits)
            {
                var candidate = hit.GetComponent<IPointerInteractable>();
                if (candidate == null)
                    continue;

                var size = hit.bounds.size;
                var area = size.x * size.y;

                if (candidate.DrawOrder < pickedOrder)
                    continue;

                if (candidate.DrawOrder == pickedOrder && area >= pickedArea)
                    continue;

                picked = candidate;
                pickedOrder = candidate.DrawOrder;
                pickedArea = area;
            }

            return picked;
        }

        // 방을 다시 그리면 강조 중이던 오브젝트가 파괴될 수 있다. 인터페이스
        // 참조는 Unity의 가짜 null 규칙을 타지 않아 그냥 두면 파괴된 컴포넌트를
        // 호출하게 되므로, 쓰기 전에 살아 있는지 확인한다.
        public void ClearHover() => SetHovered(null);

        private void SetHovered(IPointerInteractable target)
        {
            if (!IsAlive(_hovered))
                _hovered = null;

            if (ReferenceEquals(_hovered, target))
                return;

            _hovered?.SetHovered(false);
            _hovered = target;
            _hovered?.SetHovered(true);
        }

        private static bool IsAlive(IPointerInteractable target)
        {
            if (target == null)
                return false;

            return !(target is MonoBehaviour behaviour) || behaviour != null;
        }
    }
}
