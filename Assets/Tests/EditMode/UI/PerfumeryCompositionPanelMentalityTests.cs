using GameName.Core.Ampoules;
using GameName.Core.Emotions;
using GameName.Core.Events;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using GameName.Core.Validation;
using GameName.UI.Perfumery;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // 섹션 3 검증: 잔량으로 불가능해진 행동(조향)은 버튼이 스스로 비활성으로
    // 보여야 한다. 이 판단은 화면이 계산하지 않고 Core(MentalityAffordability
    // Calculator)에서 그대로 가져온다 — 여기서는 그 결과가 실제 버튼의
    // enabledSelf에 반영되는지까지 확인한다.
    public class PerfumeryCompositionPanelMentalityTests
    {
        private static VisualElement MakeCompositionRoot()
        {
            var root = new VisualElement();
            root.Add(new VisualElement { name = "base-emotion-slots" });
            root.Add(new VisualElement { name = "supporting-emotion-rows" });
            root.Add(new Label { name = "total-over-required" });
            root.Add(new VisualElement { name = "violations-list" });
            root.Add(new Button { name = "add-to-queue-button" });
            root.Add(new VisualElement { name = "craft-queue-list" });
            root.Add(new Label { name = "craft-cost-notice" });
            root.Add(new Button { name = "craft-queue-button" });
            root.Add(new Label { name = "craft-result-message" });
            return root;
        }

        [Test]
        public void 정신력이_조향_비용보다_적으면_대기열_제작_버튼이_비활성화된다()
        {
            var queue = new AmpouleCraftingQueue();
            var storage = new AmpouleStorage(new AmpouleStorageSettings(5));
            var compositionValidator = new ScentCompositionValidator(
                new EmotionCompositionPolicy(
                    minSupportingEmotionCount: 1, maxSupportingEmotionCount: 4, allowSupportingEmotionSameAsBase: false));
            var costSettings = new MentalityCostSettings(
                initialMentality: 100, maxMentality: 100,
                memoryRoomMoveCost: 1, basicAnalysisCost: 20, advancedAnalysisCost: 30,
                ampouleCraftingCost: 5, memoryRoomFullRestorationRecovery: 20);
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var mentalityGauge = new MentalityGauge(costSettings, eventBus);

            queue.Add(new AmpouleCraftingRequest(
                new MemoryRoomId("room-1"),
                new Scent(EmotionType.Joy, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 3) }))));

            var root = MakeCompositionRoot();
            var view = new PerfumeryCompositionPanelView(root);
            var craftButton = root.Q<Button>("craft-queue-button");

            // 대기열에 항목이 있으니 정신력만 충분하면 활성 상태로 시작해야
            // 한다.
            new PerfumeryCompositionPanelController(
                view, compositionValidator, storage, queue, mentalityGauge, costSettings, eventBus);
            Assert.IsTrue(craftButton.enabledSelf);

            // 잔량을 조향 비용(5)보다 적게 남긴다.
            mentalityGauge.Consume(97); // 잔량 3

            Assert.IsFalse(craftButton.enabledSelf);
        }
    }
}
