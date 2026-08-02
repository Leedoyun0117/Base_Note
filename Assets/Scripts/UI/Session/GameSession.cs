using System;
using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Journal;
using GameName.Core.Judging;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using GameName.Core.Validation;

namespace GameName.UI.Session
{
    // 화면과 무관하게 공유되는 Core 객체 그래프를 조립하는 유일한 구성 루트.
    //
    // 예전에는 PerfumeryBootstrap이 이 조립을 화면 하나 안에서 직접 했다 — 그
    // 상태로 기억 방 화면을 새로 만들면, 화면마다 EventBus/MentalityGauge/
    // Inventory 등을 따로 만들게 되어 두 화면이 "같은 플레이어"를 가리키지
    // 않는 두 개의 독립된 상태로 갈라진다. 그래서 이 조립을 화면 밖으로 뽑아
    // 여기 한 곳에서만 하고, 각 화면의 Bootstrap은 이미 만들어진 GameSession을
    // 참조로 받아 UI만 그 위에 얹는다.
    //
    // 이 타입도 게임 규칙은 계산하지 않는다 — 이미 있는 Core 타입들을 순서대로
    // 생성자에 밀어 넣어 서로 연결할 뿐이다.
    public sealed class GameSession : IDisposable
    {
        public IEventBus EventBus { get; }
        public IMentalityCostSettings MentalityCostSettings { get; }
        public IMentalityGauge MentalityGauge { get; }
        public IScentCompositionValidator CompositionValidator { get; }
        public IPlayerInventory Inventory { get; }
        public IAmpouleStorage AmpouleStorage { get; }
        public IMemoryRoomAnswerRepository AnswerRepository { get; }
        public IMemoryRoomPublicInfoRepository PublicInfoRepository { get; }
        public IMemoryRoomClueTracker ClueTracker { get; }
        public MemoryRoomGraph Graph { get; }
        public IMemoryRoomRestorationTracker RestorationTracker { get; }

        // 읽기 전용으로만 노출한다 — 위치를 실제로 바꿀 수 있는 것은
        // MovementProcessor 내부뿐이어야 한다는 규칙(IPlayerLocationMover의
        // 설계 의도)을 구성 루트 바깥에서도 지킨다.
        public IPlayerLocation PlayerLocation { get; }

        public MemoryRoomMovementProcessor MovementProcessor { get; }
        public ClueCollector ClueCollector { get; }
        public ScentTestingProcessor ScentTestingProcessor { get; }
        public AmpouleCraftingProcessor CraftingProcessor { get; }
        public AmpouleTransferProcessor TransferProcessor { get; }
        public PlayerJournal Journal { get; }

        public IReadOnlyList<MemoryRoomId> RoomIds { get; }
        public MemoryGraphNodeId PerfumeryRoomNodeId { get; }

        private readonly RoomRestorationRecoveryAdapter _recoveryAdapter;

        public GameSession(GameSessionData data, GameSessionSettings settings)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            EventBus = new EventBus(new NoOpEventExceptionHandler());
            MentalityCostSettings = settings.MentalityCostSettings;
            MentalityGauge = new MentalityGauge(MentalityCostSettings, EventBus);

            var judge = new ScentJudge(settings.JudgementSettings);
            CompositionValidator = new ScentCompositionValidator(settings.CompositionPolicy);

            Inventory = new PlayerInventory(settings.InventorySettings, new SharedSlotInventoryPolicy());
            AmpouleStorage = new AmpouleStorage(settings.StorageSettings);

            var answers = new List<MemoryRoomAnswer>(data.RoomData.Count);
            var clueDefinitions = new List<ClueDefinition>();
            var roomIds = new List<MemoryRoomId>(data.RoomData.Count);
            foreach (var roomData in data.RoomData)
            {
                answers.Add(roomData.Answer);
                roomIds.Add(roomData.Answer.RoomId);
                clueDefinitions.AddRange(roomData.Clues);
            }
            RoomIds = roomIds;

            var answerRepository = new MemoryRoomAnswerRepository(answers);
            AnswerRepository = answerRepository;
            PublicInfoRepository = answerRepository;

            ClueTracker = new MemoryRoomClueTracker(clueDefinitions);

            Graph = new MemoryRoomGraph(data.Nodes, data.OpenConnections, data.LadderConnections);
            RestorationTracker = new MemoryRoomRestorationTracker(EventBus);

            var playerLocation = new PlayerLocation(data.InitialPlayerPosition);
            PlayerLocation = playerLocation;

            MovementProcessor = new MemoryRoomMovementProcessor(
                Graph, RestorationTracker, MentalityGauge, MentalityCostSettings, playerLocation, EventBus);

            ClueCollector = new ClueCollector(playerLocation, Inventory, ClueTracker);

            ScentTestingProcessor = new ScentTestingProcessor(
                playerLocation, Inventory, judge, RestorationTracker, answerRepository, EventBus);

            PerfumeryRoomNodeId = data.PerfumeryRoomNodeId;

            CraftingProcessor = new AmpouleCraftingProcessor(
                playerLocation, PerfumeryRoomNodeId, MentalityGauge, MentalityCostSettings, CompositionValidator,
                answerRepository, AmpouleStorage, settings.AmpouleIdGenerator, EventBus);

            TransferProcessor = new AmpouleTransferProcessor(
                playerLocation, PerfumeryRoomNodeId, AmpouleStorage, Inventory);

            Journal = new PlayerJournal(EventBus, AmpouleStorage, Inventory);
            Journal.BeginCommission(data.InitialCommissionId);

            // 방이 완전히 복원되면 정신력이 자동으로 회복되는 규칙 — 예전
            // PerfumeryBootstrap에는 이 배선이 빠져 있었다(어댑터 타입 자체는
            // 진작 만들어져 테스트까지 있었지만 어디서도 생성되지 않았다).
            // 구성을 한 곳으로 모으는 김에 여기서 마저 연결한다.
            _recoveryAdapter = new RoomRestorationRecoveryAdapter(EventBus, MentalityGauge, MentalityCostSettings);
        }

        public void Dispose()
        {
            _recoveryAdapter.Dispose();
            Journal.Dispose();
        }
    }
}
