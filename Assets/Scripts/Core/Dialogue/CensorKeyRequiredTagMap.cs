using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;

namespace GameName.Core.Dialogue
{
    // RunDefinition에 실린 저작 목록(CensorKeyTagRequirement)을 키로 접어
    // ICensorKeyRequiredTags를 구현한다.
    //
    // 색 맵(CensorTokenIndexColorMap)과 달리 원문을 파싱해서 뽑아내는 값이
    // 아니라 이미 구조화된 목록이라 그대로 접기만 한다. 같은 키가 두 번
    // 적히는 저작 실수는 첫 번째 항목을 그 키의 값으로 삼는다 — 색 맵이
    // 첫 번째 색을 취하는 것과 같은 관례다.
    public sealed class CensorKeyRequiredTagMap : ICensorKeyRequiredTags
    {
        private readonly Dictionary<CensorKey, IReadOnlyList<ClueTag>> _tagsByKey =
            new Dictionary<CensorKey, IReadOnlyList<ClueTag>>();

        public CensorKeyRequiredTagMap(RunDefinition run)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));

            foreach (var requirement in run.CensorKeyTagRequirements)
            {
                if (!_tagsByKey.ContainsKey(requirement.Key))
                    _tagsByKey.Add(requirement.Key, requirement.RequiredTags);
            }
        }

        public bool TryGetRequiredTags(CensorKey key, out IReadOnlyList<ClueTag> requiredTags) =>
            _tagsByKey.TryGetValue(key, out requiredTags);
    }
}
