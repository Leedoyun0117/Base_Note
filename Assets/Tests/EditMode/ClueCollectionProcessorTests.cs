using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using GameName.Core.Trust;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 신뢰도가 깎여 방이 좁게 보이면 바깥쪽 단서는 못 집는다 — 경계값 포함해서.
    // 활성 방은 RoomStartedEvent가 정하고, 위치는 저작 데이터에서 온다.
    public class ClueCollectionProcessorTests
    {
        private static readonly MemoryRoomId TheRoom = new MemoryRoomId("room-1");

        private static readonly Dictionary<int, float> VisibilityTable = new Dictionary<int, float>
        {
            { 3, 1.0f }, { 2, 0.75f }, { 1, 0.5f }, { 0, 0.5f },
        };

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly TrustGauge Trust;
            public readonly ClueStateStore State;
            public readonly PlayerInventory Inventory;
            public readonly ClueCollectionProcessor Processor;
            public readonly List<ClueCollectedEvent> Collected = new List<ClueCollectedEvent>();

            public Fixture(params (string id, float pos)[] clues)
                : this(16, clues)
            {
            }

            public Fixture(int inventoryCapacity, (string id, float pos)[] clues)
            {
                var defs = new List<ClueDefinition>();
                var placements = new List<CluePlacement>();
                foreach (var c in clues)
                {
                    var def = new ClueDefinition(
                        new ClueId(c.id), ClueKind.Poster, c.id, new CluePositionRatio(c.pos), MemoryColor.Red);
                    defs.Add(def);
                    placements.Add(new CluePlacement(TheRoom, def));
                }

                var room = new RoomDefinition(TheRoom, defs, null, Array.Empty<DialogueLineDefinition>());
                Trust = new TrustGauge(3, Bus);
                State = new ClueStateStore(new[] { room }, Bus);
                Inventory = new PlayerInventory(
                    new InventorySettings(inventoryCapacity), new SharedSlotInventoryPolicy());
                var tracker = new MemoryRoomClueTracker(placements);
                Processor = new ClueCollectionProcessor(
                    State, new CenteredClueAccessPolicy(), Trust, new StepVisibilityPolicy(VisibilityTable),
                    tracker, Inventory, Bus);

                // 실제 배선처럼 투영을 붙인다 — 사전 확인이 없으면 두 번째 수집에서
                // InventoryProjection이 담지 못해 예외를 던지므로, 이 조합이 곧
                // "원인 지점에서 막혔는가"의 검증이다.
                _ = new InventoryProjection(Inventory, tracker, Bus);
                Bus.Subscribe<ClueCollectedEvent>(Collected.Add);

                // 활성 방 진입 — ClueStateStore 시드 + 처리기의 활성 방 설정.
                Bus.Publish(new RoomStartedEvent(TheRoom, 0));
            }

            public ClueCollectionResult Collect(string id) => Processor.Collect(new ClueId(id));
        }

        [Test]
        public void 신뢰_3이면_방_끝의_단서도_집을_수_있다()
        {
            var fx = new Fixture(("edge", 0.98f));

            var result = fx.Collect("edge");

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1, fx.Collected.Count);
            Assert.AreEqual(TheRoom, fx.Collected[0].RoomId);
            Assert.AreEqual(ClueState.Collected, fx.State.GetState(new ClueId("edge")));
        }

        [Test]
        public void 신뢰가_깎이면_바깥쪽_단서는_접근_불가로_거부된다()
        {
            var fx = new Fixture(("outer", 0.2f));
            fx.Trust.Decrease(2); // 3 → 1, v = 0.5, 구간 [0.25, 0.75]

            var result = fx.Collect("outer");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ClueCollectionFailureReason.OutOfView, result.FailureReason);
            Assert.AreEqual(ClueState.Available, fx.State.GetState(new ClueId("outer")));
            CollectionAssert.IsEmpty(fx.Collected);
        }

        [Test]
        public void 신뢰_2의_경계값_0_125에_정확히_걸친_단서는_집을_수_있다()
        {
            var fx = new Fixture(("boundary", 0.125f));
            fx.Trust.Decrease(1); // 3 → 2, v = 0.75, 구간 [0.125, 0.875]

            Assert.IsTrue(fx.Collect("boundary").Succeeded);
        }

        [Test]
        public void 신뢰_1의_경계값_0_25에_정확히_걸친_단서는_집을_수_있다()
        {
            var fx = new Fixture(("boundary", 0.25f));
            fx.Trust.Decrease(2); // 3 → 1, v = 0.5, 구간 [0.25, 0.75]

            Assert.IsTrue(fx.Collect("boundary").Succeeded);
        }

        [Test]
        public void 경계_바로_바깥은_거부된다()
        {
            var fx = new Fixture(("justOut", 0.24f));
            fx.Trust.Decrease(2); // v = 0.5, 구간 [0.25, 0.75]

            Assert.AreEqual(ClueCollectionFailureReason.OutOfView, fx.Collect("justOut").FailureReason);
        }

        [Test]
        public void 이미_수집한_단서는_다시_집을_수_없다()
        {
            var fx = new Fixture(("c", 0.5f));
            Assert.IsTrue(fx.Collect("c").Succeeded);

            var again = fx.Collect("c");

            Assert.IsFalse(again.Succeeded);
            Assert.AreEqual(ClueCollectionFailureReason.NotAvailable, again.FailureReason);
        }

        [Test]
        public void 이미_집은_단서가_가시_밖으로_밀려나도_상태는_그대로다()
        {
            // 접근성 판정은 아직 Available인 단서에게만 묻는다 — 집힌 것을 뺏지 않는다.
            var fx = new Fixture(("c", 0.2f));
            Assert.IsTrue(fx.Collect("c").Succeeded);

            fx.Trust.Decrease(3); // 방이 좁아져 0.2는 이제 가시 밖

            Assert.AreEqual(ClueState.Collected, fx.State.GetState(new ClueId("c")));
        }

        [Test]
        public void 추출_등_다른_용도로_쓰인_단서도_수집이_거부된다()
        {
            var fx = new Fixture(("c", 0.5f));
            fx.State.SetState(new ClueId("c"), ClueState.Collected);
            fx.State.SetState(new ClueId("c"), ClueState.Extracted);

            Assert.AreEqual(ClueCollectionFailureReason.NotAvailable, fx.Collect("c").FailureReason);
        }

        [Test]
        public void 이_방에_없는_단서는_거부된다()
        {
            var fx = new Fixture(("c", 0.5f));

            Assert.AreEqual(ClueCollectionFailureReason.NotAvailable, fx.Collect("clue-다른방").FailureReason);
        }

        [Test]
        public void 가방에_빈_칸이_없으면_수집이_거부되고_상태는_그대로다()
        {
            // 가방 용량 1, 다른 단서 하나가 이미 자리를 차지하고 있다.
            var fx = new Fixture(1, new[] { ("held", 0.5f), ("wanted", 0.5f) });
            Assert.IsTrue(fx.Collect("held").Succeeded);

            var result = fx.Collect("wanted");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ClueCollectionFailureReason.InventoryFull, result.FailureReason);
            Assert.AreEqual(ClueState.Available, fx.State.GetState(new ClueId("wanted")));
            Assert.AreEqual(1, fx.Collected.Count); // held 하나만
        }

        [Test]
        public void 자리를_비우면_거부됐던_단서를_다시_집을_수_있다()
        {
            var fx = new Fixture(1, new[] { ("held", 0.5f), ("wanted", 0.5f) });
            fx.Collect("held");
            Assert.AreEqual(ClueCollectionFailureReason.InventoryFull, fx.Collect("wanted").FailureReason);

            // held를 추출로 소비하면(가방에서 빠짐) 다시 자리가 생긴다.
            fx.State.SetState(new ClueId("held"), ClueState.Extracted);
            fx.Inventory.TryRemove(fx.Inventory.Items[0]);

            Assert.IsTrue(fx.Collect("wanted").Succeeded);
            Assert.AreEqual(ClueState.Collected, fx.State.GetState(new ClueId("wanted")));
        }

        [Test]
        public void 가방이_꽉_차_있어도_이미_추출된_단서는_InventoryFull이_아니라_NotAvailable로_거부된다()
        {
            var fx = new Fixture(0, new[] { ("c", 0.5f) }); // 가방 용량 0 = 항상 만원
            fx.State.SetState(new ClueId("c"), ClueState.Collected);
            fx.State.SetState(new ClueId("c"), ClueState.Extracted);

            var result = fx.Collect("c");

            // 상태 검사가 먼저다 — 만원 여부는 보지도 않는다.
            Assert.AreEqual(ClueCollectionFailureReason.NotAvailable, result.FailureReason);
        }

        [Test]
        public void 가방이_꽉_차_있어도_이미_대화에_쓴_단서는_NotAvailable로_거부된다()
        {
            var fx = new Fixture(0, new[] { ("c", 0.5f) });
            fx.State.SetState(new ClueId("c"), ClueState.Collected);
            fx.State.SetState(new ClueId("c"), ClueState.UsedInDialogue);

            Assert.AreEqual(ClueCollectionFailureReason.NotAvailable, fx.Collect("c").FailureReason);
        }

        [Test]
        public void 가방이_꽉_차_있어도_가시_밖_단서는_OutOfView가_InventoryFull보다_먼저다()
        {
            var fx = new Fixture(0, new[] { ("outer", 0.05f) });
            fx.Trust.Decrease(2); // 방이 좁아져 0.05는 가시 밖

            Assert.AreEqual(ClueCollectionFailureReason.OutOfView, fx.Collect("outer").FailureReason);
        }
    }
}
