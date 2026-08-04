using System;
using System.Collections.Generic;
using GameName.Core;
using GameName.Core.Ampoules;
using GameName.Core.Clues;
using GameName.Core.Commissions;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.FinalCrafting;
using GameName.Core.Inventory;
using GameName.Core.Journal;
using GameName.Core.Judging;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using GameName.Core.Rewards;
using GameName.Core.Upgrades;
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
    // 생성자에 밀어 넣어 서로 연결할 뿐이다. 의뢰 데이터 교체(LoadCommission)도
    // 같은 이유로 여기서만 조율한다 — "누가 무엇을 갈아 끼우는 권한을 갖는가"를
    // 한 곳에 모아야 화면 쪽 코드가 실수로 그 권한을 얻을 길이 없어진다.
    public sealed class GameSession : IDisposable
    {
        public IEventBus EventBus { get; }
        public IMentalityCostSettings MentalityCostSettings { get; }
        public IMentalityGauge MentalityGauge { get; }
        public IScentCompositionValidator CompositionValidator { get; }
        public IPlayerInventory Inventory { get; }
        public IAmpouleStorage AmpouleStorage { get; }
        public IAmpouleCraftingQueue AmpouleCraftingQueue { get; }
        public IMemoryRoomAnswerRepository AnswerRepository { get; }
        public IMemoryRoomPublicInfoRepository PublicInfoRepository { get; }
        public IMemoryRoomClueTracker ClueTracker { get; }
        public IClueStorage ClueStorage { get; }
        public IMemoryRoomGraph Graph { get; }
        public IMemoryRoomRestorationTracker RestorationTracker { get; }

        // 읽기 전용으로만 노출한다 — 위치를 실제로 바꿀 수 있는 것은
        // MovementProcessor와 CommissionSession(기억 진입 시 시작 지점으로
        // 옮길 때) 내부뿐이어야 한다는 규칙(IPlayerLocationMover의 설계 의도)을
        // 구성 루트 바깥에서도 지킨다.
        public IPlayerLocation PlayerLocation { get; }

        public MemoryRoomMovementProcessor MovementProcessor { get; }
        public ClueCollector ClueCollector { get; }
        public ScentTestingProcessor ScentTestingProcessor { get; }
        public AmpouleCraftingProcessor CraftingProcessor { get; }
        public AmpouleTransferProcessor TransferProcessor { get; }
        public ClueTransferProcessor ClueTransferProcessor { get; }
        public ClueReturnProcessor ClueReturnProcessor { get; }
        public ClueAnalyzer ClueAnalyzer { get; }
        public IClueAnalysisProgress AnalysisProgress { get; }
        public IDialogueProgressor DialogueProgressor { get; }
        public PlayerJournal Journal { get; }
        public CommissionSession CommissionSession { get; }

        public IFinalCraftingBoard FinalCraftingBoard { get; }
        public FinalCraftingProcessor FinalCraftingProcessor { get; }
        public DisplayCollection DisplayCollection { get; }
        public CommissionCompletionProcessor CommissionCompletionProcessor { get; }
        public UpgradeShop UpgradeShop { get; }

        public IReadOnlyList<MemoryRoomId> RoomIds { get; private set; }
        public MemoryGraphNodeId PerfumeryRoomNodeId { get; }
        public MemoryGraphNodeId AnalysisRoomNodeId { get; }
        public MemoryGraphNodeId MemoryEntryNodeId { get; }
        public MemoryGraphNodeId MemoryExitNodeId { get; }

        // 지금 진행 중인 의뢰의 원본 데이터 — 보상 등급표처럼 "의뢰마다 다른
        // 값 하나"가 필요한 화면(완료 화면)이 매번 GameSessionData 전체를
        // 뒤지지 않고 여기서 바로 얻게 한다.
        public CommissionData CurrentCommissionData { get; private set; }

        // 데모에서 "다음 의뢰로" 진행할 때 순환할 전체 의뢰 목록. 실제 콘텐츠
        // 소스가 생기면 이 목록의 출처만 바뀐다.
        public IReadOnlyList<CommissionData> AllCommissions { get; }

        // 마지막으로 제공(완료)한 의뢰의 결과 — 완료 화면이 결과/보상을 보여줄
        // 시점에는 이미 CommissionCompletionProcessor.Complete 호출이 끝나
        // 있으므로, 그 결과를 화면이 다시 얻을 수 있도록 여기 보관해 둔다.
        public CommissionCompletionResult LastCompletionResult { get; private set; }

        public void RecordCompletionResult(CommissionCompletionResult result) => LastCompletionResult = result;

        private readonly IReadOnlyList<MemoryGraphNode> _hubNodes;
        private readonly IReadOnlyList<OpenConnection> _hubOpenConnections;
        private readonly MemoryRoomGraph _graph;
        private readonly MemoryRoomAnswerRepository _answerRepository;
        private readonly MemoryRoomClueTracker _clueTracker;
        private readonly DialogueProgressor _dialogueProgressor;
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
            AmpouleCraftingQueue = new AmpouleCraftingQueue();

            // 그래프/정답/단서는 전부 빈 상태로 만든다 — 실제 내용은 아래
            // LoadCommission(첫 의뢰)이 채운다. 생성자 시점에 값을 채워 넣지
            // 않는 이유는 그 절차를 의뢰 전환과 완전히 같은 경로(Load)로
            // 통일하기 위해서다 — "처음 시작"과 "의뢰가 바뀜"을 서로 다른
            // 코드로 두면 둘이 어긋날 위험이 생긴다.
            _hubNodes = data.HubNodes;
            _hubOpenConnections = data.HubOpenConnections;
            AllCommissions = data.Commissions;

            _graph = new MemoryRoomGraph(_hubNodes, _hubOpenConnections, Array.Empty<LadderConnection>());
            Graph = _graph;

            _answerRepository = new MemoryRoomAnswerRepository(Array.Empty<MemoryRoomAnswer>());
            AnswerRepository = _answerRepository;
            PublicInfoRepository = _answerRepository;

            _clueTracker = new MemoryRoomClueTracker(Array.Empty<ClueDefinition>());
            ClueTracker = _clueTracker;

            ClueStorage = new ClueStorage(settings.ClueStorageSettings);

            RestorationTracker = new MemoryRoomRestorationTracker(EventBus);

            MemoryEntryNodeId = data.MemoryEntryNodeId;
            MemoryExitNodeId = data.MemoryExitNodeId;
            var playerLocation = new PlayerLocation(MemoryEntryNodeId);
            PlayerLocation = playerLocation;

            MovementProcessor = new MemoryRoomMovementProcessor(
                _graph, RestorationTracker, MentalityGauge, MentalityCostSettings, playerLocation, EventBus);

            ClueCollector = new ClueCollector(playerLocation, Inventory, _clueTracker);

            ScentTestingProcessor = new ScentTestingProcessor(
                playerLocation, Inventory, judge, RestorationTracker, _answerRepository, EventBus);

            PerfumeryRoomNodeId = data.PerfumeryRoomNodeId;

            CraftingProcessor = new AmpouleCraftingProcessor(
                playerLocation, PerfumeryRoomNodeId, MentalityGauge, MentalityCostSettings, CompositionValidator,
                _answerRepository, AmpouleStorage, settings.AmpouleIdGenerator, EventBus);

            TransferProcessor = new AmpouleTransferProcessor(
                playerLocation, PerfumeryRoomNodeId, AmpouleStorage, Inventory);

            AnalysisRoomNodeId = data.AnalysisRoomNodeId;
            AnalysisProgress = new ClueAnalysisProgress();
            ClueAnalyzer = new ClueAnalyzer(
                playerLocation, AnalysisRoomNodeId, MentalityGauge, MentalityCostSettings, EventBus,
                AnalysisProgress, _clueTracker, Inventory, ClueStorage);

            ClueTransferProcessor = new ClueTransferProcessor(
                playerLocation, AnalysisRoomNodeId, ClueStorage, Inventory);

            ClueReturnProcessor = new ClueReturnProcessor(playerLocation, Inventory, _clueTracker, EventBus);

            Journal = new PlayerJournal(EventBus, AmpouleStorage, Inventory);

            // 첫 의뢰의 대본으로 만들어 두지만 아직 이벤트는 나가지 않는다
            // (생성자는 이벤트를 발행하지 않는다) — 실제 첫 줄 이벤트는 아래
            // LoadCommission이 journal.BeginCommission 이후 LoadScript를 부를
            // 때 한 번만 나간다.
            _dialogueProgressor = new DialogueProgressor(data.Commissions[0].DialogueScript, EventBus);
            DialogueProgressor = _dialogueProgressor;

            FinalCraftingBoard = new FinalCraftingBoard();
            FinalCraftingProcessor = new FinalCraftingProcessor(
                CompositionValidator, _answerRepository, (IFinalCraftingBoardWriter)FinalCraftingBoard);

            DisplayCollection = new DisplayCollection();

            // 새 의뢰가 시작될 때 초기화되어야 하는 플레이 상태 전부를 여기
            // 한 목록으로 모은다. 단서 정의/그래프/정답/대화 대본은 여기 없다 —
            // 그것들은 "초기화"가 아니라 "통째로 교체"가 필요하므로 각자의
            // Load 메서드로 LoadCommission이 직접 호출한다. 진열
            // (DisplayCollection)과 업그레이드 상점도 여기 없다 — 여러 의뢰에
            // 걸쳐 유지되어야 하는 영구 자산이기 때문이다.
            var resettableSystems = new List<IResettable>
            {
                (IResettable)MentalityGauge,
                (IResettable)Inventory,
                (IResettable)AmpouleStorage,
                (IResettable)RestorationTracker,
                (IResettable)AnalysisProgress,
                (IResettable)AmpouleCraftingQueue,
                (IResettable)FinalCraftingBoard,
                (IResettable)ClueStorage,
                MovementProcessor,
            };

            CommissionSession = new CommissionSession(
                EventBus, resettableSystems, Journal, DialogueProgressor, playerLocation, MemoryEntryNodeId);

            CommissionCompletionProcessor = new CommissionCompletionProcessor(
                FinalCraftingBoard, _answerRepository, judge, DisplayCollection, CommissionSession, EventBus);

            // CommissionCompletionProcessor가 완료 직전에 발행하는 결과를 여기서
            // 받아 둔다 — 반드시 CommissionSession.TryComplete()가 발행하는
            // CommissionStageChangedEvent보다 먼저 발행되도록 그 처리기 내부에서
            // 순서를 보장하므로, 완료 화면으로 전환되는 시점에는 이미
            // LastCompletionResult가 채워져 있다.
            EventBus.Subscribe<CommissionCompletedEvent>(e => RecordCompletionResult(e.Result));

            UpgradeShop = new UpgradeShop(
                settings.UpgradeCatalog, DisplayCollection, Inventory, AmpouleStorage, settings.MentalityCostAdjuster);

            // 방이 완전히 복원되면 정신력이 자동으로 회복되는 규칙 — 예전
            // PerfumeryBootstrap에는 이 배선이 빠져 있었다(어댑터 타입 자체는
            // 진작 만들어져 테스트까지 있었지만 어디서도 생성되지 않았다).
            // 구성을 한 곳으로 모으는 김에 여기서 마저 연결한다.
            _recoveryAdapter = new RoomRestorationRecoveryAdapter(EventBus, MentalityGauge, MentalityCostSettings);

            LoadCommission(data.Commissions[0]);
        }

        // 지금 의뢰를 완전히 다른 의뢰로 교체한다 — 방 그래프, 방별 정답,
        // 단서, 대화 대본을 전부 갈아 끼운 뒤 새 의뢰를 시작한다.
        //
        // 순서가 중요하다:
        //   1) 그래프/정답/단서를 먼저 교체한다 — 화면이 새 의뢰 단계로
        //      전환되기 전에 조회할 데이터가 이미 새 것이어야 한다.
        //   2) CommissionSession.Begin()으로 플레이 상태를 초기화하고 기록지
        //      구간을 연다.
        //   3) 대화 대본은 반드시 그 다음에 교체한다 — LoadScript가 발행하는
        //      첫 줄의 DialogueLineShownEvent가 새 의뢰의 기록 구간에 들어가야
        //      하기 때문이다. FlowOverlay는 상시 컴포넌트라 DialogueProgressor
        //      객체 자체를 새로 만들 수 없으므로 내부 대본만 바꿔치기한다.
        public void LoadCommission(CommissionData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var allNodes = new List<MemoryGraphNode>(_hubNodes.Count + data.RoomNodes.Count);
            allNodes.AddRange(_hubNodes);
            allNodes.AddRange(data.RoomNodes);

            var allOpenConnections = new List<OpenConnection>(_hubOpenConnections.Count + data.OpenConnections.Count);
            allOpenConnections.AddRange(_hubOpenConnections);
            allOpenConnections.AddRange(data.OpenConnections);

            _graph.Load(allNodes, allOpenConnections, data.LadderConnections);

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

            _answerRepository.Load(answers);
            _clueTracker.Load(clueDefinitions);

            CurrentCommissionData = data;
            CommissionSession.Begin(data.Id);

            _dialogueProgressor.LoadScript(data.DialogueScript);
        }

        public void Dispose()
        {
            _recoveryAdapter.Dispose();
            Journal.Dispose();
        }
    }
}
