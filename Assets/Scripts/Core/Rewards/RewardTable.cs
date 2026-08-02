using System;
using System.Collections.Generic;

namespace GameName.Core.Rewards
{
    // 의뢰 하나에 딸린 보상 등급표. 평균 정확도 하나를 등급 하나(=선물 하나)로
    // 바꾸는 규칙을 여기 한 곳에 모은다 — 완료 처리기가 등급 판정 로직을
    // 직접 계산하지 않게 하기 위함이다.
    public sealed class RewardTable
    {
        private readonly List<RewardTier> _tiersDescending;

        public RewardTable(IReadOnlyList<RewardTier> tiers)
        {
            if (tiers == null) throw new ArgumentNullException(nameof(tiers));
            if (tiers.Count == 0)
                throw new ArgumentException("보상 등급이 최소 하나는 있어야 한다.", nameof(tiers));

            _tiersDescending = new List<RewardTier>(tiers);
            _tiersDescending.Sort((a, b) => b.MinimumAverageAccuracy.CompareTo(a.MinimumAverageAccuracy));

            // 최하 등급(하한 0)이 없으면 정확도가 아주 낮을 때 어떤 등급도
            // 못 찾는 경우가 생긴다 — 데이터 실수를 생성 시점에 바로 드러낸다.
            if (_tiersDescending[_tiersDescending.Count - 1].MinimumAverageAccuracy > 0.0)
                throw new ArgumentException("정확도 하한이 0인 최하 등급이 반드시 하나 있어야 한다.", nameof(tiers));
        }

        public Gift Resolve(double averageAccuracy)
        {
            foreach (var tier in _tiersDescending)
            {
                if (averageAccuracy >= tier.MinimumAverageAccuracy)
                    return new Gift(tier.EmotionalValue, tier.ReactionDialogue);
            }

            // 위 생성자 검증(최하 등급 하한 0)이 지켜지는 한 여기 도달할 수 없다.
            throw new InvalidOperationException("일치하는 보상 등급을 찾지 못했다.");
        }
    }
}
