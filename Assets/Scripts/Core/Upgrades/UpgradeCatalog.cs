using System;
using System.Collections.Generic;

namespace GameName.Core.Upgrades
{
    // IUpgradeCatalog 기본 구현. 등급표 전체를 생성자로만 주입받는다 — 밸런싱
    // 수치를 코드 여기저기에 매직 넘버로 흩어두지 않기 위함이다.
    public sealed class UpgradeCatalog : IUpgradeCatalog
    {
        private readonly Dictionary<(UpgradeCategory, int), UpgradeOption> _optionsByCategoryAndLevel;

        public UpgradeCatalog(IReadOnlyList<UpgradeOption> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            _optionsByCategoryAndLevel = new Dictionary<(UpgradeCategory, int), UpgradeOption>(options.Count);
            foreach (var option in options)
            {
                var key = (option.Category, option.Level);
                if (_optionsByCategoryAndLevel.ContainsKey(key))
                {
                    throw new ArgumentException(
                        $"같은 종류·레벨의 업그레이드가 중복되었다({option.Category}, {option.Level}).", nameof(options));
                }

                _optionsByCategoryAndLevel.Add(key, option);
            }
        }

        public bool TryGetOption(UpgradeCategory category, int level, out UpgradeOption option) =>
            _optionsByCategoryAndLevel.TryGetValue((category, level), out option);
    }
}
