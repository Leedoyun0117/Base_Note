using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameName.UI.MemoryRoom.Space
{
    // 펜던트 램프에 "오래된 백열구가 숨쉬듯" 미세한 밝기 흔들림을 준다.
    //
    // 고장난 형광등처럼 껌뻑이는 게 아니라, 대부분의 시간은 안정적이고 진폭이
    // 아주 작은 떨림이다. 매 프레임 새 난수를 뽑으면 지직거리므로 Perlin 노이즈로
    // 이어지는 곡선을 쓴다(Perlin 값은 0.5 근처에 몰려 있어 큰 변화가 드물다).
    //
    // 기본 강도는 시작할 때 한 번만 캡처한다 — 조명 빌더나 인스펙터에서 맞춰 둔
    // Light2D.intensity를 이 컴포넌트가 덮어쓰지 않기 위함이다. 꺼지거나 비활성일
    // 때는 캡처한 원래 값으로 되돌린다.
    //
    // ── dip(가끔 훅 꺼짐)은 하드 컷, 코어까지 함께 꺼진다 ────────────────
    // 부드러운 감쇠(sin 곡선)로는 "어두워졌다 밝아짐"이지 "툭 꺼졌다 켜짐"이
    // 안 된다. dip 중에는 보간 없이 즉시 _dipDepth 밝기로 떨어뜨리고 유지하다가
    // _dipDuration 뒤 즉시 원래대로 되돌린다(사각파). Lamp 스프라이트(Unlit)도
    // _spriteFlickerScale 비율로 같이 꺼져 블룸 글로우까지 함께 빠진다 —
    // 라이트만 어두워지고 전구 코어가 계속 빛나면 "빛만 떨리는" 어색함이 남는다.
    // 미세한 호흡(breath, Perlin)은 dip과 무관하게 항상 켜져 있다.
    //
    // ── 정전은 확정적이다 ────────────────────────────────────────────
    // 예전엔 깜빡임 횟수와 정전 여부가 둘 다 무작위라 언제 정전이 오는지 읽히지
    // 않았다. 지금은 규칙이 하나다:
    //   5~8초 대기 → "깜빡깜빡"(_blinksPerFlicker번 껐다켜기) — 이걸 한 번으로 센다.
    //   그 깜빡임을 _flickersBeforeBlackout번(기본 3) 채우면 →
    //   _blackoutDuration 동안 완전 암전 → _recoveryDuration에 걸쳐 SmoothStep으로
    //   서서히 복귀 → 카운트를 0으로 되돌리고 처음부터.
    // 무작위는 "언제 깜빡일지"(대기 시간) 하나뿐이라, 세 번째 깜빡임 뒤엔 반드시
    // 정전이 온다는 걸 플레이어가 신호로 읽을 수 있다. 복귀 구간만 유일하게
    // 부드럽고, 꺼지는 순간은 전부 하드 컷이다.
    [RequireComponent(typeof(Light2D))]
    public sealed class Light2DFlicker : MonoBehaviour
    {
        [SerializeField] private bool _enabled = true;

        [Header("숨쉬는 떨림 (Perlin)")]
        [Tooltip("기본 강도 대비 흔들림 폭. 0.28 = 대략 ±28%. DotTexture 포스터라이즈 " +
            "계단(10단계면 한 칸 ≈ 10%)을 확실히 넘길 만큼 크게 잡아야 화면에서 보인다.")]
        [SerializeField] private float _amplitude = 0.28f;

        [Tooltip("떨림 속도. 클수록 빠르게 흔들린다.")]
        [SerializeField] private float _speed = 0.9f;

        [Header("깜빡임 주기 — '2번 깜빡'을 3회 반복하면 정전")]
        [Tooltip("깜빡임 사이의 최소 대기 시간(초).")]
        [Min(0f)]
        [SerializeField] private float _intervalMinSeconds = 5f;

        [Tooltip("깜빡임 사이의 최대 대기 시간(초). 최소값과 이 값 사이에서 무작위로 " +
            "정해진다 — 무작위는 오직 '언제' 깜빡일지뿐이고, 횟수와 정전 시점은 확정적이다.")]
        [Min(0f)]
        [SerializeField] private float _intervalMaxSeconds = 8f;

        [Tooltip("한 번 깜빡일 때 연속으로 몇 번 껐다 켜는지. 2면 '깜빡깜빡'.")]
        [Min(1)]
        [SerializeField] private int _blinksPerFlicker = 2;

        [Tooltip("이 횟수만큼 깜빡이고 나면 정전된다. 3이면 '깜빡깜빡' 세 번 뒤 정전.")]
        [Min(1)]
        [SerializeField] private int _flickersBeforeBlackout = 3;

        [Tooltip("깜빡임 한 번의 밝기. 0.95 = 순간 5%로 뚝 떨어짐. 보간 없는 하드 컷.")]
        [SerializeField] private float _dipDepth = 0.95f;

        [Tooltip("깜빡임 한 번이 유지되는 시간(초). 짧아야 '툭'한다.")]
        [SerializeField] private float _dipDuration = 0.08f;

        [Tooltip("정전 중 밝기. 1 = 완전히 꺼짐(라이트·스프라이트 전부 0).")]
        [Range(0f, 1f)]
        [SerializeField] private float _blackoutDepth = 1f;

        [Tooltip("깜빡임 끝난 뒤 완전히 꺼진 채 머무는 시간(초).")]
        [SerializeField] private float _blackoutDuration = 2.5f;

        [Tooltip("꺼진 뒤 원래 밝기로 서서히 돌아오는 데 걸리는 시간(초). 하드 컷과 " +
            "대비되게 부드러운 곡선(SmoothStep)으로 천천히 밝아진다.")]
        [SerializeField] private float _recoveryDuration = 2f;

        [Header("스프라이트(전구 코어)도 함께 흔들기")]
        [Tooltip("Lamp 스프라이트(Unlit). 지정하면 코어 밝기도 같이 흔들려 " +
            "빛만 떨리는 어색함을 줄인다. 비우면 빛만 흔든다. dip도 함께 적용된다.")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Tooltip("스프라이트에 적용할 흔들림 비율(라이트 대비). 0.6 = 빛의 60%만.")]
        [Range(0f, 1f)]
        [SerializeField] private float _spriteFlickerScale = 0.6f;

        [Tooltip("스프라이트 밝기 하한(기본값 대비). 0에 가까울수록 dip 중 코어가 " +
            "거의 완전히 꺼진다(블룸 글로우도 함께 빠짐). 글로우가 안정적이길 " +
            "원하면 0.97로 올린다.")]
        [Range(0f, 1f)]
        [SerializeField] private float _spriteMinFactor;

        [Header("디버그")]
        [Tooltip("켜면 대략 1초에 한 번 현재 intensity/factor를 콘솔에 찍는다 — 흔들림이 " +
            "실제로 적용되는지 확인용.")]
        [SerializeField] private bool _debugLog;

        private Light2D _light;
        private float _baseIntensity;
        private Color _baseColor = Color.white;
        private float _noiseSeed;
        private bool _captured;

        // 상태 기계 — 무작위는 "언제 깜빡일지"(Idle 대기 시간) 하나뿐이다:
        //   Idle(5~8초 대기) → On ⇄ Gap 을 _blinksPerFlicker번 = 한 번의 깜빡임
        //   그 깜빡임을 _flickersBeforeBlackout번 세고 나면
        //     → Blackout(완전 암전) → Recovering(서서히 복귀) → 처음으로
        private enum DipState { Idle, On, Gap, Blackout, Recovering }
        private DipState _dipState = DipState.Idle;
        private float _dipStateTimer;
        private int _blinksRemaining;   // 이번 깜빡임에서 남은 껐다켜기 횟수
        private int _flickersDone;      // 정전까지 지금까지 센 깜빡임 횟수
        private float _nextLogTime;
        private bool _intervalArmed;

        private void Awake() => Capture();

        // 기본값 캡처는 딱 한 번. OnEnable에서 다시 잡으면 흔들리던 값을 기준으로
        // 삼아 강도가 조금씩 밀려날 수 있다.
        private void Capture()
        {
            if (_captured)
                return;

            _light = GetComponent<Light2D>();
            _baseIntensity = _light.intensity;
            if (_spriteRenderer != null)
                _baseColor = _spriteRenderer.color;
            _noiseSeed = Random.value * 100f;
            _captured = true;
        }

        private void OnDisable() => Restore();

        // 흔들린 값이 그대로 굳지 않도록 원래 값으로 되돌린다.
        private void Restore()
        {
            if (!_captured)
                return;

            _light.intensity = _baseIntensity;
            if (_spriteRenderer != null)
                _spriteRenderer.color = _baseColor;
        }

        private void Update()
        {
            if (_light == null)
                return;

            if (!_enabled)
            {
                Restore();
                return;
            }

            // 숨쉬는 떨림 — Perlin은 0.5 근처에 몰려 있어 대부분 작은 값이다.
            var t = Time.time * _speed;
            var breath = (Mathf.PerlinNoise(t, _noiseSeed) - 0.5f) * 2f * _amplitude;

            // 깜빡임/정전 시퀀스 — 사각파(하드 컷). 무작위는 Idle 대기 시간뿐이고,
            // "깜빡깜빡"을 _flickersBeforeBlackout번 채우면 반드시 정전으로 간다.
            //
            // 깜빡임·정전 구간에서는 breath(미세 호흡)를 아예 죽인다 — 안 죽이면
            // "완전히 꺼짐" 상태에서도 breath가 미세하게 밝기를 되살려 진짜 0에 못 미친다.
            var dip = 0f;
            var suppressBreath = false;
            switch (_dipState)
            {
                case DipState.On:
                    dip = -_dipDepth; // 보간 없이 즉시 — "툭"
                    suppressBreath = true;
                    _dipStateTimer -= Time.deltaTime;
                    if (_dipStateTimer <= 0f)
                    {
                        _blinksRemaining--;
                        if (_blinksRemaining > 0)
                        {
                            _dipState = DipState.Gap;
                            _dipStateTimer = Random.Range(0.05f, 0.15f); // 껐다켜기 사이 짧은 간격
                        }
                        else
                        {
                            // 이번 깜빡임이 끝났다. 정해진 횟수를 채웠으면 정전, 아니면 다시 대기.
                            _flickersDone++;
                            if (_flickersDone >= Mathf.Max(_flickersBeforeBlackout, 1))
                            {
                                _flickersDone = 0;
                                _dipState = DipState.Blackout;
                                _dipStateTimer = Mathf.Max(_blackoutDuration, 0f);
                            }
                            else
                            {
                                _dipState = DipState.Idle;
                            }
                        }
                    }
                    break;

                case DipState.Gap:
                    dip = 0f; // 버스트 사이엔 정상 밝기로 돌아왔다가 다시 꺼짐
                    _dipStateTimer -= Time.deltaTime;
                    if (_dipStateTimer <= 0f)
                    {
                        _dipState = DipState.On;
                        _dipStateTimer = Mathf.Max(_dipDuration, 0.01f);
                    }
                    break;

                case DipState.Blackout:
                    dip = -_blackoutDepth; // 계속 꺼진 채로 유지 — 보간 없음, breath도 없음
                    suppressBreath = true;
                    _dipStateTimer -= Time.deltaTime;
                    if (_dipStateTimer <= 0f)
                    {
                        _dipState = DipState.Recovering;
                        _dipStateTimer = Mathf.Max(_recoveryDuration, 0.01f);
                    }
                    break;

                case DipState.Recovering:
                {
                    // 하드 컷과 대비되게 부드러운 곡선으로 서서히 밝아진다.
                    var progress = 1f - Mathf.Clamp01(_dipStateTimer / _recoveryDuration);
                    dip = Mathf.Lerp(-_blackoutDepth, 0f, Mathf.SmoothStep(0f, 1f, progress));
                    _dipStateTimer -= Time.deltaTime;
                    if (_dipStateTimer <= 0f)
                    {
                        _dipState = DipState.Idle;
                        dip = 0f;
                    }
                    break;
                }

                default: // Idle — 다음 깜빡임까지 대기. 여기서만 무작위가 쓰인다(대기 시간).
                    if (!_intervalArmed)
                    {
                        _dipStateTimer = Random.Range(
                            Mathf.Max(_intervalMinSeconds, 0f),
                            Mathf.Max(_intervalMaxSeconds, _intervalMinSeconds));
                        _intervalArmed = true;
                    }

                    _dipStateTimer -= Time.deltaTime;
                    if (_dipStateTimer <= 0f)
                    {
                        _blinksRemaining = Mathf.Max(_blinksPerFlicker, 1);
                        _intervalArmed = false;
                        _dipState = DipState.On;
                        _dipStateTimer = Mathf.Max(_dipDuration, 0.01f);
                    }
                    break;
            }

            var effectiveBreath = suppressBreath ? 0f : breath;
            var factor = Mathf.Max(1f + effectiveBreath + dip, 0f);
            _light.intensity = _baseIntensity * factor;

            if (_spriteRenderer != null)
            {
                // 코어도 dip을 그대로(1:1) 따라간다 — breath만 완충 비율로 줄여서
                // 평소엔 코어가 방보다 덜 흔들리지만, dip/정전 중엔 라이트와 똑같이
                // 완전히 꺼진다(0.6 같은 완충 비율을 dip에도 걸면 정전 중에도
                // 코어가 40%쯤 남아 "완전히 꺼짐"이 안 된다).
                var spriteFactor = Mathf.Clamp(1f + effectiveBreath * _spriteFlickerScale + dip, _spriteMinFactor, 2f);
                var c = _baseColor * spriteFactor;
                c.a = _baseColor.a;
                _spriteRenderer.color = c;
            }

            if (_debugLog && Time.time >= _nextLogTime)
            {
                _nextLogTime = Time.time + 1f;
                Debug.Log($"[Light2DFlicker] intensity {_light.intensity:0.000} " +
                    $"(base {_baseIntensity:0.000} × factor {factor:0.000}, breath {breath:+0.000;-0.000}, dip {dip:0.000})");
            }
        }
    }
}
