using System;
using System.Collections.Generic;

namespace GameName.Core.Ampoules
{
    // IAmpouleStorage 기본 구현.
    //
    // PlayerInventory와 달리 슬롯 정책을 두지 않는다 — 이 저장소는 항상 앰플만
    // 담으므로(단서와 슬롯을 나눌 필요가 없다) 정책을 별도 타입으로 분리할
    // 이유가 없다. 상한 하나만 있는 단순한 목록이다.
    public sealed class AmpouleStorage : IAmpouleStorage
    {
        private readonly List<Ampoule> _ampoules = new List<Ampoule>();

        public int Capacity { get; }
        public IReadOnlyList<Ampoule> Ampoules => _ampoules;

        public AmpouleStorage(IAmpouleStorageSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            Capacity = settings.MaxStoredAmpoules;
        }

        public AmpouleStorageStoreResult TryStore(Ampoule ampoule)
        {
            if (ampoule == null) throw new ArgumentNullException(nameof(ampoule));

            if (_ampoules.Contains(ampoule))
                return AmpouleStorageStoreResult.Failure(AmpouleStorageStoreFailureReason.Duplicate);

            if (_ampoules.Count >= Capacity)
                return AmpouleStorageStoreResult.Failure(AmpouleStorageStoreFailureReason.Full);

            _ampoules.Add(ampoule);
            return AmpouleStorageStoreResult.Success();
        }

        public AmpouleStorageRemoveResult TryRemove(Ampoule ampoule)
        {
            if (ampoule == null) throw new ArgumentNullException(nameof(ampoule));

            return _ampoules.Remove(ampoule)
                ? AmpouleStorageRemoveResult.Success()
                : AmpouleStorageRemoveResult.Failure(AmpouleStorageRemoveFailureReason.NotFound);
        }

        public bool CanAccept(int additionalCount) => _ampoules.Count + additionalCount <= Capacity;
    }
}
