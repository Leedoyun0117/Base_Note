using System;
using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Clues;
using GameName.Core.Commissions;
using GameName.Core.Dialogue;
using GameName.Core.Emotions;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Journal;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using GameName.Core.Rewards;
using GameName.Core.Upgrades;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 섹션 3 검증:
    //  1) 업그레이드는 구매 즉시 적용된다 — 다만 상점은 의뢰 완료 화면
    //     (CommissionStage.Completed)에서만 열리므로, 구매 시점에는 이미
    //     "지금 진행 중인 의뢰"가 없다. 그래서 즉시 적용해도 결과적으로
    //     "다음 의뢰부터 적용"이 성립한다 — 별도의 지연 적용 장치가 없다.
    //  2) 흔한 실수 방지: 의뢰 초기화(CommissionSession.Begin)가 구매한
    //     업그레이드 값(용량/비용)을 덮어쓰지 않아야 한다. Reset()은 내용물만
    //     비우고 용량/비용 같은 업그레이드 대상 수치는 건드리지 않는다.
    public class UpgradeShopTests
    {
        private static readonly MemoryGraphNodeId EntryNode = new MemoryGraphNodeId("staircase");

        private static UpgradeCatalog MakeCatalog() => new UpgradeCatalog(new[]
        {
            new UpgradeOption(UpgradeCategory.InventoryCapacity, level: 1, price: 20, amount: 2, description: "인벤토리 칸 +2"),
            new UpgradeOption(UpgradeCategory.AmpouleStorageCapacity, level: 1, price: 20, amount: 2, description: "앰플 보관 칸 +2"),
            new UpgradeOption(UpgradeCategory.AnalysisEfficiency, level: 1, price: 15, amount: 5, description: "분석 비용 -5"),
            new UpgradeOption(UpgradeCategory.RoomMoveEfficiency, level: 1, price: 15, amount: 1, description: "이동 비용 -1"),
        });

        [Test]
        public void 업그레이드_구매는_인벤토리_용량을_영구히_늘린다()
        {
            var inventory = new PlayerInventory(new InventorySettings(4), new SharedSlotInventoryPolicy());
            var storage = new AmpouleStorage(new AmpouleStorageSettings(3));
            var costSettings = new MentalityCostSettings(100, 100, 1, 20, 30, 8, 20);
            var displayCollection = new DisplayCollection();
            displayCollection.Add(new Gift(30, "선물"));

            var shop = new UpgradeShop(MakeCatalog(), displayCollection, inventory, storage, costSettings);

            var result = shop.Purchase(UpgradeCategory.InventoryCapacity);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(6, inventory.Capacity);
            Assert.AreEqual(10, displayCollection.AvailableEmotionalValue);
        }

        [Test]
        public void 감정량이_부족하면_구매에_실패하고_아무_것도_바뀌지_않는다()
        {
            var inventory = new PlayerInventory(new InventorySettings(4), new SharedSlotInventoryPolicy());
            var storage = new AmpouleStorage(new AmpouleStorageSettings(3));
            var costSettings = new MentalityCostSettings(100, 100, 1, 20, 30, 8, 20);
            var displayCollection = new DisplayCollection();

            var shop = new UpgradeShop(MakeCatalog(), displayCollection, inventory, storage, costSettings);

            var result = shop.Purchase(UpgradeCategory.InventoryCapacity);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(UpgradeFailureReason.InsufficientEmotionalValue, result.FailureReason);
            Assert.AreEqual(4, inventory.Capacity);
        }

        [Test]
        public void 의뢰_초기화는_구매한_업그레이드_수치를_덮어쓰지_않는다()
        {
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var inventory = new PlayerInventory(new InventorySettings(4), new SharedSlotInventoryPolicy());
            var storage = new AmpouleStorage(new AmpouleStorageSettings(3));
            var costSettings = new MentalityCostSettings(100, 100, 1, 20, 30, 8, 20);
            var displayCollection = new DisplayCollection();
            displayCollection.Add(new Gift(50, "선물"));

            var shop = new UpgradeShop(MakeCatalog(), displayCollection, inventory, storage, costSettings);
            shop.Purchase(UpgradeCategory.InventoryCapacity);
            shop.Purchase(UpgradeCategory.AnalysisEfficiency);

            Assert.AreEqual(6, inventory.Capacity);
            Assert.AreEqual(15, costSettings.BasicAnalysisCost);

            // 인벤토리에 무언가를 담아 둔 뒤, 새 의뢰가 시작되는 상황을 흉내낸다.
            inventory.TryStore(new ClueInfo(
                new ClueId("clue-1"), ClueKind.FloorObject, new CluePositionRatio(0.5f),
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Joy, 1) })));

            var mentalityGauge = new MentalityGauge(costSettings, eventBus);
            var restorationTracker = new MemoryRoomRestorationTracker(eventBus);
            var analysisProgress = new ClueAnalysisProgress();
            var journal = new PlayerJournal(eventBus, storage, inventory);
            var dialogueNodes = new List<DialogueNode>
            {
                new DialogueNode(new DialogueLine("A", "끝"), Array.Empty<DialogueOption>()),
            };
            var dialogueProgressor = new DialogueProgressor(new DialogueScript(dialogueNodes), eventBus);
            var playerLocation = new PlayerLocation(EntryNode);

            var resettables = new List<IResettable>
            {
                mentalityGauge, (IResettable)inventory, (IResettable)storage, restorationTracker, analysisProgress,
            };
            var session = new CommissionSession(
                eventBus, resettables, journal, dialogueProgressor, playerLocation, EntryNode);

            session.Begin(new CommissionId("commission-2"));

            // 흔한 실수 지점: Reset()은 내용물만 비운다. 용량/비용처럼 업그레이드가
            // 건드리는 수치는 애초에 Reset()이 손대지 않는 필드라 그대로 남는다.
            Assert.AreEqual(0, inventory.Items.Count);
            Assert.AreEqual(6, inventory.Capacity);
            Assert.AreEqual(15, costSettings.BasicAnalysisCost);
        }
    }
}
