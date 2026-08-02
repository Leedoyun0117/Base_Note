using GameName.Core.Ampoules;
using GameName.Core.Emotions;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using GameName.Core.Validation;
using GameName.UI.Perfumery;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // 0-1 검증: 화면 전환(SetActive)은 Bootstrap이 매번 컨트롤러를 새로
    // 만들고 버리는 것과 같다. 예전에는 제작 대기열이 컨트롤러 안의 private
    // List였으므로 이 재생성 한 번으로 대기열 내용이 사라졌다. 지금은
    // IAmpouleCraftingQueue(Core, GameSession이 들고 있음)가 대기열을 대신
    // 들고 있으므로, 컨트롤러를 새로 만들어도 같은 큐 인스턴스를 다시
    // 넘기면 내용이 그대로 남아야 한다 — 그 사실을 여기서 증명한다.
    public class PerfumeryCompositionQueuePersistenceTests
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

        private static PerfumeryCompositionPanelController MakeController(
            IAmpouleCraftingQueue queue, IAmpouleStorage storage)
        {
            var view = new PerfumeryCompositionPanelView(MakeCompositionRoot());
            var compositionValidator = new ScentCompositionValidator(
                new EmotionCompositionPolicy(
                    minSupportingEmotionCount: 1, maxSupportingEmotionCount: 4, allowSupportingEmotionSameAsBase: false));
            var costSettings = new MentalityCostSettings(
                initialMentality: 100, maxMentality: 100,
                memoryRoomMoveCost: 1, basicAnalysisCost: 20, advancedAnalysisCost: 30,
                ampouleCraftingCost: 8, memoryRoomFullRestorationRecovery: 20);

            return new PerfumeryCompositionPanelController(view, compositionValidator, storage, queue, costSettings);
        }

        [Test]
        public void 컨트롤러를_새로_만들어도_같은_대기열_인스턴스라면_내용이_유지된다()
        {
            var queue = new AmpouleCraftingQueue();
            var storage = new AmpouleStorage(new AmpouleStorageSettings(5));

            queue.Add(new AmpouleCraftingRequest(
                new MemoryRoomId("room-1"),
                new Scent(EmotionType.Joy, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 3) }))));

            // 첫 번째 화면 진입(OnEnable)을 흉내낸다.
            var firstController = MakeController(queue, storage);
            Assert.AreEqual(1, firstController.Queue.Count);

            // 화면 전환(SetActive false->true)으로 컨트롤러가 버려지고 다시
            // 만들어지는 상황을 흉내낸다 — Dispose 후 같은 queue로 새 컨트롤러를
            // 만든다.
            firstController.Dispose();
            var secondController = MakeController(queue, storage);

            Assert.AreEqual(1, secondController.Queue.Count);
            Assert.AreEqual(new MemoryRoomId("room-1"), secondController.Queue[0].TargetRoomId);
        }
    }
}
