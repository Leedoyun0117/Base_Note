using System;
using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Clues;
using GameName.Core.Commissions;
using GameName.Core.Dialogue;
using GameName.Core.Emotions;
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
    // 실제 기획 데이터(레벨 에디터 산출물, 세이브 데이터 등)가 생기기 전까지
    // 쓰는 더미 데이터.
    //
    // 허브(계단-분석실-조향실 삼각형)는 모든 의뢰가 공유하는 고정 지형이라
    // GameSessionData에 한 번만 담는다. 의뢰별 데이터(방 그래프/정답/단서/
    // 대사/보상)는 CommissionData 두 개로 나눠, 의뢰 교체(GameSession.
    // LoadCommission)가 실제로 방 구조와 내용을 통째로 바꾼다는 것을 보여준다 —
    // 의뢰 1은 사다리로 이어진 방 3개, 의뢰 2는 허브에서 곧장 갈라지는 방
    // 2개로 그래프 모양 자체가 다르다.
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

        public static readonly MemoryRoomId Room4 = new MemoryRoomId("room-4");
        public static readonly MemoryRoomId Room5 = new MemoryRoomId("room-5");

        public static GameSessionData CreateWorldData()
        {
            var hubNodes = new List<MemoryGraphNode>
            {
                new MemoryGraphNode(Staircase, MemoryGraphNodeType.Staircase),
                new MemoryGraphNode(AnalysisRoom, MemoryGraphNodeType.AnalysisRoom),
                new MemoryGraphNode(PerfumeryRoom, MemoryGraphNodeType.PerfumeryRoom),
            };

            var hubOpenConnections = new List<OpenConnection>
            {
                new OpenConnection(Staircase, AnalysisRoom),
                new OpenConnection(Staircase, PerfumeryRoom),
                new OpenConnection(AnalysisRoom, PerfumeryRoom),
            };

            var commissions = new List<CommissionData>
            {
                CreateCommission1(),
                CreateCommission2(),
            };

            return new GameSessionData(
                hubNodes,
                hubOpenConnections,
                memoryEntryNodeId: Staircase,
                perfumeryRoomNodeId: PerfumeryRoom,
                analysisRoomNodeId: AnalysisRoom,
                commissions: commissions);
        }

        // 의뢰 1: 사다리로 이어진 방 3개(방1 - 방2 - 방3), 분석실에서 최하단
        // 방으로 바로 들어가는 지름길도 있다.
        private static CommissionData CreateCommission1()
        {
            var roomNodes = new List<MemoryGraphNode>
            {
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room1), MemoryGraphNodeType.MemoryRoom),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room2), MemoryGraphNodeType.MemoryRoom),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room3), MemoryGraphNodeType.MemoryRoom),
            };

            var openConnections = new List<OpenConnection>
            {
                new OpenConnection(Staircase, MemoryGraphNodeId.OfRoom(Room1)),
                // 분석실에서 계단을 거치지 않고 최하단 기억 방으로 바로 들어갈 수
                // 있게 한다 — 분석실 화면에서 요구하는 이동 수단 중 하나다.
                new OpenConnection(AnalysisRoom, MemoryGraphNodeId.OfRoom(Room1)),
            };

            var ladderConnections = new List<LadderConnection>
            {
                new LadderConnection(upperRoom: Room2, lowerRoom: Room1),
                new LadderConnection(upperRoom: Room3, lowerRoom: Room2),
            };

            var roomData = new List<MemoryRoomData>
            {
                CreateRoom1(), CreateRoom2(), CreateRoom3(),
            };

            var dialogueScript = CreateLinearDialogue(
                ("의뢰인", "부탁드립니다. 그 애 방에서 무슨 일이 있었는지 알고 싶어요."),
                ("탐정", "기억을 살펴보겠습니다. 시간이 좀 걸릴 수도 있어요."),
                ("의뢰인", "괜찮아요. 진실만 알 수 있다면요."));

            var rewardTable = CreateStandardRewardTable();

            return new CommissionData(
                new CommissionId("demo-commission-1"),
                roomNodes, openConnections, ladderConnections, roomData, dialogueScript, rewardTable);
        }

        // 의뢰 2: 허브에서 곧장 갈라지는 방 2개(사다리 없음) — 의뢰 1과 그래프
        // 모양 자체가 다르다는 것을 보여주기 위한 구성이다.
        private static CommissionData CreateCommission2()
        {
            var roomNodes = new List<MemoryGraphNode>
            {
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room4), MemoryGraphNodeType.MemoryRoom),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room5), MemoryGraphNodeType.MemoryRoom),
            };

            var openConnections = new List<OpenConnection>
            {
                new OpenConnection(Staircase, MemoryGraphNodeId.OfRoom(Room4)),
                new OpenConnection(Staircase, MemoryGraphNodeId.OfRoom(Room5)),
            };

            var ladderConnections = new List<LadderConnection>();

            var roomData = new List<MemoryRoomData> { CreateRoom4(), CreateRoom5() };

            var dialogueScript = CreateLinearDialogue(
                ("의뢰인", "이번엔 다른 사람 부탁이에요. 오래된 물건 두 개뿐이지만요."),
                ("탐정", "적어도 방향은 분명하겠군요. 살펴보겠습니다."),
                ("의뢰인", "부디 잘 부탁드려요."));

            var rewardTable = CreateStandardRewardTable();

            return new CommissionData(
                new CommissionId("demo-commission-2"),
                roomNodes, openConnections, ladderConnections, roomData, dialogueScript, rewardTable);
        }

        // 코드에 대사를 박아 두지 않기 위한 유일한 자리. 실제 콘텐츠 소스가
        // 생기면 이 메서드만 그 소스를 읽어 DialogueScript를 만드는 코드로
        // 바꾸면 된다. 대사는 전부 옵션이 하나(라벨 없음)뿐인 선형 구조다 —
        // DialogueOption/Node는 분기를 표현할 수 있지만, 이번 데모 의뢰는
        // 특수 분기가 없어 쓰지 않는다.
        private static DialogueScript CreateLinearDialogue(params (string speaker, string text)[] lines)
        {
            var nodes = new List<DialogueNode>(lines.Length);
            for (var i = 0; i < lines.Length; i++)
            {
                var isLast = i == lines.Length - 1;
                var options = isLast ? Array.Empty<DialogueOption>() : new[] { new DialogueOption(i + 1) };
                nodes.Add(new DialogueNode(new DialogueLine(lines[i].speaker, lines[i].text), options));
            }

            return new DialogueScript(nodes);
        }

        // 의뢰마다 다른 보상표를 줄 수도 있지만, 데모에서는 같은 등급 구조를
        // 재사용한다 — 등급 구조 자체가 아니라 "방/정답/단서/대사가 실제로
        // 교체되는가"가 이번 데모가 증명해야 할 지점이기 때문이다.
        private static RewardTable CreateStandardRewardTable()
        {
            return new RewardTable(new[]
            {
                new RewardTier(minimumAverageAccuracy: 0.0, emotionalValue: 10, reactionDialogue: "음... 짐작과는 조금 다르네요."),
                new RewardTier(minimumAverageAccuracy: 0.5, emotionalValue: 30, reactionDialogue: "그랬군요. 이제 알겠어요."),
                new RewardTier(minimumAverageAccuracy: 0.8, emotionalValue: 60, reactionDialogue: "정말 감사합니다. 이제야 마음이 놓여요."),
            });
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

            var upgradeCatalog = new UpgradeCatalog(new[]
            {
                new UpgradeOption(UpgradeCategory.InventoryCapacity, level: 1, price: 20, amount: 2, description: "인벤토리 칸 +2"),
                new UpgradeOption(UpgradeCategory.AmpouleStorageCapacity, level: 1, price: 20, amount: 2, description: "앰플 보관 칸 +2"),
                new UpgradeOption(UpgradeCategory.AnalysisEfficiency, level: 1, price: 15, amount: 5, description: "분석 비용 -5"),
                new UpgradeOption(UpgradeCategory.RoomMoveEfficiency, level: 1, price: 15, amount: 1, description: "이동 비용 -1"),
            });

            return new GameSessionSettings(
                mentalityCostSettings,
                mentalityCostSettings,
                judgementSettings,
                compositionPolicy,
                inventorySettings,
                storageSettings,
                new GuidAmpouleIdGenerator(),
                upgradeCatalog);
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
                // 겉보기와 실제가 다른 거짓 단서 하나 — 분석실에서 고급 분석까지
                // 해도 겉보기 구성(Joy)만 드러난다. 실제 구성(Anger)은 화면
                // 어디에도 노출되지 않는다.
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

        private static MemoryRoomData CreateRoom4()
        {
            var answer = new MemoryRoomAnswer(
                Room4,
                new Scent(EmotionType.Anger, new EmotionBlend(new[]
                {
                    new EmotionBlendEntry(EmotionType.Fear, 6),
                    new EmotionBlendEntry(EmotionType.Sadness, 6),
                })));

            var clues = new List<ClueDefinition>
            {
                new ClueDefinition(
                    new ClueId("clue-room4-note"), Room4,
                    apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Fear, 6) }),
                    trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Fear, 6) })),
            };

            return new MemoryRoomData(answer, clues);
        }

        private static MemoryRoomData CreateRoom5()
        {
            var answer = new MemoryRoomAnswer(
                Room5,
                new Scent(EmotionType.Love, new EmotionBlend(new[]
                {
                    new EmotionBlendEntry(EmotionType.Joy, 5),
                    new EmotionBlendEntry(EmotionType.Sadness, 3),
                })));

            var clues = new List<ClueDefinition>
            {
                new ClueDefinition(
                    new ClueId("clue-room5-ribbon"), Room5,
                    apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Joy, 5) }),
                    trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Joy, 5) })),
            };

            return new MemoryRoomData(answer, clues);
        }
    }
}
