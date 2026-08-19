using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 좌우 이동 한 프레임분의 계산. 씬 없이 검증할 수 있도록 MonoBehaviour에서
    // 떼어 낸다 — 컴포넌트 안에 두면 "벽을 뚫고 나가지 않는가"를 확인하려고
    // 매번 씬을 띄워야 한다.
    //
    // 이 계산은 게임 규칙이 아니라 표시 계층의 이동이다. 방과 방 사이 이동만이
    // 규칙(정신력 비용, 사다리 잠김)의 대상이고, 방 안에서 걸어 다니는 것은
    // 아무 비용도 없고 아무것도 소모하지 않는다 — 그래서 Core를 거치지 않는다.
    public static class PlayerMovement
    {
        // direction은 -1(왼쪽), 0(정지), +1(오른쪽)이다.
        public static float NextX(
            MemoryRoomLayout layout, float currentX, float direction, float deltaTime)
        {
            var moved = currentX + direction * layout.PlayerMoveSpeed * deltaTime;
            return RoomGeometry.ClampInteriorX(layout, moved);
        }

        // 좌우 키가 동시에 눌리면 서로 상쇄시킨다 — 어느 한쪽을 우선하면
        // 키보드를 어떻게 쥐었느냐에 따라 결과가 달라진다.
        public static float DirectionOf(bool leftHeld, bool rightHeld)
        {
            if (leftHeld == rightHeld)
                return 0f;

            return leftHeld ? -1f : 1f;
        }
    }
}
