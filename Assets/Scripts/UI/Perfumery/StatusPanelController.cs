using System;
using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Mentality;

namespace GameName.UI.Perfumery
{
    // 우측 패널의 입력 처리와 Core 연동을 담당한다. 정신력/보관함/인벤토리
    // 상태를 표시하고, "인벤토리로 옮기기" 버튼 클릭을 받아 AmpouleTransferProcessor를
    // 호출한다. 옮길 수 있는지, 자리가 있는지는 이 타입이 판단하지 않는다 —
    // 처리기의 결과를 그대로 보여줄 뿐이다.
    public sealed class StatusPanelController : IDisposable
    {
        private readonly StatusPanelView _view;
        private readonly IMentalityGauge _mentalityGauge;
        private readonly IAmpouleStorage _storage;
        private readonly IPlayerInventory _inventory;
        private readonly AmpouleTransferProcessor _transferProcessor;
        private readonly IDisposable _mentalityChangedSubscription;
        private readonly IDisposable _ampouleCraftedSubscription;

        public StatusPanelController(
            StatusPanelView view,
            IMentalityGauge mentalityGauge,
            IAmpouleStorage storage,
            IPlayerInventory inventory,
            AmpouleTransferProcessor transferProcessor,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _mentalityGauge = mentalityGauge ?? throw new ArgumentNullException(nameof(mentalityGauge));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _transferProcessor = transferProcessor ?? throw new ArgumentNullException(nameof(transferProcessor));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.MoveToInventoryRequested += OnMoveToInventoryRequested;
            _mentalityChangedSubscription = eventBus.Subscribe<MentalityChangedEvent>(_ => RefreshMentality());
            _ampouleCraftedSubscription = eventBus.Subscribe<AmpouleCraftedEvent>(_ => RefreshStorage());

            RefreshMentality();
            RefreshStorage();
            RefreshInventory();
        }

        private void OnMoveToInventoryRequested(Ampoule ampoule)
        {
            var result = _transferProcessor.MoveToInventory(ampoule);
            _view.SetTransferFailureMessage(result.Succeeded ? null : DescribeTransferFailure(result.FailureReason.Value));

            RefreshStorage();
            RefreshInventory();
        }

        private void RefreshMentality() =>
            _view.SetMentality(_mentalityGauge.CurrentValue, _mentalityGauge.MaxValue);

        private void RefreshStorage() => _view.SetStorage(_storage.Ampoules, _storage.Capacity);

        private void RefreshInventory()
        {
            var ampoules = new List<Ampoule>();
            foreach (var item in _inventory.Items)
            {
                if (item is Ampoule ampoule)
                    ampoules.Add(ampoule);
            }

            _view.SetInventoryAmpoules(ampoules, _inventory.Capacity);
        }

        private static string DescribeTransferFailure(AmpouleTransferFailureReason reason)
        {
            switch (reason)
            {
                case AmpouleTransferFailureReason.NotInPerfumeryRoom:
                    return "조향실에서만 옮길 수 있습니다.";
                case AmpouleTransferFailureReason.AmpouleNotFound:
                    return "이미 옮겨졌거나 찾을 수 없는 앰플입니다.";
                case AmpouleTransferFailureReason.DestinationFull:
                    return "옮길 곳에 자리가 없습니다.";
                default:
                    return "옮기기에 실패했습니다.";
            }
        }

        public void Dispose()
        {
            _view.MoveToInventoryRequested -= OnMoveToInventoryRequested;
            _mentalityChangedSubscription.Dispose();
            _ampouleCraftedSubscription.Dispose();
        }
    }
}
