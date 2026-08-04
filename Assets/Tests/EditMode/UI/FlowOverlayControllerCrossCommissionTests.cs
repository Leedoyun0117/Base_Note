using System;
using System.Collections.Generic;
using GameName.Core;
using GameName.Core.Ampoules;
using GameName.Core.Clues;
using GameName.Core.Commissions;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Journal;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using GameName.UI.Flow;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // 회귀 검증: FlowOverlayController는 씬이 켜져 있는 동안 여러 의뢰를
    // 거치며 계속 살아있는 하나의 인스턴스다. "나가기를 눌렀다"는 표시
    // (_exitRequested)가 의뢰가 바뀔 때 새로 나가지 않으면, 다음 의뢰의 시작
    // 위치도 하필 계단이라 조건이 우연히 다시 참이 되어 아무것도 하지
    // 않았는데 이탈 확인이 다시 뜨는 버그가 된다.
    public class FlowOverlayControllerCrossCommissionTests
    {
        private static readonly MemoryGraphNodeId Staircase = new MemoryGraphNodeId("staircase");

        private static VisualElement MakeRoot()
        {
            var root = new VisualElement();

            var dialoguePanel = new VisualElement { name = "dialogue-panel" };
            dialoguePanel.Add(new Label { name = "dialogue-speaker" });
            dialoguePanel.Add(new Label { name = "dialogue-text" });
            dialoguePanel.Add(new Button { name = "dialogue-advance-button" });
            root.Add(dialoguePanel);

            var exitPanel = new VisualElement { name = "exit-confirm-panel" };
            exitPanel.Add(new Label { name = "exit-summary-notice" });
            exitPanel.Add(new Button { name = "request-exit-button" });
            var confirmControls = new VisualElement { name = "exit-confirm-controls" };
            confirmControls.Add(new Button { name = "confirm-exit-button" });
            confirmControls.Add(new Button { name = "cancel-exit-button" });
            exitPanel.Add(confirmControls);
            root.Add(exitPanel);

            return root;
        }

        private static DialogueScript MakeDialogueScript() => new DialogueScript(new List<DialogueNode>
        {
            new DialogueNode(new DialogueLine("A", "첫"), new[] { new DialogueOption(1) }),
            new DialogueNode(new DialogueLine("B", "끝"), Array.Empty<DialogueOption>()),
        });

        [Test]
        public void 한_의뢰에서_나가기를_누른_뒤_다음_의뢰를_시작해도_계단에서_이탈_확인이_저절로_뜨지_않는다()
        {
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var costSettings = new MentalityCostSettings(
                initialMentality: 100, maxMentality: 100,
                memoryRoomMoveCost: 1, basicAnalysisCost: 20, advancedAnalysisCost: 30,
                ampouleCraftingCost: 5, memoryRoomFullRestorationRecovery: 20);
            var mentalityGauge = new MentalityGauge(costSettings, eventBus);
            var inventory = new PlayerInventory(new InventorySettings(5), new SharedSlotInventoryPolicy());
            var storage = new AmpouleStorage(new AmpouleStorageSettings(3));
            var restorationTracker = new MemoryRoomRestorationTracker(eventBus);
            var analysisProgress = new ClueAnalysisProgress();
            var clueStorage = new ClueStorage(new ClueStorageSettings(6));
            var journal = new PlayerJournal(eventBus, storage, inventory);
            var dialogueProgressor = new DialogueProgressor(MakeDialogueScript(), eventBus);
            var playerLocation = new PlayerLocation(Staircase);

            var resettableSystems = new List<IResettable>
            {
                mentalityGauge, (IResettable)inventory, (IResettable)storage, restorationTracker, analysisProgress,
                clueStorage,
            };
            var commissionSession = new CommissionSession(
                eventBus, resettableSystems, journal, dialogueProgressor, playerLocation, Staircase);

            var root = MakeRoot();
            new FlowOverlayController(
                root, commissionSession, dialogueProgressor, playerLocation, Staircase,
                inventory, clueStorage, analysisProgress, new MemoryRoomId[0], restorationTracker, eventBus);

            var exitPanel = root.Q<VisualElement>("exit-confirm-panel");

            // 의뢰 1: 기억으로 들어가 지도에서 나가기를 누른다 — 이탈 확인이 뜬다.
            commissionSession.Begin(new CommissionId("commission-1"));
            dialogueProgressor.LoadScript(MakeDialogueScript());
            dialogueProgressor.Advance(0);
            commissionSession.TryAdvanceToMemory();
            commissionSession.TryReturnToEntryPoint();

            Assert.AreEqual(DisplayStyle.Flex, exitPanel.style.display.value);

            // 확정도 취소도 하지 않은 채 다음 의뢰가 시작된다(Begin() 재호출).
            commissionSession.Begin(new CommissionId("commission-2"));
            dialogueProgressor.LoadScript(MakeDialogueScript());
            dialogueProgressor.Advance(0);
            commissionSession.TryAdvanceToMemory();

            // 의뢰 2의 시작 위치도 계단이지만, 이번 의뢰에서는 나가기를 누른
            // 적이 없으니 이탈 확인이 저절로 뜨면 안 된다.
            Assert.AreEqual(DisplayStyle.None, exitPanel.style.display.value);
        }
    }
}
