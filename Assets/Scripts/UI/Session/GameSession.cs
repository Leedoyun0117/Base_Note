using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Extraction;
using GameName.Core.Inventory;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using GameName.Core.Progression;
using GameName.Core.Restoration;
using GameName.Core.Trust;

namespace GameName.UI.Session
{
    // 화면과 무관하게 공유되는 Core 객체 그래프를 조립하는 유일한 구성 루트.
    //
    // 이 조립을 화면 밖으로 뽑아 여기 한 곳에서만 하고, 각 화면의 Bootstrap은
    // 이미 만들어진 GameSession을 참조로 받아 UI만 그 위에 얹는다 — 화면마다
    // EventBus/Inventory 등을 따로 만들면 두 화면이 "같은 플레이어"를 가리키지
    // 않는 두 개의 독립된 상태로 갈라지기 때문이다.
    //
    // 이 타입도 게임 규칙은 계산하지 않는다 — 이미 있는 Core 타입들을 순서대로
    // 생성자에 밀어 넣어 서로 연결할 뿐이다.
    //
    // 방 진행은 RunProgressor가 강제한다. 플레이어가 방을 걸어서 오갈 수는 없고,
    // 방이 닫히면(대화 종료 성공 또는 신뢰 0) RoomCompletionArbiter가 종료 사건을
    // 내고 RunProgressor가 그 자리에서 다음 방의 RoomStartedEvent를 발행한다.
    public sealed class GameSession
    {
        public IEventBus EventBus { get; }
        public IPlayerInventory Inventory { get; }
        public IMemoryRoomClueTracker ClueTracker { get; }

        // 단서를 집는 유일한 경로(옛 ClueCollector 자리). ClueState 전이를 소유한다.
        public ClueCollectionProcessor ClueCollectionProcessor { get; }
        public ExtractionProcessor ExtractionProcessor { get; }

        // 방 화면이 "이 단서를 이미 집었는가"를 물어 방에서 지울 때 참조한다.
        public IClueStateReader ClueState { get; }

        public ITrustReader Trust { get; }
        public IMemoryColorWallet Wallet { get; }
        public IExtractionBudget ExtractionBudget { get; }

        // 신뢰 → 방 가시 비율, 그 비율 안에 단서가 드는지. 화면(마스크·회색 처리)이
        // ClueCollectionProcessor와 똑같은 판정을 쓰도록 같은 인스턴스를 공유한다.
        public IVisibilityPolicy Visibility { get; }
        public IClueAccessPolicy ClueAccess { get; }

        // ── 대화·검열 스택 ────────────────────────────────────────────────
        // 대화 진행과 검열 해금. 화면은 이 표면만 구독/호출한다.
        public DialogueProgressor Dialogue { get; }
        public CensorUnlockProcessor CensorUnlock { get; }

        // 대화 패널이 원문을 조각으로 잘라 그릴 때, 그리고 조각이 지금 풀렸는지
        // 물을 때 쓴다. 검열 키 → 색 대응은 마스크 구간을 눌렀을 때 어느 색이
        // 필요한지 판단하는 데 쓴다.
        public ICensoredTextParser CensoredTextParser { get; }
        public ICensorResolver CensorResolver { get; }
        public ICensorKeyColorMap CensorKeyColors { get; }

        // 색별 추리 지원 마인드맵(복원도). 추출로 색이 드러날 때마다 뿌리와 단서
        // 노드가 자동으로 채워지고, 플레이어가 그 위에 자유 노드·연결을 얹는다.
        // 지갑·추출 자원과 같은 스코프라 방이 바뀌어도 리셋되지 않는다.
        //
        // 읽기와 편집을 갈라 노출한다. 편집은 판정이 거의 없는 CRUD라 처리기를
        // 두지 않았으므로(설계) 화면이 Mutator를 직접 부른다 — 그래도 진실은
        // 여전히 이벤트로만 흐른다(화면은 Core를 폴링하지 않는다).
        public IRestorationBoardReader RestorationBoard { get; }
        public IRestorationBoardMutator RestorationBoardEditor { get; }

        public IReadOnlyList<MemoryRoomId> RoomIds { get; }

        // 지금 진행 중인 방. RoomStartedEvent로 갱신된다. 씬 밖에서 "이 방을
        // 닫는다"를 알리려는 쪽(임시 개발용 진행 키 등)이 참조한다.
        public MemoryRoomId CurrentRoomId { get; private set; }

        public GameSession(GameSessionData data, GameSessionSettings settings)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            EventBus = new EventBus(new NoOpEventExceptionHandler());

            Inventory = new PlayerInventory(settings.InventorySettings, new SharedSlotInventoryPolicy());

            var clueTracker = new MemoryRoomClueTracker(data.CluePlacements);
            ClueTracker = clueTracker;

            RoomIds = data.RoomIds;

            // ── 2단계 규칙 스택 ─────────────────────────────────────────────
            var trust = new TrustGauge(data.Run.StartingTrust, EventBus);
            Trust = trust;

            var wallet = new MemoryColorWallet();
            foreach (var starting in data.StartingMemoryColors)
                wallet.Add(starting.Key, starting.Value);
            Wallet = wallet;

            var budget = new ExtractionBudget(data.Run.ExtractionBudget);
            ExtractionBudget = budget;

            var clueState = new ClueStateStore(data.Run.Rooms, EventBus);
            ClueState = clueState;

            var visibilityPolicy = new StepVisibilityPolicy(data.VisibilityByTrust);
            Visibility = visibilityPolicy;

            var clueAccess = new CenteredClueAccessPolicy();
            ClueAccess = clueAccess;

            ClueCollectionProcessor = new ClueCollectionProcessor(
                clueState, clueAccess, trust, visibilityPolicy, clueTracker, Inventory, EventBus);

            ExtractionProcessor = new ExtractionProcessor(budget, clueState, wallet, clueTracker, EventBus);

            // 인벤토리는 ClueState의 투영 — 수집/추출/방 시작 사건을 듣고 스스로 갱신한다.
            _ = new InventoryProjection(Inventory, clueTracker, EventBus);

            // 복원도는 런 전체에 걸쳐 산다 — RoomStartedEvent를 구독하지 않으므로
            // 방이 바뀌어도 내용이 유지된다. 리스너가 MemoryColorRevealedEvent를
            // 듣고 색 뿌리·단서 노드를 자동으로 채운다.
            var restorationBoard = new RestorationBoard(EventBus);
            RestorationBoard = restorationBoard;
            RestorationBoardEditor = restorationBoard;
            _ = new RestorationBoardExtractionListener(restorationBoard, clueTracker, EventBus);

            // ── 대화·검열 스택 ────────────────────────────────────────────
            // 검열 키 → 색 대응은 저작 원문을 한 번 훑어 만든다(방마다 다시 하지 않음).
            var censorLog = new CensorUnlockLog();
            CensorResolver = censorLog;

            var parser = new CensoredTextParser();
            CensoredTextParser = parser;

            var censorKeyColors = new CensorTokenIndexColorMap(new CensorTokenIndexSource(parser), data.Run);
            CensorKeyColors = censorKeyColors;

            CensorUnlock = new CensorUnlockProcessor(wallet, censorLog, censorKeyColors, EventBus);
            Dialogue = new DialogueProgressor(data.Run.Rooms, censorLog, clueState, trust, EventBus);

            // ── 방 진행 ────────────────────────────────────────────────────
            _ = new RoomCompletionArbiter(trust, EventBus);
            var runProgressor = new RunProgressor(data.Run.Rooms, EventBus);

            // 현재 방 추적. RunProgressor.Start()가 첫 RoomStartedEvent를 내기 전에 걸어 둔다.
            EventBus.Subscribe<RoomStartedEvent>(e => CurrentRoomId = e.RoomId);

            // 최적 선택 완주 시 콘솔에 "클리어"를 찍는 테스트용 관찰자.
            _ = new PerfectRunReporter(EventBus);

            // 모든 구독자 조립이 끝난 뒤에 첫 방으로 진입한다 — 신뢰 게이지·단서
            // 단계·인벤토리·방 화면이 전부 이 사건을 받아 초기화해야 하기 때문이다.
            runProgressor.Start();
        }
    }
}
