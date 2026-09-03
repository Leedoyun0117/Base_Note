using System;
using GameName.Core.Clues;
using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 방 안에 놓인 단서 한 개의 씬 오브젝트.
    //
    // 이 오브젝트가 아는 것은 자기 식별자(ClueId) 하나뿐이다. 겉보기 구성도,
    // 실제 구성도, 어느 방 소속인지도 알지 못하며 Core 처리기를 참조하지도
    // 않는다 — 눌렸다는 사실만 이벤트로 알리고, 그 뒤에 무슨 일이 일어나는지는
    // 컨트롤러가 정한다. 씬 오브젝트가 ClueDefinition에 닿을 길이 아예 없다는
    // 것은 리플렉션 테스트로 따로 증명한다.
    //
    // 테두리는 자식 오브젝트로 미리 만들어 두고 껐다 켜기만 한다 — 마우스가
    // 올라올 때마다 오브젝트를 만들고 지우면 프레임마다 할당이 생긴다.
    //
    // 가시 비율 밖으로 밀려난 단서는 SetAccessible(false)로 회색 처리 +
    // 콜라이더 비활성이 함께 일어난다. 둘을 한 메서드에서 처리하는 이유는
    // "보이는 상태와 집히는 상태가 갈라지지 않게" 하기 위함이다 — 그 어긋남은
    // 화면만 봐서는 드러나지 않는다.
    public sealed class ClueSceneObject : MonoBehaviour, IPointerInteractable
    {
        private static readonly Color InaccessibleTint = new Color(0.32f, 0.32f, 0.34f, 0.85f);

        private ClueId _clueId;
        private SpriteRenderer _outline;
        private SpriteRenderer _body;
        private BoxCollider2D _collider;
        private Color _baseColor;
        private int _drawOrder;
        private bool _accessible = true;

        public event Action<ClueId> Activated;

        // 지금 테두리가 보이는지. 마우스 오버 표시를 씬 없이 검증하기 위해
        // 노출한다.
        public bool IsOutlineVisible => _outline != null && _outline.enabled;

        // 지금 이 단서를 집을 수 있는지(가시 비율 안). 씬 없이 검증하기 위해 노출한다.
        public bool IsAccessible => _accessible;

        public int DrawOrder => _drawOrder;

        public void Initialize(
            ClueId clueId, SpriteRenderer outline, SpriteRenderer body, BoxCollider2D collider, int drawOrder)
        {
            _clueId = clueId;
            _outline = outline ?? throw new ArgumentNullException(nameof(outline));
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _collider = collider ?? throw new ArgumentNullException(nameof(collider));
            _baseColor = body.color;
            _outline.enabled = false;
            _drawOrder = drawOrder;
        }

        public void SetHovered(bool hovered)
        {
            // 집을 수 없는 단서에는 강조 테두리를 띄우지 않는다.
            if (_outline != null)
                _outline.enabled = hovered && _accessible;
        }

        public void SetAccessible(bool accessible)
        {
            _accessible = accessible;

            if (_collider != null)
                _collider.enabled = accessible;

            if (_body != null)
                _body.color = accessible ? _baseColor : InaccessibleTint;

            if (!accessible && _outline != null)
                _outline.enabled = false;
        }

        public void Activate()
        {
            if (_accessible)
                Activated?.Invoke(_clueId);
        }
    }
}
