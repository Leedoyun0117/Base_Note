using System;
using System.Collections.Generic;

namespace GameName.Core.Memories
{
    // IMemoryColorWalletMutator 기본 구현. 지금 어떤 색을 몇 개 들고 있는지를 든다.
    //
    // 방이 바뀌어도 리셋되지 않는다 — 추출로 모은 색은 다음 방의 검열을 푸는 데
    // 쓰이고, 방을 실패해 못 모은 색은 그대로 없는 채로 다음 방을 간다. 그
    // 이어짐이 런의 뼈대라 RoomStartedEvent를 구독하지 않는다.
    public sealed class MemoryColorWallet : IMemoryColorWalletMutator
    {
        private readonly Dictionary<MemoryColor, int> _countByColor = new Dictionary<MemoryColor, int>();

        public int GetCount(MemoryColor color) =>
            _countByColor.TryGetValue(color, out var count) ? count : 0;

        public void Add(MemoryColor color, int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "더할 수량은 음수일 수 없다. 빼려면 Remove를 쓴다.");

            _countByColor[color] = GetCount(color) + amount;
        }

        public void Remove(MemoryColor color, int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "뺄 수량은 음수일 수 없다.");

            _countByColor[color] = Math.Max(0, GetCount(color) - amount);
        }
    }
}
