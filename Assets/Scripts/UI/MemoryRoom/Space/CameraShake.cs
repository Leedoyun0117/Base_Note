using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 신뢰가 깎일 때 방을 흔든다. 씬 콘텐츠 루트 Transform에 오프셋을 주는
    // 방식이라, 화면 좌표로 그려지는 UI Toolkit 레이어(마스크·HUD·대화 패널)는
    // 영향을 받지 않고 2D 씬만 떨린다.
    //
    // 카메라가 아니라 _shakeTarget(방 껍데기·단서·배경을 담은 루트)을 흔드는
    // 이유: 메인 카메라에는 Pixel Perfect Camera가 붙어 LateUpdate에서 위치를
    // 픽셀 격자에 스냅한다. 같은 프레임에 이 컴포넌트가 카메라를 밀면 둘이
    // 싸워 픽셀 격자가 떨린다. 카메라는 격자에 고정해 두고 방을 대신 움직인다.
    // _shakeTarget이 비어 있으면(도구 미실행·단위 테스트) 자기 Transform을
    // 흔든다 — 예전 동작 그대로다.
    //
    // 흔들림에는 두 겹이 있다:
    //   · 임펄스 — 신뢰가 한 칸 깎인 순간의 짧은 감쇠 흔들림(Shake).
    //   · 지속 떨림 — 신뢰가 위태로운 구간에 들어가 있는 동안 계속되는 미세한
    //     떨림(SetContinuous). 언제 켜고 끄는지는 컨트롤러가 정한다.
    // 두 겹은 한 프레임 안에서 더해져 함께 적용된다.
    //
    // 세기·길이·문턱은 전부 인스펙터에서 조절한다(코드에 상수를 박지 않는다).
    public sealed class CameraShake : MonoBehaviour
    {
        [Header("흔들 대상")]
        [Tooltip("실제로 오프셋을 받을 Transform. 비우면 이 컴포넌트가 붙은 오브젝트를 흔든다.")]
        [SerializeField] private Transform _shakeTarget;

        [Header("임펄스 (신뢰 한 칸 깎임)")]
        [Tooltip("임펄스의 최대 진폭(월드 단위). 방 길이에 견줘 아주 작게 둔다.")]
        [SerializeField] private float _magnitude = 0.0415f;

        [Tooltip("한 번의 임펄스가 잦아드는 데 걸리는 시간(초).")]
        [SerializeField] private float _duration = 0.35f;

        [Tooltip("신뢰가 0으로 떨어진 순간의 임펄스 배수. 방을 잃는 타격을 더 크게 알린다.")]
        [SerializeField] private float _failMagnitudeMultiplier = 1.8f;

        [Header("지속 떨림 (신뢰 위태로운 구간)")]
        [Tooltip("SetContinuous(true) 동안 이어지는 미세 떨림의 진폭. 임펄스보다 훨씬 작게.")]
        [SerializeField] private float _continuousMagnitude = 0.01385f;

        [Tooltip("지속 떨림의 빠르기. 클수록 손이 더 떨리는 느낌.")]
        [SerializeField] private float _continuousFrequency = 14f;

        // 흔들림이 처음 시작될 때의 원위치. 전부 잦아들면 정확히 이 값으로 되돌린다.
        private Vector3 _restLocalPosition;
        private bool _hasRest;

        private float _impulseRemaining;
        private float _impulseMagnitude;
        private bool _continuous;

        // 지금 지속 떨림이 켜져 있는가. 씬 없이 검증하는 쪽이 본다.
        public bool ContinuousActive => _continuous;

        // 지금 임펄스가 잦아드는 중인가.
        public bool ImpulseActive => _impulseRemaining > 0f;

        // 오프셋을 실제로 받는 Transform. 인스펙터에서 지정하지 않았으면 자기 자신.
        private Transform Target => _shakeTarget != null ? _shakeTarget : transform;

        private void CaptureRestIfIdle()
        {
            if (_hasRest)
                return;

            _restLocalPosition = Target.localPosition;
            _hasRest = true;
        }

        // strong = true면 (신뢰 0 도달) 더 크게 흔든다.
        public void Shake(bool strong)
        {
            if (_duration <= 0f || _magnitude <= 0f)
                return;

            CaptureRestIfIdle();
            _impulseRemaining = _duration;
            _impulseMagnitude = strong ? _magnitude * _failMagnitudeMultiplier : _magnitude;
        }

        // 신뢰가 문턱 이하로 내려가 있는 동안 화면이 계속 미세하게 떨리게 한다.
        // 컨트롤러가 신뢰 변화·방 시작마다 지금 상태를 다시 알려준다.
        public void SetContinuous(bool on)
        {
            if (on == _continuous)
                return;

            _continuous = on;
            if (on)
                CaptureRestIfIdle();
        }

        // 카메라·픽셀 스냅이 끝난 뒤에 오프셋을 얹어야 한 프레임 어긋나지 않는다.
        private void LateUpdate()
        {
            var offset = Vector3.zero;
            var active = false;

            if (_continuous && _continuousMagnitude > 0f)
            {
                active = true;
                // 시간 기반이라 프레임률과 무관하다. 두 축에 서로 다른 노이즈를
                // 물려 원을 그리지 않고 불규칙하게 떨리게 한다.
                var t = Time.time * _continuousFrequency;
                var x = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f;
                var y = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f;
                offset += new Vector3(x, y, 0f) * _continuousMagnitude;
            }

            if (_impulseRemaining > 0f)
            {
                active = true;
                _impulseRemaining -= Time.deltaTime;

                // 남은 시간이 줄수록 진폭도 선형으로 잦아든다.
                var falloff = Mathf.Max(_impulseRemaining, 0f) / _duration;
                offset += (Vector3)(Random.insideUnitCircle * (_impulseMagnitude * falloff));
            }

            if (!active)
            {
                RestoreRest();
                return;
            }

            Target.localPosition = _restLocalPosition + offset;
        }

        private void RestoreRest()
        {
            if (!_hasRest)
                return;

            Target.localPosition = _restLocalPosition;
            _hasRest = false;
        }

        private void OnDisable()
        {
            _impulseRemaining = 0f;
            _continuous = false;
            RestoreRest();
        }
    }
}
