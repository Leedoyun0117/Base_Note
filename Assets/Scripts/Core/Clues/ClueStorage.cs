using System;
using System.Collections.Generic;

namespace GameName.Core.Clues
{
    // IClueStorage 기본 구현.
    //
    // AmpouleStorage와 자리는 대칭이지만(단순한 상한 하나짜리 목록) 별도
    // 타입으로 둔다 — 담는 대상의 규칙이 달라지면(예: 단서만 갖는 만료/열화
    // 같은 미래 규칙) 억지로 공유한 추상화가 발목을 잡기 때문이다.
    //
    // IResettable도 함께 구현한다 — 보관대를 통째로 비우는 권한은 정상 조회
    // 인터페이스(IClueStorage)가 아니라 오직 이 좁은 인터페이스로만 노출되고,
    // CommissionSession만 그 권한을 받는다.
    public sealed class ClueStorage : IClueStorage, IResettable
    {
        private readonly List<ClueInfo> _clues = new List<ClueInfo>();

        public int Capacity { get; }
        public IReadOnlyList<ClueInfo> Clues => _clues;

        public ClueStorage(IClueStorageSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            Capacity = settings.MaxStoredClues;
        }

        public ClueStorageStoreResult TryStore(ClueInfo clue)
        {
            if (clue == null) throw new ArgumentNullException(nameof(clue));

            if (_clues.Contains(clue))
                return ClueStorageStoreResult.Failure(ClueStorageStoreFailureReason.Duplicate);

            if (_clues.Count >= Capacity)
                return ClueStorageStoreResult.Failure(ClueStorageStoreFailureReason.Full);

            _clues.Add(clue);
            return ClueStorageStoreResult.Success();
        }

        public ClueStorageRemoveResult TryRemove(ClueInfo clue)
        {
            if (clue == null) throw new ArgumentNullException(nameof(clue));

            return _clues.Remove(clue)
                ? ClueStorageRemoveResult.Success()
                : ClueStorageRemoveResult.Failure(ClueStorageRemoveFailureReason.NotFound);
        }

        public bool CanAccept(int additionalCount) => _clues.Count + additionalCount <= Capacity;

        public bool Contains(ClueId clueId)
        {
            foreach (var clue in _clues)
            {
                if (clue.Id.Equals(clueId))
                    return true;
            }

            return false;
        }

        public void Reset() => _clues.Clear();
    }
}
