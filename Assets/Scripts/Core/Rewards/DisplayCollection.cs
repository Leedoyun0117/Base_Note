using System;
using System.Collections.Generic;

namespace GameName.Core.Rewards
{
    // 의뢰인에게 받은 선물을 진열해 쌓아 온 감정량 잔액.
    //
    // 일부러 IResettable을 구현하지 않는다 — 이것만은 의뢰 하나의 플레이
    // 상태가 아니라 여러 의뢰에 걸쳐 누적되는 진행 전체의 자산이다(업그레이드
    // 구매 재원). 의뢰가 바뀔 때 함께 지워지면 업그레이드를 위해 모아 온
    // 감정량이 의뢰 하나 끝날 때마다 사라지는 셈이 되어 진열/업그레이드
    // 시스템 자체가 성립하지 않는다.
    public sealed class DisplayCollection
    {
        private readonly List<Gift> _gifts = new List<Gift>();

        public IReadOnlyList<Gift> Gifts => _gifts;
        public int AvailableEmotionalValue { get; private set; }

        public void Add(Gift gift)
        {
            if (gift == null) throw new ArgumentNullException(nameof(gift));

            _gifts.Add(gift);
            AvailableEmotionalValue += gift.EmotionalValue;
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "지출량은 양수여야 한다.");

            if (amount > AvailableEmotionalValue)
                return false;

            AvailableEmotionalValue -= amount;
            return true;
        }
    }
}
