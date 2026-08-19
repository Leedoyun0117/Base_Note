using System;
using GameName.Core.MemoryRooms;
using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 방을 드나드는 지점(문/사다리) 한 개의 씬 오브젝트.
    //
    // 단서 오브젝트와 같은 원칙이다: 목적지 노드 식별자만 들고 있고, 그곳으로
    // 갈 수 있는지, 정신력이 얼마나 드는지, 사다리가 잠겼는지는 전혀 모른다.
    // 눌리면 컨트롤러에 알리고, 판정은 전부 Core 이동 처리기가 한다 — 갈 수
    // 없는 곳도 누를 수 있게 열어 두고 실패 사유를 그대로 보여주는 방식은
    // 기존 지도 UI가 이미 쓰던 것과 같다.
    public sealed class RoomExitSceneObject : MonoBehaviour, IPointerInteractable
    {
        private MemoryGraphNodeId _targetNodeId;
        private SpriteRenderer _outline;
        private int _drawOrder;

        public event Action<MemoryGraphNodeId> Activated;

        // 마우스가 올라왔을 때 목적지와 이동 비용을 안내하기 위한 알림.
        // 씬에 글자를 그리려면 폰트 에셋이 필요한데 아직 없으므로, 안내는
        // 기존 HUD의 문구 자리를 그대로 재사용한다.
        public event Action<MemoryGraphNodeId, bool> HoverChanged;

        public bool IsOutlineVisible => _outline != null && _outline.enabled;

        public int DrawOrder => _drawOrder;

        public void Initialize(MemoryGraphNodeId targetNodeId, SpriteRenderer outline, int drawOrder)
        {
            _targetNodeId = targetNodeId;
            _outline = outline ?? throw new ArgumentNullException(nameof(outline));
            _outline.enabled = false;
            _drawOrder = drawOrder;
        }

        public void SetHovered(bool hovered)
        {
            if (_outline != null)
                _outline.enabled = hovered;

            HoverChanged?.Invoke(_targetNodeId, hovered);
        }

        public void Activate() => Activated?.Invoke(_targetNodeId);
    }
}
