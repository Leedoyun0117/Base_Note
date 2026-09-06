using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 아트가 아직 없어서 벽/바닥/플레이어/단서를 전부 단색 사각형으로 그린다.
    // 그 사각형을 만드는 방법을 한 곳에 모아 둔 자리다.
    //
    // ── 스프라이트 교체 지점 ────────────────────────────────────────────
    // 실제 아트가 들어오면 바꿀 곳은 두 군데뿐이다.
    //   1) 아래 UnitSprite — 임시 1x1 흰색 스프라이트를 실제 스프라이트로.
    //   2) Create(...)를 부르는 쪽(MemoryRoomSpaceView)에서 부위별로 서로 다른
    //      스프라이트를 넘기도록 인자를 하나 늘리는 것.
    // 크기를 localScale로 주고 있으므로 스프라이트만 갈아 끼워도 배치는 그대로
    // 유지된다(스프라이트 크기는 1 유닛 기준으로 맞춰 임포트할 것).
    //
    // 3D 프리미티브(Quad 등) 대신 SpriteRenderer를 쓰는 이유: 이 프로젝트는
    // URP 2D 렌더러라 3D 기본 머티리얼이 그대로 보이지 않는다.
    internal static class SolidShapeFactory
    {
        private static Sprite _unitSprite;
        private static Material _unlitMaterial;

        // 1 월드 유닛짜리 흰 사각형. 색은 SpriteRenderer.color로, 크기는
        // localScale로 준다 — 그래서 스프라이트 하나를 모든 도형이 공유한다.
        private static Sprite UnitSprite
        {
            get
            {
                if (_unitSprite == null)
                {
                    var texture = new Texture2D(1, 1);
                    texture.SetPixel(0, 0, Color.white);
                    texture.Apply();

                    _unitSprite = Sprite.Create(
                        texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), pixelsPerUnit: 1f);
                    _unitSprite.name = "UnitSolidSprite";
                }

                return _unitSprite;
            }
        }

        // 조명을 받지 않는 도형이 쓰는 머티리얼. 게임플레이 어포던스(단서 강조
        // 테두리·본체, 플레이어 위치 표식)는 램프에서 멀어 어두워지면 "집을 수
        // 있음" 신호나 아바타 위치를 잃어버리므로, 이들만 Unlit로 그려 조명과
        // 무관하게 항상 authored 색으로 보이게 한다. 방 껍데기(벽/바닥 등 임시
        // 아트)는 기본값(Lit) 그대로 둔다.
        private static Material UnlitMaterial
        {
            get
            {
                if (_unlitMaterial == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                    if (shader == null)
                        return null; // URP 2D 셰이더를 못 찾으면 기본(Lit)으로 둔다 — 크래시보다 낫다.

                    _unlitMaterial = new Material(shader)
                    {
                        name = "SolidShapeUnlit",
                        hideFlags = HideFlags.HideAndDontSave,
                    };
                }

                return _unlitMaterial;
            }
        }

        public static SpriteRenderer Create(
            string name, Transform parent, Vector2 center, Vector2 size, Color color, int sortingOrder, bool lit = true)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, worldPositionStays: false);
            gameObject.transform.localPosition = new Vector3(center.x, center.y, 0f);
            gameObject.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = UnitSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            if (!lit)
            {
                var unlit = UnlitMaterial;
                if (unlit != null)
                    renderer.sharedMaterial = unlit;
            }

            return renderer;
        }

        public static SpriteRenderer Create(
            string name, Transform parent, RoomShape shape, Color color, int sortingOrder, bool lit = true) =>
            Create(name, parent, shape.Center, shape.Size, color, sortingOrder, lit);
    }
}
