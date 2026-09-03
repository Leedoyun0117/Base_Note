using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;

namespace GameName.UI.MemoryRoom.Space
{
    // 씬에 그려진 기억 방과 Core 사이를 잇는 단 하나의 지점.
    //
    // 씬 오브젝트는 Core 처리기를 전혀 참조하지 않는다 — 단서 오브젝트는 자기
    // 식별자만 들고 있고, 눌렸다는 사실만 알린다. 그 뒤의 판단(집을 수 있는가)은
    // 전부 여기서 Core에 물어 얻은 결론이다.
    //
    // 지금 어느 방을 그릴지는 RunProgressor가 정한다(RoomStartedEvent). 플레이어가
    // 방을 걸어서 오갈 수 없으므로 출입구 오브젝트도, 그래프 조회도 없다 — 방은
    // 대화 종료나 신뢰 0으로만 닫히고, 그때 다음 방의 RoomStartedEvent가 온다.
    //
    // 방이 다시 그려지는 계기는 전부 Refresh 하나로 모인다(방 진입, 단서 수집).
    // 계기마다 화면을 조금씩 다르게 손대기 시작하면 어느 경로에서는 이전 방의
    // 오브젝트가 남는 상태가 생기고, 그건 화면을 보고는 알아채기 어려운 종류의
    // 어긋남이다.
    public sealed class MemoryRoomSpaceController : IDisposable
    {
        private readonly MemoryRoomSpaceView _view;
        private readonly IMemoryRoomClueTracker _clueTracker;
        private readonly IClueStateReader _clueState;
        private readonly IClueAccessPolicy _accessPolicy;
        private readonly MemoryRoomLayout _layout;

        // 지금 방이 보이는 가로 비율. 신뢰가 깎이면 마스크 컨트롤러가 낮춰 준다.
        // 이 비율 밖의 단서는 회색 처리 + 콜라이더 비활성 — ClueCollectionProcessor의
        // 거부 사유(OutOfView)와 반드시 같은 판정(_accessPolicy)이어야 한다.
        private float _visibleRatio = 1f;

        private readonly IDisposable _clueCollectedSubscription;
        private readonly IDisposable _roomStartedSubscription;

        // 지금 방에 보이는 단서들의 공개 정보. 씬 오브젝트는 식별자만 알리므로,
        // 확대 화면에 넘길 정보를 여기서 되찾는다 — 씬 오브젝트가 ClueInfo를
        // 들고 있지 않아야 한다는 원칙을 지키면서도 화면이 필요한 것을 얻는
        // 방법이다.
        private readonly Dictionary<ClueId, ClueInfo> _visibleCluesById = new Dictionary<ClueId, ClueInfo>();

        private MemoryRoomId _activeRoomId;
        private bool _hasActiveRoom;

        // 단서를 눌렀다 — 확대 화면을 열 차례다. 확대 화면 자체는 이 컨트롤러가
        // 소유하지 않는다(씬과 UI Toolkit은 서로 다른 표시 수단이라, 둘을 잇는
        // 것은 한 단계 위인 화면 컨트롤러의 몫이다).
        public event Action<ClueInfo> ClueActivated;

        // 플레이어에게 보여줄 한 줄.
        public event Action<string> MessageChanged;

        public MemoryRoomSpaceController(
            MemoryRoomSpaceView view,
            MemoryRoomLayout layout,
            IMemoryRoomClueTracker clueTracker,
            IClueStateReader clueState,
            IClueAccessPolicy accessPolicy,
            MemoryRoomId initialRoomId,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _clueTracker = clueTracker ?? throw new ArgumentNullException(nameof(clueTracker));
            _clueState = clueState ?? throw new ArgumentNullException(nameof(clueState));
            _accessPolicy = accessPolicy ?? throw new ArgumentNullException(nameof(accessPolicy));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.Build(_layout);
            _view.ClueActivated += OnClueActivated;

            // 단서를 집으면 그 방에서 사라져야 한다(ClueState가 Available을 벗어난다).
            _clueCollectedSubscription = eventBus.Subscribe<ClueCollectedEvent>(_ => Refresh());
            // 이후의 방 전환은 RoomStartedEvent로 따라간다.
            _roomStartedSubscription = eventBus.Subscribe<RoomStartedEvent>(OnRoomStarted);

            // 첫 RoomStartedEvent는 GameSession 조립 중(이 화면이 만들어지기 전)에
            // 이미 발행되었으므로, 지금 진행 중인 방을 인자로 받아 곧바로 그린다.
            _activeRoomId = initialRoomId;
            _hasActiveRoom = true;
            Refresh();
        }

        // 확대 화면이 열려 있는 동안 방을 조작하지 못하게 막는다.
        public void SetInteractionEnabled(bool enabled) => _view.SetInteractionEnabled(enabled);

        // 마스크 컨트롤러가 신뢰 변화에 맞춰 넘겨 주는 가시 비율. 값이 실제로
        // 바뀔 때만 방을 다시 그린다 — 계단식 정책이라 신뢰가 움직여도 비율은
        // 그대로일 수 있다.
        public void SetVisibleRatio(float visibleRatio)
        {
            // 정책이 돌려주는 값은 표에 적힌 이산값(1.0, 0.75, 0.5…)이라 같은
            // 비율이면 비트까지 같다 — 근사 비교가 필요 없다.
            if (_visibleRatio == visibleRatio)
                return;

            _visibleRatio = visibleRatio;
            Refresh();
        }

        public void Refresh()
        {
            if (!_hasActiveRoom)
            {
                _visibleCluesById.Clear();
                _view.HideRoom();
                return;
            }

            _view.SetContents(BuildClueItems(_activeRoomId));
        }

        private void OnRoomStarted(RoomStartedEvent e)
        {
            _activeRoomId = e.RoomId;
            _hasActiveRoom = true;
            Refresh();
        }

        private IReadOnlyList<ClueSceneItem> BuildClueItems(MemoryRoomId roomId)
        {
            // 카탈로그는 그 방에 배치된 단서를 전부 내어준다 — 이미 집은 단서를
            // 거르는 것은 화면의 몫이다. "집었는가"의 유일한 진실은 ClueState다.
            _visibleCluesById.Clear();
            var items = new List<ClueSceneItem>();
            foreach (var info in _clueTracker.GetCluesInRoom(roomId))
            {
                if (_clueState.TryGetState(info.Id, out var state) && state != ClueState.Available)
                    continue;

                _visibleCluesById[info.Id] = info;
                items.Add(new ClueSceneItem(
                    info.Id, info.Kind,
                    CluePlacementLayout.PositionAt(_layout, info.Kind, info.AuthoredPosition),
                    // 집는 처리기와 같은 정책·같은 비율. 여기서 false면 그쪽도 OutOfView로 막는다.
                    _accessPolicy.IsAccessible(info.AuthoredPosition, _visibleRatio)));
            }

            return items;
        }

        private void OnClueActivated(ClueId clueId)
        {
            if (_visibleCluesById.TryGetValue(clueId, out var info))
            {
                ClueActivated?.Invoke(info);
                return;
            }

            // 여기 오면 씬에 남아 있는 오브젝트와 이 컨트롤러가 아는 목록이
            // 어긋난 것이다. 조용히 넘기면 "눌러도 아무 일이 없다"로만 보여서
            // 원인을 찾을 길이 없어진다.
            MessageChanged?.Invoke("이 단서는 더 이상 이 방에 없습니다.");
        }

        public void Dispose()
        {
            _view.ClueActivated -= OnClueActivated;

            _clueCollectedSubscription.Dispose();
            _roomStartedSubscription.Dispose();
        }
    }
}
