using System;
using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;
using GameName.UI.Shared;

namespace GameName.UI.MemoryRoom
{
    // 시향 패널의 입력 처리와 Core 연동을 담당한다.
    //
    // 인벤토리의 앰플 중 목표 방이 "지금 있는 방"과 같은 것만 활성화한다 —
    // 실제 차단은 여전히 ScentTestingProcessor.Test(WrongRoom)가 하고, 이
    // 컨트롤러의 판단(AmpouleEligibility)은 화면에 미리 보여주는 용도일 뿐이다.
    //
    // "이 방이 복원되었는가"는 시향 결과의 FeedbackStage로 스스로 재판정하지
    // 않는다 — Test()가 내부에서 이미 IMemoryRoomRestorationTracker에 보고했으므로,
    // 그 결과를 다시 조회(IsRestored)해서 보여준다. 복원 조건(최상위 단계)을
    // 이 화면에서 다시 알 필요가 없게 하기 위함이다.
    public sealed class ScentTestingPanelController : IDisposable
    {
        private readonly ScentTestingPanelView _view;
        private readonly IPlayerLocation _playerLocation;
        private readonly IPlayerInventory _inventory;
        private readonly ScentTestingProcessor _testingProcessor;
        private readonly IMemoryRoomRestorationTracker _restorationTracker;
        private readonly IReadOnlyList<MemoryRoomId> _roomIds;
        private readonly IDisposable _moveSubscription;

        private Ampoule _armedAmpoule;

        // 지금 이 방에서 실제로 시향할 수 있는 앰플을 들고 있는가. 상단 바가
        // 시향 버튼을 보여줄지 정하는 데 쓴다 — 눌러도 아무 것도 못 하는
        // 버튼은 "여기서 뭔가 할 수 있다"는 잘못된 신호가 된다.
        public bool IsTestPossible { get; private set; }

        public event Action<bool> AvailabilityChanged;

        // 시향에 성공하면 인벤토리에서 앰플이 사라진다 — 인벤토리 패널은 이
        // 화면의 다른 컨트롤러가 소유하므로, 화면 컨트롤러가 이 이벤트를 듣고
        // 인벤토리 패널에 새로고침을 지시한다.
        public event Action AmpouleTested;

        public ScentTestingPanelController(
            ScentTestingPanelView view,
            IPlayerLocation playerLocation,
            IPlayerInventory inventory,
            ScentTestingProcessor testingProcessor,
            IMemoryRoomRestorationTracker restorationTracker,
            IReadOnlyList<MemoryRoomId> roomIds,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _testingProcessor = testingProcessor ?? throw new ArgumentNullException(nameof(testingProcessor));
            _restorationTracker = restorationTracker ?? throw new ArgumentNullException(nameof(restorationTracker));
            _roomIds = roomIds ?? throw new ArgumentNullException(nameof(roomIds));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.TestRequested += OnTestRequested;
            _view.ConfirmTestRequested += OnConfirmTestRequested;
            _view.CancelTestRequested += OnCancelTestRequested;
            _view.CloseRequested += Close;
            _moveSubscription = eventBus.Subscribe<MemoryRoomMoveCompletedEvent>(_ => OnMoved());

            Refresh();
        }

        // 시향 화면은 항상 떠 있지 않는다 — 상단 바의 버튼으로 연다.
        public void Open() => _view.SetVisible(true);

        public void Close() => _view.SetVisible(false);

        public bool IsOpen => _view.IsVisible;

        private void OnMoved()
        {
            // 방을 옮기면 대기 중이던 확정 요청은 더 이상 의미가 없다(대상
            // 방과의 관계 자체가 바뀐다). 지난 방에서 받은 결과 문구도 마찬가지다
            // — 그대로 두면 새 방에서 방금 시향한 것처럼 읽힌다.
            _armedAmpoule = null;
            _view.ClearResult();
            Refresh();
        }

        private void OnTestRequested(Ampoule ampoule)
        {
            _armedAmpoule = ampoule;
            Refresh();
        }

        private void OnCancelTestRequested()
        {
            _armedAmpoule = null;
            Refresh();
        }

        private void OnConfirmTestRequested()
        {
            if (_armedAmpoule == null)
                return;

            var ampoule = _armedAmpoule;
            _armedAmpoule = null;

            var result = _testingProcessor.Test(ampoule);
            if (!result.Succeeded)
            {
                _view.SetResult(null, false);
                Refresh();
                return;
            }

            // TODO 사운드 재생 지점: result.Judgement.Stage에 맞는 효과음(무반응/
            // 피아노/피아노+바이올린/피아노+바이올린+드럼)을 여기서 재생한다.
            var isRestored = _restorationTracker.IsRestored(ampoule.TargetRoomId);
            _view.SetResult(result.Judgement.Stage, isRestored);

            Refresh();
            AmpouleTested?.Invoke();
        }

        private void Refresh()
        {
            var hasCurrentRoom = CurrentRoomResolver.TryResolve(_playerLocation.Current, _roomIds, out var currentRoomId);
            MemoryRoomId? currentRoomIdOrNull = hasCurrentRoom ? currentRoomId : (MemoryRoomId?)null;

            var rows = new List<AmpouleTestRowData>();
            foreach (var item in _inventory.Items)
            {
                if (!(item is Ampoule ampoule))
                    continue;

                var eligible = AmpouleEligibility.CanTestInCurrentRoom(ampoule.TargetRoomId, currentRoomIdOrNull);
                var isArmed = _armedAmpoule != null && _armedAmpoule.Equals(ampoule);
                rows.Add(new AmpouleTestRowData(ampoule, eligible, isArmed));
            }

            _view.SetAmpoules(rows);

            var isPossible = false;
            foreach (var row in rows)
            {
                if (!row.Eligible)
                    continue;

                isPossible = true;
                break;
            }

            if (isPossible == IsTestPossible)
                return;

            IsTestPossible = isPossible;
            AvailabilityChanged?.Invoke(isPossible);
        }

        public void Dispose()
        {
            _view.TestRequested -= OnTestRequested;
            _view.ConfirmTestRequested -= OnConfirmTestRequested;
            _view.CancelTestRequested -= OnCancelTestRequested;
            _view.CloseRequested -= Close;
            _moveSubscription.Dispose();
        }
    }
}
