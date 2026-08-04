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
using GameName.UI.Session;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // 0-3 검증: 의뢰를 교체하면 방 그래프/방별 정답/단서/대사가 전부 새 의뢰
    // 것으로 바뀌고, 이전 의뢰의 것은 더 이상 조회되지 않아야 한다.
    public class CommissionDataSwapTests
    {
        private static readonly MemoryGraphNodeId Staircase = new MemoryGraphNodeId("staircase");
        private static readonly MemoryGraphNodeId AnalysisRoom = new MemoryGraphNodeId("analysis-room");
        private static readonly MemoryGraphNodeId PerfumeryRoom = new MemoryGraphNodeId("perfumery-room");
        private static readonly MemoryGraphNodeId MemoryExit = new MemoryGraphNodeId("memory-exit");

        private static readonly MemoryRoomId RoomA = new MemoryRoomId("room-a");
        private static readonly MemoryRoomId RoomB = new MemoryRoomId("room-b");

        private static GameSession MakeSession(out CommissionData commission1, out CommissionData commission2)
        {
            var hubNodes = new List<MemoryGraphNode>
            {
                new MemoryGraphNode(Staircase, MemoryGraphNodeType.Staircase, new MemoryGraphCoordinate(0, 0)),
                new MemoryGraphNode(AnalysisRoom, MemoryGraphNodeType.AnalysisRoom, new MemoryGraphCoordinate(1, 0)),
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

            commission1 = MakeCommission("commission-1", RoomA, "clue-a", "첫 번째 의뢰 대사");
            commission2 = MakeCommission("commission-2", RoomB, "clue-b", "두 번째 의뢰 대사");

            var data = new GameSessionData(
                hubNodes, hubOpenConnections,
                memoryEntryNodeId: Staircase, memoryExitNodeId: MemoryExit,
                perfumeryRoomNodeId: PerfumeryRoom, analysisRoomNodeId: AnalysisRoom,
                commissions: new[] { commission1, commission2 });

            var mentalityCostSettings = new MentalityCostSettings(
                initialMentality: 100, maxMentality: 100,
                memoryRoomMoveCost: 1, basicAnalysisCost: 20, advancedAnalysisCost: 30,
                ampouleCraftingCost: 8, memoryRoomFullRestorationRecovery: 20);

            var settings = new GameSessionSettings(
                mentalityCostSettings,
                mentalityCostSettings,
                new ScentJudgementSettings(highAccuracyThreshold: 0.8),
                new EmotionCompositionPolicy(
                    minSupportingEmotionCount: 1, maxSupportingEmotionCount: 4, allowSupportingEmotionSameAsBase: false),
                new InventorySettings(4),
                new AmpouleStorageSettings(3),
                new ClueStorageSettings(6),
                new GuidAmpouleIdGenerator(),
                new UpgradeCatalog(Array.Empty<UpgradeOption>()));

            return new GameSession(data, settings);
        }

        private static CommissionData MakeCommission(
            string commissionId, MemoryRoomId roomId, string clueId, string firstLineText)
        {
            var roomNodes = new List<MemoryGraphNode>
            {
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(roomId), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(0, 1)),
            };
            var openConnections = new List<OpenConnection>
            {
                new OpenConnection(Staircase, MemoryGraphNodeId.OfRoom(roomId)),
            };

            var answer = new MemoryRoomAnswer(
                roomId,
                new Scent(EmotionType.Joy, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) })));
            var clue = new ClueDefinition(
                new ClueId(clueId), roomId,
                apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }),
                trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));

            var roomData = new List<MemoryRoomData> { new MemoryRoomData(answer, new[] { clue }) };

            var dialogueNodes = new List<DialogueNode>
            {
                new DialogueNode(new DialogueLine("의뢰인", firstLineText), Array.Empty<DialogueOption>()),
            };

            var rewardTable = new RewardTable(new[]
            {
                new RewardTier(minimumAverageAccuracy: 0.0, emotionalValue: 10, reactionDialogue: "그렇군요."),
            });

            return new CommissionData(
                new CommissionId(commissionId),
                roomNodes, openConnections, new List<LadderConnection>(), roomData,
                new DialogueScript(dialogueNodes), rewardTable);
        }

        [Test]
        public void 의뢰를_교체하면_방_그래프_정답_단서_대사가_전부_새_의뢰_것으로_바뀐다()
        {
            var session = MakeSession(out var commission1, out var commission2);

            // 첫 의뢰(생성자가 자동으로 시작) 상태 확인.
            CollectionAssert.Contains(session.RoomIds, RoomA);
            CollectionAssert.DoesNotContain(session.RoomIds, RoomB);
            Assert.IsTrue(session.AnswerRepository.TryGetAnswer(RoomA, out _));
            Assert.IsFalse(session.AnswerRepository.TryGetAnswer(RoomB, out _));
            Assert.IsTrue(session.ClueTracker.TryGetDefinition(new ClueId("clue-a"), out _));
            Assert.IsFalse(session.ClueTracker.TryGetDefinition(new ClueId("clue-b"), out _));
            Assert.IsTrue(session.Graph.TryGetNode(MemoryGraphNodeId.OfRoom(RoomA), out _));
            Assert.IsFalse(session.Graph.TryGetNode(MemoryGraphNodeId.OfRoom(RoomB), out _));
            Assert.AreEqual("첫 번째 의뢰 대사", session.DialogueProgressor.CurrentLine.Text);

            session.LoadCommission(commission2);

            CollectionAssert.Contains(session.RoomIds, RoomB);
            CollectionAssert.DoesNotContain(session.RoomIds, RoomA);
            Assert.IsTrue(session.AnswerRepository.TryGetAnswer(RoomB, out _));
            Assert.IsFalse(session.AnswerRepository.TryGetAnswer(RoomA, out _));
            Assert.IsTrue(session.ClueTracker.TryGetDefinition(new ClueId("clue-b"), out _));
            Assert.IsFalse(session.ClueTracker.TryGetDefinition(new ClueId("clue-a"), out _));
            Assert.IsTrue(session.Graph.TryGetNode(MemoryGraphNodeId.OfRoom(RoomB), out _));
            Assert.IsFalse(session.Graph.TryGetNode(MemoryGraphNodeId.OfRoom(RoomA), out _));
            Assert.AreEqual("두 번째 의뢰 대사", session.DialogueProgressor.CurrentLine.Text);

            session.Dispose();
        }
    }
}
