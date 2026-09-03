using System.Collections.Generic;
using System.Linq;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using GameName.Core.Restoration;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 복원도는 색마다 하나씩 있는 추리 지원 마인드맵이다.
    //
    //   · 자동: 추출로 색이 드러나면 그 색 뿌리와 단서 노드가 잠긴 간선으로
    //     채워진다(RestorationBoardExtractionListener).
    //   · 수동: 플레이어가 그 위에 자유 노드·연결을 얹는다. 자동으로 생긴 것은
    //     지우거나 이름을 바꿀 수 없고, 배치(이동)만 모두에게 열려 있다.
    //
    // 런 전체에 걸쳐 살아 방이 바뀌어도 리셋되지 않는다.
    public class RestorationBoardTests
    {
        private static readonly MemoryRoomId TheRoom = new MemoryRoomId("room-1");

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly RestorationBoard Board;

            public Fixture()
            {
                Board = new RestorationBoard(Bus);

                var placements = new List<CluePlacement>
                {
                    Placement("clue-red-1", "붉은 리본", MemoryColor.Red),
                    Placement("clue-red-2", "붉은 편지", MemoryColor.Red),
                    Placement("clue-blue-1", "파란 표", MemoryColor.Blue),
                };
                var tracker = new MemoryRoomClueTracker(placements);

                _ = new RestorationBoardExtractionListener(Board, tracker, Bus);
            }

            private static CluePlacement Placement(string id, string name, MemoryColor color) =>
                new CluePlacement(
                    TheRoom,
                    new ClueDefinition(
                        new ClueId(id), ClueKind.FloorObject, name, new CluePositionRatio(0.5f), color));

            // 추출로 색이 드러난 상황을 흉내낸다 — 리스너가 이 사건을 듣는다.
            public void RevealColor(MemoryColor color, string clueId) =>
                Bus.Publish(new MemoryColorRevealedEvent(color, new ClueId(clueId)));
        }

        // ── 자동 생성 ─────────────────────────────────────────────────────

        [Test]
        public void 첫_추출에_색_뿌리가_지연_생성되고_두_번째_추출은_뿌리를_늘리지_않는다()
        {
            var fx = new Fixture();
            Assert.IsFalse(fx.Board.HasColorRoot(MemoryColor.Red));

            fx.RevealColor(MemoryColor.Red, "clue-red-1");
            Assert.IsTrue(fx.Board.HasColorRoot(MemoryColor.Red));

            fx.RevealColor(MemoryColor.Red, "clue-red-2");

            var roots = fx.Board.NodesOf(MemoryColor.Red)
                .Where(n => n.Kind == RestorationNodeKind.ColorRoot)
                .ToArray();
            Assert.AreEqual(1, roots.Length);
        }

        [Test]
        public void EnsureColorRoot는_멱등이고_같은_인스턴스를_돌려준다()
        {
            var fx = new Fixture();

            var first = fx.Board.EnsureColorRoot(MemoryColor.Green);
            var second = fx.Board.EnsureColorRoot(MemoryColor.Green);

            Assert.AreEqual(first.Id, second.Id);
            Assert.AreEqual(1, fx.Board.NodesOf(MemoryColor.Green).Count);
        }

        [Test]
        public void 추출된_단서마다_잠긴_노드와_잠긴_간선이_자동으로_생긴다()
        {
            var fx = new Fixture();

            fx.RevealColor(MemoryColor.Red, "clue-red-1");
            fx.RevealColor(MemoryColor.Red, "clue-red-2");

            var clueNodes = fx.Board.NodesOf(MemoryColor.Red)
                .Where(n => n.Kind == RestorationNodeKind.ClueLinked)
                .ToArray();
            Assert.AreEqual(2, clueNodes.Length);
            CollectionAssert.AreEquivalent(
                new[] { "붉은 리본", "붉은 편지" }, clueNodes.Select(n => n.Label).ToArray());

            var lockedEdges = fx.Board.EdgesOf(MemoryColor.Red).Where(e => e.IsLocked).ToArray();
            Assert.AreEqual(2, lockedEdges.Length);

            // 잠긴 간선은 전부 뿌리에서 나간다.
            fx.Board.TryGetColorRoot(MemoryColor.Red, out var root);
            Assert.IsTrue(lockedEdges.All(e => e.From == root.Id));
        }

        [Test]
        public void 자동_노드의_출처_단서와_색이_기록된다()
        {
            var fx = new Fixture();
            fx.RevealColor(MemoryColor.Blue, "clue-blue-1");

            var node = fx.Board.NodesOf(MemoryColor.Blue)
                .Single(n => n.Kind == RestorationNodeKind.ClueLinked);

            Assert.AreEqual(new ClueId("clue-blue-1"), node.SourceClue);
            Assert.AreEqual(MemoryColor.Blue, node.Color);
        }

        // ── 자동 생성물은 편집 불가 ───────────────────────────────────────

        [Test]
        public void 자동_노드는_이름을_바꾸거나_지울_수_없다()
        {
            var fx = new Fixture();
            fx.RevealColor(MemoryColor.Red, "clue-red-1");

            var clueNode = fx.Board.NodesOf(MemoryColor.Red)
                .Single(n => n.Kind == RestorationNodeKind.ClueLinked);
            fx.Board.TryGetColorRoot(MemoryColor.Red, out var root);

            Assert.AreEqual(RestorationEditFailureReason.NodeNotEditable,
                fx.Board.RenamePlayerNode(clueNode.Id, "딴 이름").FailureReason);
            Assert.AreEqual(RestorationEditFailureReason.NodeNotEditable,
                fx.Board.RemovePlayerNode(clueNode.Id).FailureReason);
            Assert.AreEqual(RestorationEditFailureReason.NodeNotEditable,
                fx.Board.RenamePlayerNode(root.Id, "딴 이름").FailureReason);
            Assert.AreEqual(RestorationEditFailureReason.NodeNotEditable,
                fx.Board.RemovePlayerNode(root.Id).FailureReason);
        }

        [Test]
        public void 잠긴_간선은_지울_수_없다()
        {
            var fx = new Fixture();
            fx.RevealColor(MemoryColor.Red, "clue-red-1");

            var locked = fx.Board.EdgesOf(MemoryColor.Red).Single(e => e.IsLocked);

            Assert.AreEqual(RestorationEditFailureReason.EdgeLocked,
                fx.Board.RemovePlayerEdge(locked.Id).FailureReason);
        }

        // ── 플레이어 노드·간선 ────────────────────────────────────────────

        [Test]
        public void 아직_추출_안_한_색에는_플레이어_노드를_놓을_수_없다()
        {
            var fx = new Fixture();

            var result = fx.Board.AddPlayerNode(MemoryColor.Green, "메모", new BoardPosition(1, 1));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(RestorationEditFailureReason.ColorNotStarted, result.FailureReason);
        }

        [Test]
        public void 플레이어_노드는_같은_색끼리_이을_수_있고_다른_색과는_실패한다()
        {
            var fx = new Fixture();
            fx.RevealColor(MemoryColor.Red, "clue-red-1");
            fx.RevealColor(MemoryColor.Blue, "clue-blue-1");

            var redA = fx.Board.AddPlayerNode(MemoryColor.Red, "붉은 메모 A", default).Node;
            var redB = fx.Board.AddPlayerNode(MemoryColor.Red, "붉은 메모 B", default).Node;
            var blue = fx.Board.AddPlayerNode(MemoryColor.Blue, "파란 메모", default).Node;

            var sameColor = fx.Board.AddPlayerEdge(redA.Id, redB.Id);
            Assert.IsTrue(sameColor.Succeeded);
            Assert.IsFalse(sameColor.Edge.IsLocked);

            var crossColor = fx.Board.AddPlayerEdge(redA.Id, blue.Id);
            Assert.IsFalse(crossColor.Succeeded);
            Assert.AreEqual(RestorationEditFailureReason.ColorMismatch, crossColor.FailureReason);
        }

        [Test]
        public void 플레이어_노드는_자동_단서_노드와도_이을_수_있다()
        {
            var fx = new Fixture();
            fx.RevealColor(MemoryColor.Red, "clue-red-1");
            var clueNode = fx.Board.NodesOf(MemoryColor.Red)
                .Single(n => n.Kind == RestorationNodeKind.ClueLinked);

            var memo = fx.Board.AddPlayerNode(MemoryColor.Red, "이 리본은 유키 것", default).Node;

            var edge = fx.Board.AddPlayerEdge(memo.Id, clueNode.Id);
            Assert.IsTrue(edge.Succeeded);
            Assert.IsFalse(edge.Edge.IsLocked);
        }

        [Test]
        public void 플레이어_노드를_지우면_거기_붙은_플레이어_간선도_함께_사라진다()
        {
            var fx = new Fixture();
            fx.RevealColor(MemoryColor.Red, "clue-red-1");

            var a = fx.Board.AddPlayerNode(MemoryColor.Red, "A", default).Node;
            var b = fx.Board.AddPlayerNode(MemoryColor.Red, "B", default).Node;
            var edge = fx.Board.AddPlayerEdge(a.Id, b.Id).Edge;

            var removed = new List<RestorationEdgeRemovedEvent>();
            fx.Bus.Subscribe<RestorationEdgeRemovedEvent>(removed.Add);

            var result = fx.Board.RemovePlayerNode(a.Id);

            Assert.IsTrue(result.Succeeded);
            Assert.IsFalse(fx.Board.TryGetNode(a.Id, out _));
            Assert.IsFalse(fx.Board.TryGetEdge(edge.Id, out _));
            Assert.IsTrue(fx.Board.TryGetNode(b.Id, out _));
            Assert.AreEqual(1, removed.Count);
            Assert.AreEqual(edge.Id, removed[0].EdgeId);
        }

        [Test]
        public void 노드_이동은_뿌리든_단서든_플레이어_노드든_전부_성공한다()
        {
            var fx = new Fixture();
            fx.RevealColor(MemoryColor.Red, "clue-red-1");

            fx.Board.TryGetColorRoot(MemoryColor.Red, out var root);
            var clueNode = fx.Board.NodesOf(MemoryColor.Red)
                .Single(n => n.Kind == RestorationNodeKind.ClueLinked);
            var memo = fx.Board.AddPlayerNode(MemoryColor.Red, "메모", default).Node;

            var target = new BoardPosition(4.5f, -2f);
            Assert.IsTrue(fx.Board.MoveNode(root.Id, target).Succeeded);
            Assert.IsTrue(fx.Board.MoveNode(clueNode.Id, target).Succeeded);
            Assert.IsTrue(fx.Board.MoveNode(memo.Id, target).Succeeded);

            fx.Board.TryGetNode(clueNode.Id, out var movedClue);
            Assert.AreEqual(target, movedClue.Position);
        }

        [Test]
        public void 플레이어_노드_이름_변경은_성공하고_사건을_낸다()
        {
            var fx = new Fixture();
            fx.RevealColor(MemoryColor.Red, "clue-red-1");
            var memo = fx.Board.AddPlayerNode(MemoryColor.Red, "초안", default).Node;

            var renamed = new List<RestorationNodeRenamedEvent>();
            fx.Bus.Subscribe<RestorationNodeRenamedEvent>(renamed.Add);

            Assert.IsTrue(fx.Board.RenamePlayerNode(memo.Id, "유키의 물건").Succeeded);
            fx.Board.TryGetNode(memo.Id, out var after);
            Assert.AreEqual("유키의 물건", after.Label);
            Assert.AreEqual(1, renamed.Count);
        }

        [Test]
        public void 한_노드를_자기_자신에게_이으면_실패한다()
        {
            var fx = new Fixture();
            fx.RevealColor(MemoryColor.Red, "clue-red-1");
            var memo = fx.Board.AddPlayerNode(MemoryColor.Red, "메모", default).Node;

            Assert.AreEqual(RestorationEditFailureReason.SelfLoop,
                fx.Board.AddPlayerEdge(memo.Id, memo.Id).FailureReason);
        }

        // ── 스코프 ────────────────────────────────────────────────────────

        [Test]
        public void 방이_바뀌어도_복원도_내용은_유지된다()
        {
            var fx = new Fixture();
            fx.RevealColor(MemoryColor.Red, "clue-red-1");
            var memo = fx.Board.AddPlayerNode(MemoryColor.Red, "메모", default).Node;
            var edge = fx.Board.AddPlayerEdge(
                memo.Id,
                fx.Board.NodesOf(MemoryColor.Red).Single(n => n.Kind == RestorationNodeKind.ClueLinked).Id).Edge;

            fx.Bus.Publish(new RoomStartedEvent(new MemoryRoomId("room-2"), 1));

            Assert.IsTrue(fx.Board.HasColorRoot(MemoryColor.Red));
            Assert.IsTrue(fx.Board.TryGetNode(memo.Id, out _));
            Assert.IsTrue(fx.Board.TryGetEdge(edge.Id, out _));
            Assert.AreEqual(3, fx.Board.NodesOf(MemoryColor.Red).Count); // 뿌리 + 단서 + 메모
        }
    }
}
