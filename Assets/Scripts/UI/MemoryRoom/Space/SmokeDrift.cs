using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 머그잔에서 피어오르는 김.
    //
    // ── 방식: 조각마다 제각각인 이미터 ──────────────────────────────────
    // 고정 창 안에서 픽셀을 순환(wrap)시키는 방식은 원리상 어딘가를 반드시 자른다
    // (위로 나간 부분이 아래로 되돌아오며 중간이 끊긴다). 그래서 순환을 버리고,
    // 원본의 아래쪽 조각(_emitSourceRows)을 _emitCyclePx마다 하나씩 내보내
    // 위로 올리며 지우는 이미터로 만들었다.
    //
    // 다만 "똑같은 조각을 일정 간격으로 찍어내는" 것만으로는 규칙적인 무늬로 보인다.
    // 그래서 각 조각에 **발행 번호(emitIndex) 기반의 고유 seed**를 주고, 그 seed로
    // 조각마다 다르게 만든다:
    //   · 좌우 경로 — 조각마다 다른 노이즈 경로를 따라 흔들린다. 올라갈수록(ageFrac)
    //     크게 벗어나므로 각자 다른 길로 흩어진다. 화면 행 기준으로 흔들면 기둥
    //     전체가 한 몸처럼 물결쳐 인위적으로 보인다 — 조각 기준이라야 자연스럽다.
    //   · 가로 퍼짐 — 올라갈수록 좌우로 벌어진다(_spreadAmount). 연기가 확산되는 느낌.
    //   · 농도 — 조각마다 0.6~1.0 배로 달라 균일한 띠로 안 보인다.
    //   · 소멸 — (1-age)^1.4 로 비선형이라 끝에서 빠르게 옅어진다.
    //
    // 조각은 서로 독립적이다(각자 자기 d만 가지고 태어났다 사라진다). 그래서 행마다
    // 다른 속도를 줬다가 시간이 지나며 위상이 어긋나 무너졌던 예전 버그가 구조적으로
    // 재발할 수 없다.
    //
    // ── 작업 텍스처는 원본과 "완전히 같은 크기·피벗"이다 ─────────────────
    // 김 영역만 잘라 작은 텍스처를 만들면 크롭 중심과 피벗을 맞추는 보정이 필요해지고,
    // 그 계산이 한 끗 어긋나면 Play 시작 순간 위치가 튄다(실제로 두 번 재발했다).
    // 원본과 같은 가로세로·같은 피벗이면 배치가 100% 동일해 어긋날 여지가 없다.
    // 매 프레임 다시 칠하는 범위만 작게 잡아 부분 업로드로 저렴하게 유지한다.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SmokeDrift : MonoBehaviour
    {
        [SerializeField] private bool _enabled = true;

        [Tooltip("초당 올라가는 픽셀 수(아트 px).")]
        [SerializeField] private float _riseRowsPerSecond = 7f;

        [Tooltip("내보낼 조각의 높이(원본 아래쪽부터, 아트 px). 원본 플룸 전체를 " +
            "반복 단위로 쓰면 덩어리가 뚝뚝 떠오르는 것처럼 보인다 — 잔에 닿는 좁은 " +
            "밑동만 잘라 써야 촘촘히 이어진다. 0이면 전체.")]
        [Min(0)]
        [SerializeField] private int _emitSourceRows = 8;

        [Tooltip("새 조각이 나오는 간격(아트 px). 작을수록 촘촘하고 연속적이다.")]
        [Min(1)]
        [SerializeField] private int _emitCyclePx = 2;

        [Tooltip("조각이 완전히 사라질 때까지 올라가는 거리(아트 px).")]
        [Min(1)]
        [SerializeField] private int _lifetimePx = 34;

        [Tooltip("나오기 시작할 때 이 거리(아트 px) 동안 알파를 0→1로 올린다.")]
        [Min(0)]
        [SerializeField] private int _fadeInPx = 2;

        [Tooltip("조각이 좌우로 벗어나는 최대 폭(아트 px). 올라갈수록 커진다.")]
        [SerializeField] private int _swayAmpPx = 5;

        [Tooltip("조각의 좌우 경로가 꿈틀거리는 빈도(올라간 거리 1px당). " +
            "작을수록 완만하게 휜다.")]
        [SerializeField] private float _swayFrequency = 0.07f;

        [Tooltip("다 올라갔을 때 가로로 몇 배 퍼지는지. 1.8이면 1.8배까지 벌어진다. " +
            "0이면 안 퍼지고 그대로 올라간다.")]
        [Min(0f)]
        [SerializeField] private float _spreadAmount = 1.8f;

        private SpriteRenderer _renderer;
        private Sprite _sourceSprite;
        private Sprite _workSprite;
        private Texture2D _workTex;

        private Color32[] _src;        // 원본 텍스처 전체(절대 좌표)
        private Color32[] _work;       // 다시 칠할 영역만 담는 버퍼
        private bool[] _srcRowFilled;  // 조각의 각 행에 내용이 있는지(빈 행 건너뛰기)
        private int _texW, _texH;
        private int _minX, _minY;      // 다시 칠할 영역 시작 — 절대 좌표
        private int _boxW, _boxH;      // 다시 칠할 영역 크기(올라갈 여유 포함)
        private int _srcRowCount;      // 조각 높이
        private float _centerX;        // 조각의 가로 중심(영역 로컬) — 퍼짐의 기준점

        private float _baseAlpha;
        private float _travel;         // 지금까지 올라간 총 거리(조각 발행 번호의 기준)
        private bool _ready;

        private void Awake() => Build();

        // 화면 전환으로 껐다 켜질 때(Awake는 한 번뿐) 작업 스프라이트로 되돌린다.
        private void OnEnable()
        {
            if (_ready && _renderer != null)
                _renderer.sprite = _workSprite;
        }

        private void Build()
        {
            if (_ready)
                return;

            _renderer = GetComponent<SpriteRenderer>();
            _sourceSprite = _renderer.sprite;
            if (_sourceSprite == null || _sourceSprite.texture == null || !_sourceSprite.texture.isReadable)
            {
                Debug.LogWarning("[SmokeDrift] 소스 스프라이트 텍스처를 읽을 수 없다(Read/Write 꺼짐?). 비활성화.");
                enabled = false;
                return;
            }

            var tex = _sourceSprite.texture;
            _texW = tex.width;
            _texH = tex.height;
            _src = tex.GetPixels32();

            // 불투명 영역 bbox
            int minX = _texW, minY = _texH, maxX = -1, maxY = -1;
            for (int y = 0; y < _texH; y++)
            for (int x = 0; x < _texW; x++)
            {
                if (_src[y * _texW + x].a == 0)
                    continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            if (maxX < 0)
            {
                enabled = false;
                return;
            }

            // 가로 여유: 흔들림 + 퍼짐으로 벗어나는 만큼 확보한다.
            var halfWidth = (maxX - minX) * 0.5f;
            var padX = _swayAmpPx + Mathf.CeilToInt(halfWidth * _spreadAmount) + 2;
            _minX = Mathf.Max(0, minX - padX);
            var maxXPadded = Mathf.Min(_texW - 1, maxX + padX);
            _boxW = maxXPadded - _minX + 1;
            _centerX = (minX + maxX) * 0.5f - _minX;

            // 조각: 잔에 닿는 아래쪽 일부만(아래 1px 투명 패딩 포함).
            _minY = Mathf.Max(0, minY - 1);
            var srcTop = Mathf.Min(_texH - 1, maxY + 1);
            var fullRows = srcTop - _minY + 1;
            _srcRowCount = _emitSourceRows > 0 ? Mathf.Min(_emitSourceRows, fullRows) : fullRows;

            _srcRowFilled = new bool[_srcRowCount];
            for (int r = 0; r < _srcRowCount; r++)
            {
                var y = _minY + r;
                for (int x = _minX; x < _minX + _boxW; x++)
                {
                    if (_src[y * _texW + x].a != 0) { _srcRowFilled[r] = true; break; }
                }
            }

            // 조각이 올라가 사라질 자리를 위로 확보(없으면 올라가다 영역 끝에서 잘린다).
            _boxH = Mathf.Min(_srcRowCount + _lifetimePx + 3, _texH - _minY);
            _work = new Color32[_boxW * _boxH];

            // 작업 텍스처는 원본과 정확히 같은 크기 — 크롭도, 피벗 보정도 없다.
            _workTex = new Texture2D(_texW, _texH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "SmokeWork",
            };
            _workTex.SetPixels32(_src);
            _workTex.Apply(false);

            var pivotNormalized = new Vector2(_sourceSprite.pivot.x / _texW, _sourceSprite.pivot.y / _texH);
            _workSprite = Sprite.Create(
                _workTex, new Rect(0, 0, _texW, _texH), pivotNormalized,
                _sourceSprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            _workSprite.name = "SmokeWork";
            _renderer.sprite = _workSprite;

            _baseAlpha = _renderer.color.a;
            _ready = true;
        }

        private void OnDisable()
        {
            if (!_ready)
                return;

            _renderer.sprite = _sourceSprite;
        }

        private void OnDestroy()
        {
            if (_workTex != null) Destroy(_workTex);
            if (_workSprite != null) Destroy(_workSprite);
        }

        // 발행 번호에서 안정적인 0~1 난수. 같은 조각은 사는 동안 늘 같은 값을 받는다.
        private static float Hash01(int n, int salt)
        {
            unchecked
            {
                var h = (uint)(n * 374761393 + salt * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFFu) / (float)0xFFFFFF;
            }
        }

        // 부드럽게 이어지는 1D 값 노이즈 — 조각의 좌우 경로에 쓴다.
        private static float Noise1D(float x, int seed)
        {
            var i = Mathf.FloorToInt(x);
            var f = x - i;
            f = f * f * (3f - 2f * f);
            return Mathf.Lerp(Hash01(i, seed), Hash01(i + 1, seed), f);
        }

        private void Update()
        {
            if (!_ready || !_enabled)
                return;

            var cycle = Mathf.Max(_emitCyclePx, 1);
            _travel += _riseRowsPerSecond * Time.deltaTime;

            // 정밀도 위생 — 발행 번호 간격(cycle)의 배수만큼만 접어 조각 간 관계를 보존한다.
            var fold = cycle * 100000f;
            if (_travel > fold)
                _travel -= fold;

            System.Array.Clear(_work, 0, _work.Length);

            var newest = Mathf.FloorToInt(_travel / cycle);
            var copies = Mathf.CeilToInt((float)_lifetimePx / cycle) + 2;

            for (int k = 0; k < copies; k++)
            {
                var emitIndex = newest - k;
                var d = _travel - emitIndex * (float)cycle;
                if (d < 0f || d > _lifetimePx)
                    continue;

                var ageFrac = d / _lifetimePx;
                var fadeIn = _fadeInPx > 0 ? Mathf.Clamp01(d / _fadeInPx) : 1f;
                // 조각마다 농도가 달라야 균일한 띠로 안 보인다. 끝에서 빠르게 옅어지도록 비선형.
                var puffAlpha = fadeIn
                    * Mathf.Pow(1f - ageFrac, 1.4f)
                    * Mathf.Lerp(0.6f, 1f, Hash01(emitIndex, 29));
                if (puffAlpha <= 0.002f)
                    continue;

                // 조각마다 다른 경로로 흔들린다(화면 행이 아니라 조각 기준).
                var sway = (Noise1D(d * _swayFrequency + Hash01(emitIndex, 11) * 40f, emitIndex * 7919) - 0.5f)
                    * 2f * _swayAmpPx * ageFrac;
                var spread = 1f + _spreadAmount * ageFrac;
                var invSpread = 1f / Mathf.Max(spread, 0.0001f);

                var risen = Mathf.RoundToInt(d);
                for (int by = 0; by < _boxH; by++)
                {
                    var srcRow = by - risen;
                    if (srcRow < 0 || srcRow >= _srcRowCount || !_srcRowFilled[srcRow])
                        continue;

                    var srcAbsY = _minY + srcRow;
                    for (int bx = 0; bx < _boxW; bx++)
                    {
                        // 올라갈수록 벌어지므로, 출력 픽셀은 중심에 더 가까운 소스를 본다.
                        var offset = (bx - _centerX) * invSpread - sway;
                        var srcX = _minX + Mathf.RoundToInt(_centerX + offset);
                        if (srcX < _minX || srcX >= _minX + _boxW)
                            continue;

                        var c = _src[srcAbsY * _texW + srcX];
                        if (c.a == 0)
                            continue;

                        var na = (byte)(c.a * puffAlpha * _baseAlpha);
                        var idx = by * _boxW + bx;
                        if (na > _work[idx].a) // 조각이 겹치면 진한 쪽을 남긴다
                        {
                            c.a = na;
                            _work[idx] = c;
                        }
                    }
                }
            }

            _workTex.SetPixels32(_minX, _minY, _boxW, _boxH, _work, 0);
            _workTex.Apply(false);
        }
    }
}
