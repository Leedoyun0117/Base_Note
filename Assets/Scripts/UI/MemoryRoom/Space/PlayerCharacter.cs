using UnityEngine;
using UnityEngine.InputSystem;

namespace GameName.UI.MemoryRoom.Space
{
    // 방 안을 좌우로 걸어 다니는 플레이어.
    //
    // 이동 자체는 게임 규칙이 아니라 표시다 — 방과 방 사이 이동만 규칙(비용,
    // 사다리 잠김)의 대상이고 방 안에서 걷는 것은 아무것도 소모하지 않는다.
    // 그래서 이 컴포넌트는 Core를 전혀 참조하지 않는다.
    //
    // 실제 좌표 계산은 PlayerMovement(순수 함수)에 맡긴다 — 벽을 뚫지 않는지
    // 같은 것을 씬 없이 검증하기 위해서다. 여기서는 키보드를 읽고 결과를
    // Transform에 반영하는 일만 한다.
    //
    // 스프라이트 교체 지점: 지금은 SolidShapeFactory가 만든 단색 네모를 붙여
    // 두고 있다. 실제 캐릭터가 들어오면 이 오브젝트의 SpriteRenderer에 스프라이트
    // (또는 Animator)를 얹으면 되고, 이 컴포넌트는 바뀌지 않는다.
    public sealed class PlayerCharacter : MonoBehaviour
    {
        private MemoryRoomLayout _layout;

        public float CurrentX => transform.localPosition.x;

        public void Configure(MemoryRoomLayout layout, float startX)
        {
            _layout = layout;
            MoveTo(startX);
        }

        public void MoveTo(float x)
        {
            if (_layout == null)
                return;

            var position = RoomGeometry.PlayerPosition(_layout, x);
            transform.localPosition = new Vector3(position.x, position.y, 0f);
        }

        private void Update()
        {
            if (_layout == null)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            var leftHeld = keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed;
            var rightHeld = keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed;

            var direction = PlayerMovement.DirectionOf(leftHeld, rightHeld);
            if (Mathf.Approximately(direction, 0f))
                return;

            MoveTo(PlayerMovement.NextX(_layout, CurrentX, direction, Time.deltaTime));
        }
    }
}
