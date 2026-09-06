using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.Memories;

namespace GameName.Core.Dialogue
{
    // 추출한 기억 하나를 제시해 검열 키 하나를 푸는 처리기.
    //
    // 푸는 단위는 키다 — 그 키로 가려진 구간은 이 대사에 있든 다른 대사에 있든
    // 한 번의 해금으로 전부 원문으로 돌아온다. 그 "함께 풀림"은 렌더러가
    // 세그먼트마다 IsRevealed(key)를 보기 때문에 이 처리기가 따로 할 일이 없다.
    //
    // 판정 기준은 색이 아니라 태그다: 제시한 기억의 태그가 이 키의 요구 태그와
    // 하나라도 겹쳐야 풀린다. 색이 같아도 태그가 다르면 실패한다 — 색은 추출
    // 순간 드러나는 힌트일 뿐, 정답을 정하지 않는다.
    //
    // 멱등이다. 같은 키가 여러 줄에 걸쳐 있어 화면을 그릴 때마다 다시 풀려는
    // 시도가 올 수 있는데, 이미 풀린 키면 기억을 건드리지 않고 성공으로 돌려준다.
    // 그래서 렌더링 경로에서 매 줄 확인해도 자원이 새지 않는다.
    public sealed class CensorUnlockProcessor
    {
        private readonly IExtractedMemoryStoreMutator _memories;
        private readonly ICensorUnlockRecord _unlockRecord;
        private readonly ICensorKeyRequiredTags _requiredTags;
        private readonly IEventBus _eventBus;

        public CensorUnlockProcessor(
            IExtractedMemoryStoreMutator memories,
            ICensorUnlockRecord unlockRecord,
            ICensorKeyRequiredTags requiredTags,
            IEventBus eventBus)
        {
            _memories = memories ?? throw new ArgumentNullException(nameof(memories));
            _unlockRecord = unlockRecord ?? throw new ArgumentNullException(nameof(unlockRecord));
            _requiredTags = requiredTags ?? throw new ArgumentNullException(nameof(requiredTags));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        // presentedMemoryClueId: 지금 제시하려는 추출된 기억의 출처 단서 id.
        // 화면(대화 패널)이 손에 든 기억 목록에서 플레이어가 고른 값을 그대로
        // 넘긴다 — 어떤 기억을 낼지 고르는 선택 자체가 이 판정 이전에 이미
        // 끝나 있다.
        public CensorUnlockResult Unlock(CensorKey key, ClueId presentedMemoryClueId)
        {
            if (_unlockRecord.IsRevealed(key))
                return CensorUnlockResult.AlreadyRevealed();

            if (!_requiredTags.TryGetRequiredTags(key, out var required))
                return CensorUnlockResult.Failure(CensorUnlockFailureReason.UnknownKey);

            if (!_memories.TryGet(presentedMemoryClueId, out var memory))
                return CensorUnlockResult.Failure(CensorUnlockFailureReason.MemoryNotFound);

            if (!HasAnyMatchingTag(memory.Tags, required))
                return CensorUnlockResult.Failure(CensorUnlockFailureReason.TagMismatch);

            _memories.Remove(presentedMemoryClueId);
            _unlockRecord.Record(key);

            _eventBus.Publish(new CensorKeyUnlockedEvent(key, memory.Color));

            return CensorUnlockResult.Spent(memory.Color);
        }

        private static bool HasAnyMatchingTag(IReadOnlyList<ClueTag> memoryTags, IReadOnlyList<ClueTag> requiredTags)
        {
            for (var i = 0; i < memoryTags.Count; i++)
            {
                for (var j = 0; j < requiredTags.Count; j++)
                {
                    if (memoryTags[i].Equals(requiredTags[j]))
                        return true;
                }
            }

            return false;
        }
    }
}
