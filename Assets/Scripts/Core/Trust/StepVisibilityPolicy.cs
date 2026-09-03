using System;
using System.Collections.Generic;

namespace GameName.Core.Trust
{
    // 신뢰도 → 가시 비율 대응을 표로 받아 그대로 적용하는 정책.
    //
    // 계산식을 박지 않는 이유는 IVisibilityPolicy 주석 그대로다 — 이 대응은
    // 밸런싱 대상이고 선형일 필요도 없다(3에서 1.0이었다가 2에서 0.75로 뚝
    // 떨어지고 1과 0이 같을 수 있다). 그래서 표를 통째로 주입받고, 이 클래스는
    // 표에서 값을 꺼내는 규칙만 안다.
    //
    // 표에 없는 신뢰도는 표의 최솟값~최댓값으로 자른 뒤, 그 이하에서 가장 큰
    // 키의 값을 쓴다 — 표가 도달 가능한 모든 신뢰도(0..시작값)를 덮는다는 전제
    // 아래에서는 이 보간이 쓰일 일이 없지만, 계산 결과로 잠깐 범위를 벗어난
    // 값이 들어와도 예외로 터지지 않게 해 둔다.
    public sealed class StepVisibilityPolicy : IVisibilityPolicy
    {
        private readonly int[] _thresholdsDescending;
        private readonly Dictionary<int, float> _ratioByTrust;

        public StepVisibilityPolicy(IReadOnlyDictionary<int, float> ratioByTrust)
        {
            if (ratioByTrust == null) throw new ArgumentNullException(nameof(ratioByTrust));
            if (ratioByTrust.Count == 0)
                throw new ArgumentException("가시 비율 표는 비어 있을 수 없다.", nameof(ratioByTrust));

            _ratioByTrust = new Dictionary<int, float>();
            foreach (var pair in ratioByTrust)
                _ratioByTrust[pair.Key] = pair.Value;

            _thresholdsDescending = new int[_ratioByTrust.Count];
            _ratioByTrust.Keys.CopyTo(_thresholdsDescending, 0);
            Array.Sort(_thresholdsDescending);
            Array.Reverse(_thresholdsDescending);
        }

        public float GetVisibleRatio(int trust)
        {
            if (_ratioByTrust.TryGetValue(trust, out var exact))
                return exact;

            var highest = _thresholdsDescending[0];
            if (trust >= highest)
                return _ratioByTrust[highest];

            var lowest = _thresholdsDescending[_thresholdsDescending.Length - 1];
            if (trust <= lowest)
                return _ratioByTrust[lowest];

            // 표의 최솟값과 최댓값 사이의 빈칸: 그 이하에서 가장 큰 칸으로 내림.
            foreach (var threshold in _thresholdsDescending)
            {
                if (threshold <= trust)
                    return _ratioByTrust[threshold];
            }

            return _ratioByTrust[lowest];
        }
    }
}
