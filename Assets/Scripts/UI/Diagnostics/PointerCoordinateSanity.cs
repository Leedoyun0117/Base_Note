using UnityEngine;

namespace GameName.UI.Diagnostics
{
    // 마우스 좌표가 애초에 말이 되는 값인지 본다.
    //
    // 이 검사를 따로 둔 이유: 판정이 실패했을 때 우리는 늘 "콜라이더가 없나,
    // UI가 가렸나, 고르는 방식이 틀렸나"를 먼저 의심했다. 그런데 실제 로그에서
    // 나온 값은 화면 밖 좌표였다 — 그 지점에 아무것도 없는 것이 당연하고, 뒤에
    // 이어지는 모든 판정은 처음부터 의미가 없었다. 판정을 따지기 전에 "그 좌표가
    // 화면 안이기는 한가"를 먼저 확인해야 같은 착각을 반복하지 않는다.
    //
    // 좌표가 밖으로 나가는 흔한 경로가 몇 가지 있고, 아래 값들을 함께 찍으면
    // 어느 쪽인지 바로 갈린다:
    //   · 게임 뷰가 아닌 창(Scene 뷰 등)을 클릭했다 → 좌표가 음수이거나 화면
    //     크기를 넘고, 창 포커스도 대개 함께 어긋난다.
    //   · 게임 뷰 배율이나 고정 해상도가 창과 다르다 → Screen 크기와 카메라의
    //     픽셀 크기가 어긋난다.
    //   · 카메라가 화면 일부만 그리도록 잘려 있다 → 카메라 rect가 (0,0,1,1)이
    //     아니다.
    internal static class PointerCoordinateSanity
    {
        public static bool IsOutsideViewport(Vector2 screenPosition, Camera camera)
        {
            var pixelRect = camera == null ? new Rect(0, 0, Screen.width, Screen.height) : camera.pixelRect;

            return screenPosition.x < pixelRect.xMin || screenPosition.x > pixelRect.xMax ||
                   screenPosition.y < pixelRect.yMin || screenPosition.y > pixelRect.yMax;
        }

        public static string Describe(Vector2 screenPosition, Camera camera)
        {
            var inside = !IsOutsideViewport(screenPosition, camera);
            var pixelRect = camera == null ? default : camera.pixelRect;
            var viewportRect = camera == null ? default : camera.rect;

            return
                $"좌표검사={(inside ? "화면 안" : "화면 밖")}" +
                $", Screen={Screen.width}x{Screen.height}" +
                $", 카메라픽셀=({pixelRect.xMin:0}~{pixelRect.xMax:0}, {pixelRect.yMin:0}~{pixelRect.yMax:0})" +
                $", 카메라rect=({viewportRect.x:0.##},{viewportRect.y:0.##},{viewportRect.width:0.##},{viewportRect.height:0.##})" +
                $", 창포커스={Application.isFocused}" +
                $", 카메라={(camera == null ? "없음" : camera.name)}";
        }
    }
}
