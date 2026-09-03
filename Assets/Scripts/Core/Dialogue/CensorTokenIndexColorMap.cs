using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Memories;

namespace GameName.Core.Dialogue
{
    // 1단계 저작 토큰 목록(CensorTokenIndex)을 훑어 키 → 색 대응을 만든다.
    //
    // 저작 계층이 이미 판 전체의 원문을 한 번 파싱해 토큰 목록을 만들어 두므로
    // (ICensorTokenIndexSource), 여기서 다시 파싱하지 않고 그 목록만 받아 접는다.
    // 같은 키가 여러 색으로 적히는 어긋남은 1단계 검증기가 저작 시점에 막으니,
    // 첫 번째로 만난 색을 그 키의 색으로 삼는다.
    public sealed class CensorTokenIndexColorMap : ICensorKeyColorMap
    {
        private readonly Dictionary<CensorKey, MemoryColor> _colorByKey = new Dictionary<CensorKey, MemoryColor>();

        public CensorTokenIndexColorMap(ICensorTokenIndexSource tokens, RunDefinition run)
        {
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            if (run == null) throw new ArgumentNullException(nameof(run));

            foreach (var use in tokens.For(run).Uses)
            {
                if (!_colorByKey.ContainsKey(use.Key))
                    _colorByKey.Add(use.Key, use.Color);
            }
        }

        public bool TryGetColor(CensorKey key, out MemoryColor color) =>
            _colorByKey.TryGetValue(key, out color);
    }
}
