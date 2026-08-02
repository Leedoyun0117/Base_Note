using System;
using System.Collections.Generic;

namespace GameName.Core.Ampoules
{
    // IAmpouleStorage 기본 구현.
    //
    // PlayerInventory와 달리 슬롯 정책을 두지 않는다 — 이 저장소는 항상 앰플만
    // 담으므로(단서와 슬롯을 나눌 필요가 없다) 정책을 별도 타입으로 분리할
    // 이유가 없다. 상한 하나만 있는 단순한 목록이다.
    // IResettable도 함께 구현한다 — 보관함을 통째로 비우는 권한은 정상 동작
    // 인터페이스(IAmpouleStorage)가 아니라 오직 이 좁은 인터페이스로만 노출되고,
    // CommissionSession만 그 권한을 받는다.
    public sealed class AmpouleStorage : IAmpouleStorage, IResettable
    {
        private readonly List<Ampoule> _ampoules = new List<Ampoule>();

        public int Capacity { get; private set; }
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

        public void IncreaseCapacity(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "용량 증가분은 양수여야 한다.");

            Capacity += amount;
        }

        public void Reset() => _ampoules.Clear();
    }
}
