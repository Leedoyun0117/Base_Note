using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 시계면 위에서 시침·분침을 돌린다. 실제 시계 속도가 아니라 "방 안에서
    // 시간이 흐르고 있다"가 눈에 보이게 하는 연출이다.
    //
    // ── 회전 0 = 그려진 포즈 ────────────────────────────────────────────
    // Hour.png / Min.png는 바늘이 프레임 안 제자리에 그려진 풀프레임이고,
    // 커스텀 피벗이 시계 허브에 맞춰져 있다. 그래서 localRotation 0이 곧 원본
    // 그림의 포즈다. 여기서부터 시계방향으로 각을 더해 나가면 시작 자세를 따로
    // 맞출 필요가 없다 — 그림이 무슨 시각을 그렸든 상관없이 그 자세에서 출발한다.
    //
    // ── 바늘마다 스냅을 따로 둔다 (실제 아날로그 시계처럼) ─────────────
    // 분침은 6도(60분할)로 스냅해 "똑딱"거리며 움직인다 — 60초/바퀴라 1초에
    // 한 칸씩. 스냅해야 픽셀아트 계단 윤곽이 프레임마다 흔들리지 않는다.
    //
    // 시침은 스냅 없이(0) 연속으로 스르륵 돈다 — 720초/바퀴로 아주 느려
    // 스냅하면 12·3·6·9시 같은 자리에서 잠깐 멈췄다 튀는 게 눈에 띈다.
    // 느린 만큼 Point 필터 지글거림도 거의 안 보인다.
    //
    // 화면 전환으로 껐다 켜지면 Update가 멈췄다 이어진다 — 방을 벗어난 동안
    // 시간이 멈춘 셈이라 자연스럽다.
    [DisallowMultipleComponent]
    public sealed class ClockHands : MonoBehaviour
    {
        [SerializeField] private bool _enabled = true;

        [Tooltip("돌릴 시침 Transform. localRotation 0이 그려진 포즈여야 한다.")]
        [SerializeField] private Transform _hourHand;

        [Tooltip("돌릴 분침 Transform. localRotation 0이 그려진 포즈여야 한다.")]
        [SerializeField] private Transform _minuteHand;

        [Tooltip("분침이 한 바퀴 도는 데 걸리는 시간(초). 실제 시계(3600초)가 아니라 " +
            "시간 흐름이 눈에 보이는 빠르기. 시침은 이 12배가 걸린다.")]
        [Min(0.01f)]
        [SerializeField] private float _minuteSecondsPerRevolution = 60f;

        [Tooltip("분침 회전 각도 스냅(도). 6이면 60분할 — 1초에 한 칸씩 '똑딱'. " +
            "픽셀아트 계단 윤곽이 프레임마다 흔들리지 않도록. 0 이하면 연속.")]
        [SerializeField] private float _minuteSnapDegrees = 6f;

        [Tooltip("시침 회전 각도 스냅(도). 0이면 연속으로 스르륵 돈다 — 느린 바늘이라 " +
            "스냅하면 자리에서 잠깐 멈췄다 튀는 게 눈에 띈다.")]
        [SerializeField] private float _hourSnapDegrees;

        // 그려진 포즈로부터 흐른 시간.
        private float _elapsedSeconds;

        private void Update()
        {
            if (!_enabled || _minuteSecondsPerRevolution <= 0f)
                return;

            _elapsedSeconds += Time.deltaTime;

            // 정밀도 위생 — 가장 긴 주기(시침)로 접는다. 그 안에서 분침은 정확히
            // 12바퀴라 위상이 보존되고, 각도가 몇 시간씩 쌓여 float가 뭉개지지 않는다.
            var hourPeriod = _minuteSecondsPerRevolution * 12f;
            if (_elapsedSeconds > hourPeriod)
                _elapsedSeconds -= hourPeriod;

            Apply(_minuteHand, _elapsedSeconds, _minuteSecondsPerRevolution, _minuteSnapDegrees);
            Apply(_hourHand, _elapsedSeconds, _minuteSecondsPerRevolution * 12f, _hourSnapDegrees);
        }

        private static void Apply(
            Transform hand, float elapsedSeconds, float secondsPerRevolution, float snapDegrees)
        {
            if (hand == null)
                return;

            var degrees = SnappedAngleDegrees(elapsedSeconds, secondsPerRevolution, snapDegrees);
            // 시계방향은 Z 음수. 그려진 포즈(0)에서 앞으로 돈다.
            hand.localRotation = Quaternion.Euler(0f, 0f, -degrees);
        }

        // 흐른 시간 → 그려진 포즈 기준 시계방향 회전각(도), 스냅 적용.
        // 씬 없이 검증하는 쪽이 이걸 직접 부른다.
        public static float SnappedAngleDegrees(
            float elapsedSeconds, float secondsPerRevolution, float snapDegrees)
        {
            if (secondsPerRevolution <= 0f)
                return 0f;

            var degrees = elapsedSeconds / secondsPerRevolution * 360f;

            if (snapDegrees > 0f)
                degrees = Mathf.Round(degrees / snapDegrees) * snapDegrees;

            return degrees;
        }
    }
}
