using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Complexes;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.Core.Mind;
using GameName.Core.Progression;

namespace GameName.UI.Session
{
    // 화면과 무관하게 공유되는 Core 객체 그래프를 조립하는 유일한 구성 루트.
    //
    // 이 조립을 화면 밖으로 뽑아 여기 한 곳에서만 하고, 각 화면의 Bootstrap은
    // 이미 만들어진 GameSession을 참조로 받아 UI만 그 위에 얹는다.
    //
    // 이 타입도 게임 규칙은 계산하지 않는다 — 이미 있는 Core 타입들을 순서대로
    // 생성자에 밀어 넣어 서로 연결할 뿐이다.
    //
    // 3차 개편 루프: 단서를 클릭하면 그 태그가 활성 컴플렉스 체인을 통과해
    // 최종 태그가 나오고, 그 최종 태그의 감정이 안정 축을 민다. 그 한 번의
    // 클릭이 한 턴을 쓴다. 지정된 턴 수를 버티면 라운드 클리어. 안정 축이
    // 극단에 가까울수록 매 턴 새 컴플렉스가 붙을 확률이 오른다.
    public sealed class GameSession
    {
        public IEventBus EventBus { get; }
        public IMemoryRoomClueTracker ClueTracker { get; }

        // 단서 단계(Available/Used)를 읽는다 — 방 화면이 이미 읽은 단서를 지울 때 참조.
        public IClueStateReader ClueState { get; }

        // 단서를 클릭해 읽는 유일한 경로. 이 한 번이 한 턴을 쓴다.
        public ClueUseProcessor ClueUse { get; }

        // 나츠의 안정 축(침체 ~ 안정 ~ 흥분). 최종 태그의 감정이 이 축을 움직이고,
        // 축 위치가 컴플렉스 발생 확률을 정한다.
        public IStabilityReader Stability { get; }

        // 지금 라운드의 턴 진행 상황.
        public ITurnReader Turns { get; }

        // 지금 활성인 컴플렉스들(우선순위 순). 해석 로그·HUD가 읽는다.
        public IActiveComplexListReader ActiveComplexes { get; }

        public IReadOnlyList<MemoryRoomId> RoomIds { get; }

        // 지금 진행 중인 라운드. RoomStartedEvent로 갱신된다.
        public MemoryRoomId CurrentRoomId { get; private set; }

        public GameSession(GameSessionData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var run = data.Run;

            EventBus = new EventBus(new NoOpEventExceptionHandler());

            var clueTracker = new MemoryRoomClueTracker(data.CluePlacements);
            ClueTracker = clueTracker;
            RoomIds = data.RoomIds;

            var stability = new StabilityAxis(
                run.StartingStability, run.StabilityMin, run.StabilityMax, EventBus);
            Stability = stability;

            var clueState = new ClueStateStore(run.Rooms, EventBus);
            ClueState = clueState;

            // ── 컴플렉스 파이프라인 ────────────────────────────────────────
            var catalog = new ComplexCatalog(data.Complexes);

            // 활성 목록은 TurnAdvancedEvent를 스스로 듣고 지속 턴을 줄인다 —
            // 발생 리스너보다 먼저 만들어야 매 턴 "감소 → 발생" 순서가 된다.
            var activeComplexes = new ActiveComplexList(maxConcurrent: 4, EventBus);
            ActiveComplexes = activeComplexes;

            var turns = new TurnCoordinator(run.Rooms, EventBus);
            Turns = turns;

            var resolver = new ComplexChainResolver();

            // 최종 태그의 감정 축 → 안정 축 이동.
            _ = new ClueInterpretationStabilityListener(stability, data.EmotionShiftByValue, EventBus);

            // 매 턴, 안정 축 위치에 따른 확률로 발생 풀에서 컴플렉스 하나를 뽑아 붙인다.
            var spawnPolicy = new StepComplexSpawnPolicy(data.SpawnChanceByStabilityDistance);
            var drawSource = new CatalogComplexDrawSource(
                catalog, activeComplexes, run.Rooms, run.Seed, EventBus);
            _ = new ComplexSpawnListener(
                spawnPolicy, stability, activeComplexes, drawSource, run.Seed, EventBus);

            // 라운드가 시작되면 활성 목록을 비우고 그 라운드의 시작 컴플렉스를 건다.
            _ = new RoundSetupListener(run.Rooms, activeComplexes, catalog, EventBus);

            ClueUse = new ClueUseProcessor(
                clueState, clueTracker, resolver, activeComplexes, turns, EventBus);

            // ── 라운드 진행 ───────────────────────────────────────────────
            var runProgressor = new RunProgressor(run.Rooms, EventBus);

            EventBus.Subscribe<RoomStartedEvent>(e => CurrentRoomId = e.RoomId);

            // 모든 구독자 조립이 끝난 뒤에 첫 라운드로 진입한다.
            runProgressor.Start();
        }
    }
}
