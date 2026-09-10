using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 시계면 불빛에 이끌린 나방·벌레가 빛 주위를 불규칙하게 맴돈다.
    //
    // ── SmokeDrift와 같은 원칙 ─────────────────────────────────────────
    // 매 프레임 새 난수를 뽑으면 지직거린다. 그래서 위치는 시간 항 하나로만
    // 결정되는 순수 함수다(같은 시각 → 같은 자리). 곡선은 Perlin 두 겹을 겹쳐
    // 큰 배회 + 작은 떨림을 만들고, 결과는 아트 픽셀 격자에 스냅해 픽셀아트
    // 계단이 프레임마다 흔들리지 않게 한다.
    //
    // ── 마리마다 위상이 다르다 ────────────────────────────────────────
    // 벌레는 씬에 구워진 통짜 그림(Fly.png)이라 낱개로 못 뜯는다. 대신 여기서
    // 작은 점 스프라이트를 _count마리 만들어 각자 다른 seed로 돌린다 — 궤도
    // 반경·속도·중심 오프셋이 조금씩 달라 무리져 다니되 한 몸으로 안 움직인다.
    //
    // 화면 전환으로 껐다 켜지면 Update가 멈췄다 이어진다 — 방을 벗어난 동안
    // 벌레도 멈춘 셈이라 자연스럽다.
    [DisallowMultipleComponent]
    public sealed class FlyWander : MonoBehaviour
    {
        [SerializeField] private bool _enabled = true;

        [Tooltip("맴도는 벌레 수. 각자 위상이 다르다.")]
        [Min(0)]
        [SerializeField] private int _count = 6;

        [Tooltip("무리의 중심(이 오브젝트 로컬 좌표, 유닛). 시계면 허브 근처에 두면 " +
            "'빛 주위를 맴도는' 것으로 읽힌다.")]
        [SerializeField] private Vector2 _center = Vector2.zero;

        [Tooltip("배회 반경(아트 px). 벌레가 중심에서 이만큼까지 벗어난다.")]
        [Min(1f)]
        [SerializeField] private float _radiusPx = 26f;

        [Tooltip("배회 속도 배율. 클수록 빠르게 돌아다닌다.")]
        [Min(0f)]
        [SerializeField] private float _speed = 0.35f;

        [Tooltip("작은 떨림(날갯짓)의 진폭(아트 px).")]
        [Min(0f)]
        [SerializeField] private float _jitterPx = 3f;

        [Tooltip("위치를 스냅할 격자(아트 px). 1이면 픽셀 단위. 0이면 스냅 안 함.")]
        [Min(0f)]
        [SerializeField] private float _pixelSnapPx = 1f;

        [Tooltip("점 하나의 한 변 크기(아트 px).")]
        [Min(1)]
        [SerializeField] private int _dotSizePx = 1;

        [Tooltip("벌레 색. 빛을 받은 창백한 회백색이 밤 장면에 묻히지 않는다.")]
        [SerializeField] private Color _color = new Color(0.75f, 0.78f, 0.85f, 0.9f);

        [Tooltip("점 스프라이트의 sortingOrder. 최전면(난간·인물보다 앞).")]
        [SerializeField] private int _sortingOrder = -30;

        [Tooltip("점에 씌울 머티리얼(Sprite-Unlit-Default). 어두운 방에서도 안 어두워지게.")]
        [SerializeField] private Material _unlitMaterial;

        private const float ArtPixelsPerUnit = 100f;

        private Transform[] _flies;
        private Sprite _dotSprite;
        private Texture2D _dotTex;
        private bool _built;

        private void Awake() => Build();

        private void OnEnable()
        {
            // Build은 한 번뿐. 재활성 시 점들을 다시 켠다(Build이 만들 때 자식으로 붙였다).
            if (_built && _flies != null)
                foreach (var f in _flies)
                    if (f != null) f.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            if (_built && _flies != null)
                foreach (var f in _flies)
                    if (f != null) f.gameObject.SetActive(false);
        }

        private void Build()
        {
            if (_built)
                return;

            _dotTex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "FlyDot",
            };
            _dotTex.SetPixel(0, 0, Color.white);
            _dotTex.Apply(false);

            // PPU를 1/_dotSizePx로 잡으면 1px 텍스처가 _dotSizePx 아트픽셀 크기로 그려진다.
            var ppu = ArtPixelsPerUnit / Mathf.Max(_dotSizePx, 1);
            _dotSprite = Sprite.Create(_dotTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), ppu);
            _dotSprite.name = "FlyDot";

            _flies = new Transform[Mathf.Max(_count, 0)];
            for (var i = 0; i < _flies.Length; i++)
            {
                var go = new GameObject($"Fly_{i}");
                go.transform.SetParent(transform, worldPositionStays: false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _dotSprite;
                sr.color = _color;
                sr.sortingOrder = _sortingOrder;
                if (_unlitMaterial != null)
                    sr.sharedMaterial = _unlitMaterial;
                _flies[i] = go.transform;
            }

            _built = true;
        }

        private void OnDestroy()
        {
            if (_dotTex != null) Destroy(_dotTex);
            if (_dotSprite != null) Destroy(_dotSprite);
        }

        private void Update()
        {
            if (!_built || !_enabled || _flies == null)
                return;

            var t = Time.time;
            for (var i = 0; i < _flies.Length; i++)
            {
                if (_flies[i] == null)
                    continue;

                var offset = LocalOffset(t, i, _radiusPx, _speed, _jitterPx, _pixelSnapPx);
                _flies[i].localPosition = new Vector3(_center.x + offset.x, _center.y + offset.y, 0f);
            }
        }

        // 흐른 시간 → 무리 중심 기준 오프셋(유닛), 픽셀 격자 스냅 적용.
        // 씬 없이 검증하는 쪽이 이걸 직접 부른다. 프레임마다 랜덤이 아니라
        // 시간 항 하나로만 결정된다(같은 인자 → 같은 값).
        public static Vector2 LocalOffset(
            float time, int flyIndex, float radiusPx, float speed, float jitterPx, float pixelSnapPx)
        {
            // 마리마다 다른 seed — 궤도가 겹치지 않게.
            var sx = flyIndex * 37.13f;
            var sy = flyIndex * 91.77f + 12.3f;
            var ts = time * Mathf.Max(speed, 0f);

            // 큰 배회 — Perlin은 0.5 근처에 몰려 있어 대부분 중심 가까이 있다가 가끔 크게 벗어난다.
            var wanderX = (Mathf.PerlinNoise(ts * 0.5f + sx, sy) - 0.5f) * 2f;
            var wanderY = (Mathf.PerlinNoise(sx, ts * 0.5f + sy) - 0.5f) * 2f;

            // 느린 원형 드리프트 — 빛 주위를 '도는' 성분(마리마다 다른 속도·방향).
            var orbitRate = 0.6f + (flyIndex % 3) * 0.25f;
            var orbitDir = (flyIndex % 2 == 0) ? 1f : -1f;
            var ang = ts * orbitRate * orbitDir + flyIndex * 1.7f;
            var orbitX = Mathf.Cos(ang) * 0.45f;
            var orbitY = Mathf.Sin(ang) * 0.30f; // 세로로 납작한 궤도

            var px = (wanderX * 0.7f + orbitX) * radiusPx;
            var py = (wanderY * 0.7f + orbitY) * radiusPx;

            // 빠른 떨림(날갯짓).
            px += (Mathf.PerlinNoise(ts * 6f + sx, 5.5f) - 0.5f) * 2f * jitterPx;
            py += (Mathf.PerlinNoise(7.1f, ts * 6f + sy) - 0.5f) * 2f * jitterPx;

            // 아트 px → 유닛, 그다음 픽셀 격자에 스냅.
            var x = Snap(px, pixelSnapPx) / ArtPixelsPerUnit;
            var y = Snap(py, pixelSnapPx) / ArtPixelsPerUnit;
            return new Vector2(x, y);
        }

        private static float Snap(float valuePx, float snapPx)
        {
            if (snapPx <= 0f)
                return valuePx;
            return Mathf.Round(valuePx / snapPx) * snapPx;
        }
    }
}
