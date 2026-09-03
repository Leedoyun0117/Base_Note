using System;
using GameName.Core.Events;
using GameName.Core.Memories;

namespace GameName.Core.Dialogue
{
    // 기억색 1을 소모해 검열 키 하나를 푸는 처리기.
    //
    // 푸는 단위는 키다 — 그 키로 가려진 구간은 이 대사에 있든 다른 대사에 있든
    // 한 번의 해금으로 전부 원문으로 돌아온다. 그 "함께 풀림"은 렌더러가
    // 세그먼트마다 IsRevealed(key)를 보기 때문에 이 처리기가 따로 할 일이 없다.
    //
    // 멱등이다. 같은 키가 여러 줄에 걸쳐 있어 화면을 그릴 때마다 다시 풀려는
    // 시도가 올 수 있는데, 이미 풀린 키면 지갑을 건드리지 않고 성공으로 돌려준다.
    // 그래서 렌더링 경로에서 매 줄 확인해도 자원이 새지 않는다.
    public sealed class CensorUnlockProcessor
    {
        private readonly IMemoryColorWalletMutator _wallet;
        private readonly ICensorUnlockRecord _unlockRecord;
        private readonly ICensorKeyColorMap _keyColors;
        private readonly IEventBus _eventBus;

        public CensorUnlockProcessor(
            IMemoryColorWalletMutator wallet,
            ICensorUnlockRecord unlockRecord,
            ICensorKeyColorMap keyColors,
            IEventBus eventBus)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _unlockRecord = unlockRecord ?? throw new ArgumentNullException(nameof(unlockRecord));
            _keyColors = keyColors ?? throw new ArgumentNullException(nameof(keyColors));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public CensorUnlockResult Unlock(CensorKey key)
        {
            if (_unlockRecord.IsRevealed(key))
                return CensorUnlockResult.AlreadyRevealed();

            if (!_keyColors.TryGetColor(key, out var color))
                return CensorUnlockResult.Failure(CensorUnlockFailureReason.UnknownKey);

            if (_wallet.GetCount(color) <= 0)
                return CensorUnlockResult.Failure(CensorUnlockFailureReason.InsufficientMemory);

            _wallet.Remove(color, 1);
            _unlockRecord.Record(key);

            _eventBus.Publish(new CensorKeyUnlockedEvent(key, color));

            return CensorUnlockResult.Spent(color);
        }
    }
}
