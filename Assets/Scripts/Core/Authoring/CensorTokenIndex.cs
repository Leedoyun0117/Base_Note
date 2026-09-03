using System;
using System.Collections.Generic;
using GameName.Core.Dialogue;

namespace GameName.Core.Authoring
{
    // 판 전체의 원문을 한 번 훑어 모은 검열 토큰 목록.
    //
    // 검열에 대해 묻는 규칙이 늘어나면서 생긴 타입이다. 규칙마다 방과 대사와
    // 선택지를 따로 훑으면 같은 원문을 규칙 수만큼 다시 파싱하게 되고, 무엇보다
    // "원문이 어디어디에 있는가"라는 지식이 규칙마다 복사된다 — 나중에 대사가
    // 붙는 자리가 하나 늘면 어느 규칙이 그것을 빠뜨렸는지 알 수 없게 된다.
    public sealed class CensorTokenIndex
    {
        private readonly HashSet<CensorKey> _keys;

        public IReadOnlyList<CensorTokenUse> Uses { get; }

        public CensorTokenIndex(IReadOnlyList<CensorTokenUse> uses)
        {
            Uses = uses ?? throw new ArgumentNullException(nameof(uses));

            _keys = new HashSet<CensorKey>();
            foreach (var use in uses)
                _keys.Add(use.Key);
        }

        // 이 키로 가려진 말이 판 안에 하나라도 있는가.
        public bool Contains(CensorKey key) => _keys.Contains(key);
    }
}
