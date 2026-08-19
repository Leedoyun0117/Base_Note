using System.Collections.Generic;
using GameName.UI.MemoryRoom.Space;
using UnityEngine;

namespace GameName.UI.Diagnostics
{
    // 방을 드나드는 지점(문·사다리) 오브젝트의 상태.
    //
    // 이걸 뒤늦게 추가한 이유가 진단의 사각지대였다. 지금까지 프로브는 단서
    // 오브젝트만 세고 있었다 — 증상이 "단서가 안 눌린다"였으니 단서만 보면
    // 된다고 생각한 것이다. 그런데 같은 방에 그려지는 오브젝트는 단서만이
    // 아니고, 겹쳐 보이거나 클릭을 가로채는 쪽이 출입구일 수도 있다. 보지 않는
    // 것은 로그에 남지 않고, 로그에 없는 것은 없는 것처럼 취급된다.
    internal static class ExitSceneSnapshot
    {
        private const float TouchTolerance = 0.001f;

        public static List<RoomExitSceneObject> Capture(MemoryRoomSpaceView view)
        {
            var exits = new List<RoomExitSceneObject>();
            if (view == null)
                return exits;

            foreach (var exit in view.GetComponentsInChildren<RoomExitSceneObject>(includeInactive: true))
                exits.Add(exit);

            return exits;
        }

        public static string Describe(IReadOnlyList<RoomExitSceneObject> exits)
        {
            if (exits.Count == 0)
                return "없음";

            var parts = new List<string>(exits.Count);
            foreach (var exit in exits)
            {
                var collider = exit.GetComponent<BoxCollider2D>();
                var bounds = collider == null ? "콜라이더 없음" : Format(collider.bounds);
                parts.Add($"{exit.name} 좌표({exit.transform.localPosition.x:0.###},{exit.transform.localPosition.y:0.###}) {bounds}");
            }

            return string.Join(" | ", parts);
        }

        // 판정 영역이 완전히 같은 출입구가 둘 있으면 하나는 영영 누를 수 없다.
        // 눈으로는 하나처럼 보이므로 화면만 봐서는 알아챌 방법이 없다.
        public static string FindOverlaps(IReadOnlyList<RoomExitSceneObject> exits)
        {
            var overlaps = new List<string>();

            for (var i = 0; i < exits.Count; i++)
            {
                for (var j = i + 1; j < exits.Count; j++)
                {
                    var left = exits[i].GetComponent<BoxCollider2D>();
                    var right = exits[j].GetComponent<BoxCollider2D>();
                    if (left == null || right == null)
                        continue;

                    // 나란히 붙은 두 영역은 경계선을 공유한다. 그 상태까지 겹친
                    // 것으로 치면 정상 배치에도 경고가 뜨므로 오차만큼 줄여 본다.
                    var shrunk = left.bounds;
                    shrunk.Expand(-TouchTolerance);
                    if (!shrunk.Intersects(right.bounds))
                        continue;

                    var identical = left.bounds == right.bounds;
                    overlaps.Add(
                        $"{exits[i].name} ↔ {exits[j].name} " +
                        $"({(identical ? "완전히 같은 자리" : "일부 겹침")}) {Format(left.bounds)} / {Format(right.bounds)}");
                }
            }

            return overlaps.Count == 0 ? null : string.Join(" | ", overlaps);
        }

        private static string Format(Bounds bounds) =>
            $"[{bounds.min.x:0.##},{bounds.min.y:0.##}~{bounds.max.x:0.##},{bounds.max.y:0.##}]";
    }
}
