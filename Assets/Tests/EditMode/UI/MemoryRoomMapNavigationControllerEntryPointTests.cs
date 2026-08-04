using System;
using System.Collections.Generic;
using System.Reflection;
using GameName.Core;
using GameName.Core.Ampoules;
using GameName.Core.Commissions;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Journal;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using GameName.UI.MemoryRoom;
using GameName.UI.Shared;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // 맵에서 이탈 노드(계단과는 별개의 공간)를 누르는 것은 일반 이동이 아니라
    // 즉시 계단으로 복귀임을 검증한다 — 인접하지 않은 방 깊숙한 곳에서도
    // 이탈 노드를 누르면 곧장 계단으로 돌아가야 "언제든 나갈 수 있다"는
    // 요구가 지켜진다.
    public class MemoryRoomMapNavigationControllerEntryPointTests
    {
        private static readonly MemoryGraphNodeId Staircase = new MemoryGraphNodeId("staircase");
        private static readonly MemoryGraphNodeId MemoryExit = new MemoryGraphNodeId("memory-exit");
        private static readonly MemoryRoomId FarRoom = new MemoryRoomId("room-far");
        private static readonly MemoryGraphNodeId FarRoomNode = MemoryGraphNodeId.OfRoom(FarRoom);

        private static VisualElement MakeMapRoot()
        {
            var root = new VisualElement();
            root.Add(new VisualElement { name = "memory-map-preview" });
            var expanded = new VisualElement { name = "memory-map-expanded" };
            expanded.Add(new Button { name = "memory-map-close-button" });
            expanded.Add(new VisualElement { name = "memory-map" });
            expanded.Add(new VisualElement { name = "memory-map-connections" });
            expanded.Add(new VisualElement { name = "memory-map-nodes" });
            expanded.Add(new Label { name = "memory-map-selection-info" });
            root.Add(expanded);
            return root;
        }

        private static RoomNavigationPanelView MakeStatusView()
        {
            var root = new VisualElement();
            root.Add(new Label { name = "current-room-label" });
            root.Add(new Label { name = "current-room-restored-badge" });
            root.Add(new VisualElement { name = "mentality-bar-fill" });
            root.Add(new Label { name = "mentality-value" });
            root.Add(new Label { name = "mentality-notice" });
            root.Add(new Label { name = "mentality-affordance-notice" });
            root.Add(new Label { name = "move-failure-message" });
            return new RoomNavigationPanelView(root);
        }

        // MemoryMapView.NodeSelected는 필드형 이벤트라, 실제 클릭 없이도 그
        // 뒤에 있는 델리게이트 필드를 리플렉션으로 직접 호출해 "그 노드가
        // 클릭되었다"는 알림을 재현할 수 있다.
        private static void RaiseNodeSelected(MemoryMapView mapView, MemoryGraphNodeId nodeId)
        {
            var field = typeof(MemoryMapView).GetField("NodeSelected", BindingFlags.NonPublic | BindingFlags.Instance);
            var handler = (Action<MemoryGraphNodeId>)field.GetValue(mapView);
            handler.Invoke(nodeId);
        }

        private static DialogueScript MakeDialogueScript() => new DialogueScript(new List<DialogueNode>
        {
            new DialogueNode(new DialogueLine("A", "첫"), new[] { new DialogueOption(1) }),
            new DialogueNode(new DialogueLine("B", "끝"), Array.Empty<DialogueOption>()),
        });

        [Test]
        public void 인접하지_않은_방에서도_이탈_노드를_누르면_즉시_계단으로_돌아간다()
        {
            var nodes = new[]
            {
                new MemoryGraphNode(Staircase, MemoryGraphNodeType.Staircase, new MemoryGraphCoordinate(0, 0)),
                new MemoryGraphNode(MemoryExit, MemoryGraphNodeType.Exit, new MemoryGraphCoordinate(0, 1)),
                new MemoryGraphNode(FarRoomNode, MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(5, 5)),
            };
            var openConnections = new[] { new OpenConnection(Staircase, MemoryExit) };
            var graph = new MemoryRoomGraph(nodes, openConnections, new LadderConnection[0]);
            Assert.IsFalse(graph.AreOpenlyConnected(Staircase, FarRoomNode));
            Assert.IsFalse(graph.AreOpenlyConnected(MemoryExit, FarRoomNode));

            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var restorationTracker = new MemoryRoomRestorationTracker(eventBus);
            var costSettings = new MentalityCostSettings(
                initialMentality: 100, maxMentality: 100,
                memoryRoomMoveCost: 1, basicAnalysisCost: 20, advancedAnalysisCost: 30,
                ampouleCraftingCost: 5, memoryRoomFullRestorationRecovery: 20);
            var mentalityGauge = new MentalityGauge(costSettings, eventBus);
            var playerLocation = new PlayerLocation(FarRoomNode);
            var movementProcessor = new MemoryRoomMovementProcessor(
                graph, restorationTracker, mentalityGauge, costSettings, playerLocation, eventBus);

            var inventory = new PlayerInventory(new InventorySettings(5), new SharedSlotInventoryPolicy());
            var storage = new AmpouleStorage(new AmpouleStorageSettings(3));
            var journal = new PlayerJournal(eventBus, storage, inventory);
            var dialogueProgressor = new DialogueProgressor(MakeDialogueScript(), eventBus);

            // FarRoomNode를 기억 진입 지점으로 두면 "인접하지 않은 곳에서
            // 시작"이 애매해지므로, 시작 지점은 Staircase로 두되 곧바로
            // FarRoomNode로 옮겨 실제 플레이(들어간 뒤 안쪽까지 이동)를
            // 재현한다.
            var commissionSession = new CommissionSession(
                eventBus, new List<IResettable> { mentalityGauge, (IResettable)inventory, (IResettable)storage },
                journal, dialogueProgressor, playerLocation, Staircase);
            commissionSession.Begin(new CommissionId("commission-1"));
            dialogueProgressor.Advance(0);
            commissionSession.TryAdvanceToMemory();
            playerLocation.MoveTo(FarRoomNode);

            var statusView = MakeStatusView();
            var mapView = new MemoryMapView(MakeMapRoot());
            var controller = new MemoryRoomMapNavigationController(
                statusView, mapView, graph, restorationTracker, mentalityGauge, costSettings, movementProcessor,
                playerLocation, new[] { FarRoom }, commissionSession, MemoryExit, eventBus);

            RaiseNodeSelected(mapView, MemoryExit);

            // 실제로 "이탈 노드" 자리로 이동한 게 아니라, 계단(기억 진입/이탈
            // 지점)으로 곧장 돌아갔어야 한다 — 이탈 노드는 그 트리거일 뿐,
            // 목적지가 아니다.
            Assert.AreEqual(Staircase, playerLocation.Current);
        }

        [Test]
        public void 계단은_이탈_노드와_달리_평범한_허브_노드라_인접하지_않으면_이동이_실패한다()
        {
            var nodes = new[]
            {
                new MemoryGraphNode(Staircase, MemoryGraphNodeType.Staircase, new MemoryGraphCoordinate(0, 0)),
                new MemoryGraphNode(MemoryExit, MemoryGraphNodeType.Exit, new MemoryGraphCoordinate(0, 1)),
                new MemoryGraphNode(FarRoomNode, MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(5, 5)),
            };
            var openConnections = new[] { new OpenConnection(Staircase, MemoryExit) };
            var graph = new MemoryRoomGraph(nodes, openConnections, new LadderConnection[0]);

            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var restorationTracker = new MemoryRoomRestorationTracker(eventBus);
            var costSettings = new MentalityCostSettings(
                initialMentality: 100, maxMentality: 100,
                memoryRoomMoveCost: 1, basicAnalysisCost: 20, advancedAnalysisCost: 30,
                ampouleCraftingCost: 5, memoryRoomFullRestorationRecovery: 20);
            var mentalityGauge = new MentalityGauge(costSettings, eventBus);
            var playerLocation = new PlayerLocation(FarRoomNode);
            var movementProcessor = new MemoryRoomMovementProcessor(
                graph, restorationTracker, mentalityGauge, costSettings, playerLocation, eventBus);

            var inventory = new PlayerInventory(new InventorySettings(5), new SharedSlotInventoryPolicy());
            var storage = new AmpouleStorage(new AmpouleStorageSettings(3));
            var journal = new PlayerJournal(eventBus, storage, inventory);
            var dialogueProgressor = new DialogueProgressor(MakeDialogueScript(), eventBus);

            var commissionSession = new CommissionSession(
                eventBus, new List<IResettable> { mentalityGauge, (IResettable)inventory, (IResettable)storage },
                journal, dialogueProgressor, playerLocation, Staircase);
            commissionSession.Begin(new CommissionId("commission-1"));
            dialogueProgressor.Advance(0);
            commissionSession.TryAdvanceToMemory();
            playerLocation.MoveTo(FarRoomNode);

            var statusView = MakeStatusView();
            var mapView = new MemoryMapView(MakeMapRoot());
            new MemoryRoomMapNavigationController(
                statusView, mapView, graph, restorationTracker, mentalityGauge, costSettings, movementProcessor,
                playerLocation, new[] { FarRoom }, commissionSession, MemoryExit, eventBus);

            RaiseNodeSelected(mapView, Staircase);

            // 텔레포트되지 않는다 — 계단은 이제 평범한 허브 노드라 인접하지
            // 않으면 그냥 이동이 실패한다.
            Assert.AreEqual(FarRoomNode, playerLocation.Current);
        }
    }
}
