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
        public static readonly MemoryGraphNodeId MemoryExit = new MemoryGraphNodeId("memory-exit");

        public static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");
        public static readonly MemoryRoomId Room2 = new MemoryRoomId("room-2");
        public static readonly MemoryRoomId Room3 = new MemoryRoomId("room-3");

        public static readonly MemoryRoomId Room4 = new MemoryRoomId("room-4");
        public static readonly MemoryRoomId Room5 = new MemoryRoomId("room-5");

        public static GameSessionData CreateWorldData()
        {
            // 허브는 전부 Row 0 — "가장 현재"인 지점들이다. 가로로만 삼각형
            // 모양으로 늘어놓는다(문 연결이라 Column에는 규칙이 없다).
            //
            // 이탈 공간(MemoryExit)은 계단과는 별개의 노드다. 계단 바로
            // 아래(Column 1, Row 1)에 매달아 둬서 계단을 거쳐야 닿을 수 있게
            // 하면서도 지도에서 분명히 다른 자리로 보이게 한다 — 음수 좌표는
            // 쓰지 않는다(MemoryMapView의 지도 컨테이너 크기 계산이 좌표
            // 최댓값만 보고 정하므로, 0보다 작은 좌표는 컨테이너 밖으로 잘려
            // 그려진다). 이 자리는 어느 의뢰의 방과도 겹치지 않는다 — 의뢰 1의
            // 방들은 Column 3, 의뢰 2의 방들은 Row 1의 Column 0/2를 쓴다.
            var hubNodes = new List<MemoryGraphNode>
            {
                new MemoryGraphNode(Staircase, MemoryGraphNodeType.Staircase, new MemoryGraphCoordinate(1, 0)),
                new MemoryGraphNode(AnalysisRoom, MemoryGraphNodeType.AnalysisRoom, new MemoryGraphCoordinate(0, 0)),
                new MemoryGraphNode(PerfumeryRoom, MemoryGraphNodeType.PerfumeryRoom, new MemoryGraphCoordinate(2, 0)),
                new MemoryGraphNode(MemoryExit, MemoryGraphNodeType.Exit, new MemoryGraphCoordinate(1, 1)),
            };

            var hubOpenConnections = new List<OpenConnection>
            {
                new OpenConnection(Staircase, AnalysisRoom),
                new OpenConnection(Staircase, PerfumeryRoom),
                new OpenConnection(AnalysisRoom, PerfumeryRoom),
                new OpenConnection(Staircase, MemoryExit),
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
                memoryExitNodeId: MemoryExit,
                perfumeryRoomNodeId: PerfumeryRoom,
                analysisRoomNodeId: AnalysisRoom,
                commissions: commissions);
        }

        // 의뢰 1: 사다리로 이어진 방 3개(방1 - 방2 - 방3), 분석실에서 최하단
        // 방으로 바로 들어가는 지름길도 있다.
        private static CommissionData CreateCommission1()
        {
            // 사다리로 이어진 세 방은 한 줄로 세운다. Room1이 허브에서 가장
            // 먼저 들어가는 방이라 Row가 가장 크다(가장 과거) — 사다리를 타고
            // 올라갈수록(Room2 -> Room3) Row가 작아져 점점 "현재"에 가까워진다.
            //
            // Column은 계단(1)·분석실(0) 어느 쪽과도 겹치지 않는 3을 쓴다 —
            // 계단과 같은 줄(Column 1)에 두면, 계단-Room1 직선 연결이 화면에서
            // 그 사이에 낀 Room2/Room3 자리를 그대로 관통해 지나가면서 마치
            // 계단이 Room2/Room3과도 연결된 것처럼 보이는 착시가 생긴다(실제
            // 연결은 계단↔Room1, 분석실↔Room1뿐이다).
            var roomNodes = new List<MemoryGraphNode>
            {
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room1), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(3, 3)),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room2), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(3, 2)),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room3), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(3, 1)),
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
            // 사다리가 없으니 서로 겹치지만 않으면 된다 — 허브 바로 아래
            // Row 1에, 분석실/조향실 쪽에 하나씩 걸쳐 놓는다.
            var roomNodes = new List<MemoryGraphNode>
            {
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room4), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(0, 1)),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room5), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(2, 1)),
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
            // 조향 비용(예전 8)이 "이동 다음으로 싼 행동"이라 잔량이 어중간할
            // 때 무엇을 할 수 있는지를 사실상 이 값이 정한다 — 이동(1)은 항상
            // 무료로 면제되므로, 진짜 "이동밖에 할 수 없는" 죽은 구간의 폭은
            // 조향 비용보다 1 작다. 8일 때는 그 폭이 7이었다(잔량 1~7). 5로
            // 낮춰 폭을 4(잔량 1~4)로 줄인다 — 분석 비용(20/30)은 이 폭에
            // 영향을 주지 않으므로(조향이 이미 더 싸다) 그대로 둔다. 자세한
            // 근거와 남은 정신력으로 가능한 행동 횟수 계산은 이 조정을 만든
            // 대화 기록을 참고.
            var mentalityCostSettings = new MentalityCostSettings(
                initialMentality: 100, maxMentality: 100,
                memoryRoomMoveCost: 1, basicAnalysisCost: 20, advancedAnalysisCost: 30,
                ampouleCraftingCost: 5, memoryRoomFullRestorationRecovery: 20);

            var judgementSettings = new ScentJudgementSettings(highAccuracyThreshold: 0.8);

            var compositionPolicy = new EmotionCompositionPolicy(
                minSupportingEmotionCount: 2, maxSupportingEmotionCount: 4, allowSupportingEmotionSameAsBase: false);

            // 확대 화면의 인벤토리 사이드바가 2x2로 그려지는 것과 맞물리는
            // 값이다. 다만 그 화면이 4를 전제로 그리는 것이 아니라, 이 값을 읽어
            // 칸 개수를 정한다 — 여기서 6으로 올리면 화면도 여섯 칸을 그린다.
            var inventorySettings = new InventorySettings(initialCapacity: 4);
            var storageSettings = new AmpouleStorageSettings(maxStoredAmpoules: 3);

            // 데모 의뢰 1의 단서 총합(방마다 포스터 1 + 바닥 물건 1 = 6개)보다
            // 여유 있게 잡아, 정상적으로 플레이하면 보관대 부족으로 막히지
            // 않게 한다. 그렇다고 무제한으로 두지는 않는다 — 그러면 "무엇을
            // 인벤토리에 남기고 무엇을 보관대로 옮길지" 라는 선택 자체가
            // 사라져, 이 기능이 풀려던 자원 관리 문제(2번 문단)가 다시
            // "사실상 무한 인벤토리"로 되돌아간다.
            var clueStorageSettings = new ClueStorageSettings(maxStoredClues: 6);

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
                clueStorageSettings,
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

        // 단서의 가로 자리는 방 길이에 대한 비율이다(0 = 왼쪽 끝, 1 = 오른쪽
        // 끝). 방마다 포스터와 바닥 물건을 서로 다른 자리에 두어, 배치가 코드가
        // 아니라 이 데이터에서 온다는 것이 눈으로 보이게 한다. 세로는 여기
        // 적지 않는다 — 포스터는 벽, 바닥 물건은 바닥이라는 것이 종류만으로
        // 이미 정해진다.
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
                    new ClueId("clue-room1-photo"), ClueKind.Poster, new CluePositionRatio(0.18f),
                    apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 8) }),
                    trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 8) })),
                new ClueDefinition(
                    new ClueId("clue-room1-letter"), ClueKind.FloorObject, new CluePositionRatio(0.72f),
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
                new ClueDefinition(
                    new ClueId("clue-room2-poster"), ClueKind.Poster, new CluePositionRatio(0.82f),
                    apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Sadness, 5) }),
                    trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Sadness, 5) })),
                // 겉보기와 실제가 다른 거짓 단서 하나 — 분석실에서 고급 분석까지
                // 해도 겉보기 구성(Joy)만 드러난다. 실제 구성(Anger)은 화면
                // 어디에도 노출되지 않는다.
                new ClueDefinition(
                    new ClueId("clue-room2-diary"), ClueKind.FloorObject, new CluePositionRatio(0.28f),
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
                    new ClueId("clue-room3-poster"), ClueKind.Poster, new CluePositionRatio(0.35f),
                    apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Anger, 4) }),
                    trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Anger, 4) })),
                new ClueDefinition(
                    new ClueId("clue-room3-ring"), ClueKind.FloorObject, new CluePositionRatio(0.88f),
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
                    new ClueId("clue-room4-poster"), ClueKind.Poster, new CluePositionRatio(0.15f),
                    apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Sadness, 6) }),
                    trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Sadness, 6) })),
                new ClueDefinition(
                    new ClueId("clue-room4-note"), ClueKind.FloorObject, new CluePositionRatio(0.62f),
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
                    new ClueId("clue-room5-poster"), ClueKind.Poster, new CluePositionRatio(0.70f),
                    apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Sadness, 3) }),
                    trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Sadness, 3) })),
                new ClueDefinition(
                    new ClueId("clue-room5-ribbon"), ClueKind.FloorObject, new CluePositionRatio(0.24f),
                    apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Joy, 5) }),
                    trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Joy, 5) })),
            };

            return new MemoryRoomData(answer, clues);
        }
    }
}
