using System;
using System.Collections.Generic;

namespace GameName.Core.Complexes
{
    // |안정 위치| → 발생 확률 대응을 표로 받아 그대로 적용하는 정책.
    //
    // StepVisibilityPolicy와 같은 구조다 — 계산식을 박지 않고 표를 통째로
    // 주입받아, 표에서 값을 꺼내는 규칙만 안다. 대응은 밸런싱 대상이고 선형일
    // 필요도 없다(안정 폭 20 이내면 0.0, 40에서 0.15, 80 이상에서 0.6 처럼).
    //
    // 축은 부호가 있지만(음수 침체 / 양수 흥분) 발생 확률은 "얼마나 극단인가"만
    // 보므로 |위치|로 조회한다. 표에 없는 값은 그 이하에서 가장 큰 키의 값을
    // 쓰고(계단식 내림), 표의 최대 키를 넘으면 그 값으로 고정한다.
    public sealed class StepComplexSpawnPolicy : IComplexSpawnPolicy
    {
        private readonly int[] _thresholdsDescending;
        private readonly Dictionary<int, float> _chanceByDistance;

        public StepComplexSpawnPolicy(IReadOnlyDictionary<int, float> chanceByDistance)
        {
            if (chanceByDistance == null) throw new ArgumentNullException(nameof(chanceByDistance));
            if (chanceByDistance.Count == 0)
                throw new ArgumentException("발생 확률 표는 비어 있을 수 없다.", nameof(chanceByDistance));

            _chanceByDistance = new Dictionary<int, float>();
            foreach (var pair in chanceByDistance)
            {
                if (pair.Key < 0)
                    throw new ArgumentException("표의 키는 |안정 위치|라 음수일 수 없다.", nameof(chanceByDistance));

                _chanceByDistance[pair.Key] = Clamp01(pair.Value);
            }

            _thresholdsDescending = new int[_chanceByDistance.Count];
            _chanceByDistance.Keys.CopyTo(_thresholdsDescending, 0);
            Array.Sort(_thresholdsDescending);
            Array.Reverse(_thresholdsDescending);
        }

        public float SpawnChance(int stabilityPosition)
        {
            var distance = Math.Abs(stabilityPosition);

            if (_chanceByDistance.TryGetValue(distance, out var exact))
                return exact;

            var highest = _thresholdsDescending[0];
            if (distance >= highest)
                return _chanceByDistance[highest];

            foreach (var threshold in _thresholdsDescending)
            {
                if (threshold <= distance)
                    return _chanceByDistance[threshold];
            }

            var lowest = _thresholdsDescending[_thresholdsDescending.Length - 1];
            return _chanceByDistance[lowest];
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
