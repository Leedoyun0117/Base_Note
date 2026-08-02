using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Clues;
using GameName.Core.Emotions;
using GameName.Core.Inventory;
using GameName.Core.Journal;
using GameName.Core.Judging;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using GameName.Core.Validation;

namespace GameName.UI.Session
{
    // 실제 기획 데이터(레벨 에디터 산출물, 세이브 데이터 등)가 생기기 전까지
    // 쓰는 더미 데이터. 그래프 구조: 허브 삼각형(계단-분석실-조향실) + 계단에서
    // 기억 방 1로 들어가는 문 + 방1-방2, 방2-방3을 잇는 사다리(각각 아래 방이
    // 복원되어야 열림). 방 3개에는 정답과 단서 몇 개씩을 채워 둔다.
    //
    // 이 파일 하나만 실제 데이터 소스로 교체하면 GameSession 이하 나머지 코드는
    // 전혀 바뀌지 않는다 — 그 지점을 명확히 하기 위해 더미 데이터 구성을 전부
    // 여기 한 파일에 모아 둔다.
    internal static class DemoGameData
    {
        public static readonly MemoryGraphNodeId Staircase = new MemoryGraphNodeId("staircase");
        public static readonly MemoryGraphNodeId AnalysisRoom = new MemoryGraphNodeId("analysis-room");
        public static readonly MemoryGraphNodeId PerfumeryRoom = new MemoryGraphNodeId("perfumery-room");
        public static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");
        public static readonly MemoryRoomId Room2 = new MemoryRoomId("room-2");
        public static readonly MemoryRoomId Room3 = new MemoryRoomId("room-3");

        public static GameSessionData CreateWorldData()
        {
            var nodes = new List<MemoryGraphNode>
            {
                new MemoryGraphNode(Staircase, MemoryGraphNodeType.Staircase),
                new MemoryGraphNode(AnalysisRoom, MemoryGraphNodeType.AnalysisRoom),
                new MemoryGraphNode(PerfumeryRoom, MemoryGraphNodeType.PerfumeryRoom),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room1), MemoryGraphNodeType.MemoryRoom),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room2), MemoryGraphNodeType.MemoryRoom),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room3), MemoryGraphNodeType.MemoryRoom),
            };

            var openConnections = new List<OpenConnection>
            {
                new OpenConnection(Staircase, AnalysisRoom),
                new OpenConnection(Staircase, PerfumeryRoom),
                new OpenConnection(AnalysisRoom, PerfumeryRoom),
                new OpenConnection(Staircase, MemoryGraphNodeId.OfRoom(Room1)),
            };

            var ladderConnections = new List<LadderConnection>
            {
                new LadderConnection(upperRoom: Room2, lowerRoom: Room1),
                new LadderConnection(upperRoom: Room3, lowerRoom: Room2),
            };

            var roomData = new List<MemoryRoomData>
            {
                CreateRoom1(),
                CreateRoom2(),
                CreateRoom3(),
            };

            return new GameSessionData(
                nodes,
                openConnections,
                ladderConnections,
                roomData,
                initialPlayerPosition: MemoryGraphNodeId.OfRoom(Room1),
                perfumeryRoomNodeId: PerfumeryRoom,
                initialCommissionId: new CommissionId("demo-commission"));
        }

        public static GameSessionSettings CreateSettings()
        {
            var mentalityCostSettings = new MentalityCostSettings(
                initialMentality: 100, maxMentality: 100,
                memoryRoomMoveCost: 1, basicAnalysisCost: 20, advancedAnalysisCost: 30,
                ampouleCraftingCost: 8, memoryRoomFullRestorationRecovery: 20);

            var judgementSettings = new ScentJudgementSettings(highAccuracyThreshold: 0.8);

            var compositionPolicy = new EmotionCompositionPolicy(
                minSupportingEmotionCount: 2, maxSupportingEmotionCount: 4, allowSupportingEmotionSameAsBase: false);

            var inventorySettings = new InventorySettings(initialCapacity: 4);
            var storageSettings = new AmpouleStorageSettings(maxStoredAmpoules: 3);

            return new GameSessionSettings(
                mentalityCostSettings,
                judgementSettings,
                compositionPolicy,
                inventorySettings,
                storageSettings,
                new GuidAmpouleIdGenerator());
        }

        // GameSession 자체의 책임이 아니라 "이 데모를 어떤 상태로 시작할지"에
        // 대한 더미 연출이라 여기 따로 둔다. 방 3을 처음부터 복원된 상태로
        // 시작해, 사다리 잠금 해제/복원 배지 같은 화면 요소를 플레이하지 않고도
        // 바로 확인할 수 있게 한다.
        public static void SeedDemoState(GameSession session)
        {
            session.RestorationTracker.ReportJudgement(
                Room3,
                new ScentJudgementResult(isBaseEmotionCorrect: true, stage: FeedbackStage.PianoAndViolinAndDrum, accuracy: 1.0));
        }

        private static MemoryRoomData CreateRoom1()
        {
            var answer = new MemoryRoomAnswer(
                Room1,
                new Scent(EmotionType.Sadness, new EmotionBlend(new[]
                {
                    new EmotionBlendEntry(EmotionType.Love, 8),
                    new EmotionBlendEntry(EmotionType.Fear, 7),
                })));

            var clues = new List<ClueDefinition>
            {
                new ClueDefinition(
                    new ClueId("clue-room1-photo"), Room1,
                    apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 8) }),
                    trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 8) })),
                new ClueDefinition(
                    new ClueId("clue-room1-letter"), Room1,
                    apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Fear, 7) }),
                    trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Fear, 7) })),
            };

            return new MemoryRoomData(answer, clues);
        }

        private static MemoryRoomData CreateRoom2()
        {
            var answer = new MemoryRoomAnswer(
                Room2,
                new Scent(EmotionType.Fear, new EmotionBlend(new[]
                {
                    new EmotionBlendEntry(EmotionType.Anger, 5),
                    new EmotionBlendEntry(EmotionType.Sadness, 5),
                    new EmotionBlendEntry(EmotionType.Joy, 5),
                })));

            var clues = new List<ClueDefinition>
            {
                // 겉보기와 실제가 다른 거짓 단서 하나 — 분석실 화면(이번 범위 밖)이
                // 생기면 고급 분석으로 이 차이를 드러낼 수 있다.
                new ClueDefinition(
                    new ClueId("clue-room2-diary"), Room2,
                    apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Joy, 5) }),
                    trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Anger, 5) })),
            };

            return new MemoryRoomData(answer, clues);
        }

        private static MemoryRoomData CreateRoom3()
        {
            var answer = new MemoryRoomAnswer(
                Room3,
                new Scent(EmotionType.Joy, new EmotionBlend(new[]
                {
                    new EmotionBlendEntry(EmotionType.Love, 6),
                    new EmotionBlendEntry(EmotionType.Anger, 4),
                })));

            var clues = new List<ClueDefinition>
            {
                new ClueDefinition(
                    new ClueId("clue-room3-ring"), Room3,
                    apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 6) }),
                    trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 6) })),
            };

            return new MemoryRoomData(answer, clues);
        }
    }
}
