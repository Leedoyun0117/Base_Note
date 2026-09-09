using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Extraction;
using GameName.Core.Hiromi;
using GameName.Core.Inventory;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using GameName.Core.Mind;
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
    // 대화를 다 본 뒤 "다음으로" 버튼을 눌러야 넘어간다(히로민 15를 치른다) —
    // 신뢰 0으로 방이 무너지면 다음 방 없이 그 자리에서 런이 끝난다.
    public sealed class GameSession
    {
        public IEventBus EventBus { get; }
        public IPlayerInventory Inventory { get; }
        public IMemoryRoomClueTracker ClueTracker { get; }

        // 단서를 집는 유일한 경로(옛 ClueCollector 자리). ClueState 전이를 소유한다.
        public ClueCollectionProcessor ClueCollectionProcessor { get; }
        public ExtractionProcessor ExtractionProcessor { get; }

        // 손에 든 단서를 이 런에서 완전히 버리는 유일한 경로. 단서가 방을 넘어
        // 누적되며 인벤토리 용량이 곧 상한이 됐고, 자리를 비우는 방법은 이것뿐이다.
        public ClueDiscardProcessor ClueDiscardProcessor { get; }

        // 방 화면이 "이 단서를 이미 집었는가"를 물어 방에서 지울 때 참조한다.
        public IClueStateReader ClueState { get; }

        public ITrustReader Trust { get; }

        // 나츠의 안정 축(침체 ~ 안정 ~ 흥분 연속값)과 심리 상태(낙관/광기/우울).
        // 피드백 대사가 안정 축을 움직이고, 그 위치가 유키의 인내심(Trust)을 깎는다.
        public IStabilityReader Stability { get; }
        public IPsychologyReader Psychology { get; }

        // 지금 손에 든 추출된 기억 — 검열 해금에 제시할 후보들. 옛
        // IMemoryColorWallet(색→개수) 자리를 대신한다.
        public IExtractedMemoryStore Memories { get; }

        // 대화·추출·기억 이동이 함께 오가는 런 전체 단일 자원. 옛
        // IExtractionBudget 자리를 대신한다 — 추출 전용 자원과 공존하지 않는다.
        public IHiromiReader Hiromi { get; }

        // 히로민이 모자란 채로 강제 이동할 때마다 줄고, 0이 되면 런이 끝난다.
        public IChanceReader Chance { get; }

        // 플레이어가 스스로 다음 기억으로 넘어가는 유일한 경로.
        public MemoryMoveProcessor MemoryMove { get; }

        // 다음 기억으로 이동하는 데 드는 히로민이자 "그냥 이동해도 되는가"의
        // 문턱. 화면이 이동 확인 팝업을 띄울지, HUD 게이지에 문턱을 어디에
        // 그릴지 판단하는 데 MemoryMove와 같은 값을 참조해야 하므로 그대로 노출한다.
        public int MoveHiromiCost { get; }

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
        // 추출된 기억 저장소·추출 자원과 같은 스코프라 방이 바뀌어도 리셋되지 않는다.
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

            // 랜덤 분기 풀을 런 시작 시 한 번 확정한다. 이 아래로는 고정 라인과
            // 뽑힌 라인이 섞인 하나의 대화 그래프만 흐른다 — 처리기들은 풀의
            // 존재를 모른다. 시드는 데이터에 실려 오므로 테스트에서 고정할 수 있다.
            var run = BranchResolver.Resolve(data.Run, data.Run.Seed);

            EventBus = new EventBus(new NoOpEventExceptionHandler());

            Inventory = new PlayerInventory(settings.InventorySettings, new SharedSlotInventoryPolicy());

            var clueTracker = new MemoryRoomClueTracker(data.CluePlacements);
            ClueTracker = clueTracker;

            RoomIds = data.RoomIds;

            // ── 2단계 규칙 스택 ─────────────────────────────────────────────
            var trust = new TrustGauge(run.StartingTrust, EventBus);
            Trust = trust;

            var memories = new ExtractedMemoryStore();
            Memories = memories;

            var hiromi = new HiromiWallet(run.StartingHiromi, EventBus);
            Hiromi = hiromi;

            var chance = new ChanceTracker(run.StartingChance, EventBus);
            Chance = chance;
            _ = new ChanceExhaustionListener(EventBus);
            _ = new HiromiDialogueEarningListener(hiromi, EventBus);

            var stability = new StabilityAxis(
                run.StartingStability, run.StabilityMin, run.StabilityMax, EventBus);
            Stability = stability;

            var psychology = new PsychologyTracker(run.StartingPsychology, EventBus);
            Psychology = psychology;

            // 안정 축이 중심에서 멀리 벗어나 있으면 답변마다 유키의 인내심이 깎인다.
            // (예전의 "오답이면 신뢰 -1"을 대체한다 — 이제 신뢰는 답의 질이 아니라
            //  나츠가 얼마나 불안정한 상태로 대화하는가에 반응한다.)
            _ = new StabilityTrustErosionListener(
                stability, trust, run.TrustErosionFreeBand, run.TrustErosionDivisor, EventBus);

            var clueState = new ClueStateStore(run.Rooms, EventBus);
            ClueState = clueState;

            var visibilityPolicy = new StepVisibilityPolicy(data.VisibilityByTrust);
            Visibility = visibilityPolicy;

            var clueAccess = new CenteredClueAccessPolicy();
            ClueAccess = clueAccess;

            ClueCollectionProcessor = new ClueCollectionProcessor(
                clueState, clueAccess, trust, visibilityPolicy, clueTracker, Inventory, EventBus);

            ExtractionProcessor = new ExtractionProcessor(hiromi, clueState, memories, clueTracker, EventBus);
            ClueDiscardProcessor = new ClueDiscardProcessor(clueState, EventBus);

            // 인벤토리는 ClueState의 투영 — 수집/추출/버리기 사건을 듣고 스스로 갱신한다.
            _ = new InventoryProjection(Inventory, clueTracker, EventBus);

            // 다음 방으로 넘어가면 손에 든 단서는 전부 두고 나온다 — 각 기억은
            // 그 안에서 완결되고, 앞 방 물건으로 나중 방에 답하는 흐름은 없다.
            _ = new RoomEntryInventoryClear(Inventory, ClueDiscardProcessor, EventBus);

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

            var censorKeyColors = new CensorTokenIndexColorMap(new CensorTokenIndexSource(parser), run);
            CensorKeyColors = censorKeyColors;

            var censorKeyRequiredTags = new CensorKeyRequiredTagMap(run);

            CensorUnlock = new CensorUnlockProcessor(memories, censorLog, censorKeyRequiredTags, EventBus);
            Dialogue = new DialogueProgressor(
                run.Rooms, censorLog, clueState, trust, new TagMatchGrader(), EventBus);

            // ── 방 진행 ────────────────────────────────────────────────────
            _ = new RoomCompletionArbiter(trust, EventBus);
            var runProgressor = new RunProgressor(run.Rooms, EventBus);
            MemoryMove = new MemoryMoveProcessor(run.MoveHiromiCost, hiromi, chance, runProgressor);
            // 방을 다 보고 "다음으로"를 누르면 그 이동에도 이동 비용(15)이 든다.
            _ = new RoomClearanceMoveListener(MemoryMove, EventBus);
            MoveHiromiCost = run.MoveHiromiCost;

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
