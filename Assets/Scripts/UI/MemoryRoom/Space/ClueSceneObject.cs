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
    public sealed class ClueSceneObject : MonoBehaviour, IPointerInteractable
    {
        private ClueId _clueId;
        private SpriteRenderer _outline;
        private int _drawOrder;

        public event Action<ClueId> Activated;

        // 지금 테두리가 보이는지. 마우스 오버 표시를 씬 없이 검증하기 위해
        // 노출한다.
        public bool IsOutlineVisible => _outline != null && _outline.enabled;

        public int DrawOrder => _drawOrder;

        public void Initialize(ClueId clueId, SpriteRenderer outline, int drawOrder)
        {
            _clueId = clueId;
            _outline = outline ?? throw new ArgumentNullException(nameof(outline));
            _outline.enabled = false;
            _drawOrder = drawOrder;
        }

        public void SetHovered(bool hovered)
        {
            if (_outline != null)
                _outline.enabled = hovered;
        }

        public void Activate() => Activated?.Invoke(_clueId);
    }
}
