using System;
using System.Collections.Generic;
using System.Linq;
using GameName.Core.Events;
using GameName.Core.Memories;
using GameName.Core.Restoration;
using GameName.UI.Restoration;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // 복원도 컨트롤러는 규칙을 하나도 계산하지 않는다 — 6종 이벤트를 받아
    // 캔버스를 갱신하고, 캔버스에서 온 행동을 9개 mutator로 넘긴다. 여기서는
    // 가짜 View + 실제 RestorationBoard로 그 오감만 고정한다(Phase 8 패턴).
    public class RestorationScreenControllerTests
    {
        private sealed class FakeView : IRestorationCanvasView
        {
#pragma warning disable CS0067
            public event Action<RestorationNodeId, BoardPosition> NodeDragCommitted;
            public event Action<RestorationNodeId, string> NodeRenameCommitted;
            public event Action<MemoryColor, string> PlayerNodeRequested;
            public event Action<RestorationNodeId, RestorationNodeId> EdgeRequested;
            public event Action<RestorationNodeId> PlayerNodeDeleteRequested;
            public event Action<RestorationEdgeId> PlayerEdgeDeleteRequested;
#pragma warning restore CS0067

            public readonly List<RestorationNode> Added = new List<RestorationNode>();
            public readonly List<RestorationNodeId> Removed = new List<RestorationNodeId>();
            public readonly List<KeyValuePair<RestorationNodeId, BoardPosition>> Positioned =
                new List<KeyValuePair<RestorationNodeId, BoardPosition>>();
            public readonly List<KeyValuePair<RestorationNodeId, string>> Labeled =
                new List<KeyValuePair<RestorationNodeId, string>>();
            public readonly List<RestorationEdge> EdgesAdded = new List<RestorationEdge>();
            public readonly List<RestorationEdgeId> EdgesRemoved = new List<RestorationEdgeId>();
            public readonly List<string> Notices = new List<string>();
            public readonly List<RestorationNodeId> Focused = new List<RestorationNodeId>();
            public IReadOnlyList<RestorationClusterInfo> Clusters = new List<RestorationClusterInfo>();
            public int ClearCount;
            public int ResetConnectCount;

            public void Clear()
            {
                ClearCount++;
                Added.Clear();
                EdgesAdded.Clear();
            }

            public void SetColorClusters(IReadOnlyList<RestorationClusterInfo> clusters) => Clusters = clusters;
            public void AddNode(RestorationNode node) => Added.Add(node);
            public void RemoveNode(RestorationNodeId nodeId) => Removed.Add(nodeId);
            public void UpdateNodePosition(RestorationNodeId nodeId, BoardPosition position) =>
                Positioned.Add(new KeyValuePair<RestorationNodeId, BoardPosition>(nodeId, position));
            public void UpdateNodeLabel(RestorationNodeId nodeId, string label) =>
                Labeled.Add(new KeyValuePair<RestorationNodeId, string>(nodeId, label));
            public void FocusNode(RestorationNodeId nodeId) => Focused.Add(nodeId);
            public void AddEdge(RestorationEdge edge) => EdgesAdded.Add(edge);
            public void RemoveEdge(RestorationEdgeId edgeId) => EdgesRemoved.Add(edgeId);
            public void ShowNotice(string message) => Notices.Add(message);
            public void ResetConnectSelection() => ResetConnectCount++;

            public void RaiseDrag(RestorationNodeId id, BoardPosition p) => NodeDragCommitted?.Invoke(id, p);
            public void RaiseRename(RestorationNodeId id, string label) => NodeRenameCommitted?.Invoke(id, label);
            public void RaisePlayerNode(MemoryColor c, string label) => PlayerNodeRequested?.Invoke(c, label);
            public void RaiseEdge(RestorationNodeId a, RestorationNodeId b) => EdgeRequested?.Invoke(a, b);
            public void RaiseNodeDelete(RestorationNodeId id) => PlayerNodeDeleteRequested?.Invoke(id);
        }

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly RestorationBoard Board;
            public readonly FakeView View = new FakeView();
            public readonly RestorationScreenController Controller;

            public Fixture()
            {
                Board = new RestorationBoard(Bus);
                Controller = new RestorationScreenController(
                    View, Board, Board, c => c.ToString(), Bus);
            }

            // 추출 리스너를 거치지 않고 자동 생성만 흉내낸다.
            public void RevealClue(MemoryColor color, string clueId, string label)
            {
                Board.EnsureColorRoot(color);
                Board.AddClueNode(color, new GameName.Core.Clues.ClueId(clueId), label);
            }
        }

        [Test]
        public void 색_루트와_단서_노드와_잠긴_간선이_캔버스에_더해진다()
        {
            var fx = new Fixture();

            fx.RevealClue(MemoryColor.Blue, "clue-blanket", "낡은 모포");

            Assert.AreEqual(2, fx.View.Added.Count(n => n.Color == MemoryColor.Blue));
            Assert.IsTrue(fx.View.Added.Any(n => n.Kind == RestorationNodeKind.ColorRoot));
            Assert.IsTrue(fx.View.Added.Any(n => n.Kind == RestorationNodeKind.ClueLinked && n.Label == "낡은 모포"));
            Assert.AreEqual(1, fx.View.EdgesAdded.Count(e => e.IsLocked));
        }

        [Test]
        public void 자동_노드는_화면이_첫_자리를_잡아_MoveNode로_Core에_적는다()
        {
            var fx = new Fixture();

            fx.RevealClue(MemoryColor.Red, "clue-x", "무엇");

            // 원점이 아닌 자리로 옮겨졌다.
            fx.Board.TryGetColorRoot(MemoryColor.Red, out var root);
            Assert.AreNotEqual(new BoardPosition(0f, 0f), root.Position);
            Assert.IsTrue(fx.View.Positioned.Any(p => p.Key == root.Id));
        }

        [Test]
        public void 루트가_생긴_색만_클러스터_목록에_오르고_라벨은_색_이름_함수로_채운다()
        {
            var fx = new Fixture();

            fx.RevealClue(MemoryColor.Green, "clue-g", "풀");

            Assert.AreEqual(1, fx.View.Clusters.Count);
            Assert.AreEqual(MemoryColor.Green, fx.View.Clusters[0].Color);
            Assert.AreEqual("Green", fx.View.Clusters[0].Label);
        }

        [Test]
        public void 드래그_종료는_MoveNode를_태운다()
        {
            var fx = new Fixture();
            fx.RevealClue(MemoryColor.Red, "clue-x", "무엇");
            var node = fx.Board.NodesOf(MemoryColor.Red).First(n => n.Kind == RestorationNodeKind.ClueLinked);

            fx.View.RaiseDrag(node.Id, new BoardPosition(123f, 456f));

            fx.Board.TryGetNode(node.Id, out var moved);
            Assert.AreEqual(new BoardPosition(123f, 456f), moved.Position);
        }

        [Test]
        public void 플레이어_노드_추가_요청은_AddPlayerNode를_태우고_캔버스에_뜬다()
        {
            var fx = new Fixture();
            fx.RevealClue(MemoryColor.Blue, "clue-b", "표");
            fx.View.Added.Clear();

            fx.View.RaisePlayerNode(MemoryColor.Blue, "유키의 메모");

            Assert.IsTrue(fx.Board.NodesOf(MemoryColor.Blue)
                .Any(n => n.Kind == RestorationNodeKind.PlayerAuthored && n.Label == "유키의 메모"));
            Assert.IsTrue(fx.View.Added.Any(n => n.Kind == RestorationNodeKind.PlayerAuthored));
        }

        [Test]
        public void 아직_추출_안_한_색에_메모_추가_요청하면_안내만_뜬다()
        {
            var fx = new Fixture();

            fx.View.RaisePlayerNode(MemoryColor.Green, "메모");

            Assert.IsFalse(fx.Board.HasColorRoot(MemoryColor.Green));
            Assert.IsTrue(fx.View.Notices.Any(n => !string.IsNullOrEmpty(n)));
        }

        [Test]
        public void 다른_색_노드_연결_요청은_실패로_안내되고_선택이_초기화된다()
        {
            var fx = new Fixture();
            fx.RevealClue(MemoryColor.Red, "clue-r", "빨강");
            fx.RevealClue(MemoryColor.Blue, "clue-b", "파랑");
            var red = fx.Board.NodesOf(MemoryColor.Red).First(n => n.Kind == RestorationNodeKind.ClueLinked);
            var blue = fx.Board.NodesOf(MemoryColor.Blue).First(n => n.Kind == RestorationNodeKind.ClueLinked);

            fx.View.RaiseEdge(red.Id, blue.Id);

            Assert.IsEmpty(fx.Board.EdgesOf(MemoryColor.Red).Where(e => !e.IsLocked));
            Assert.IsTrue(fx.View.Notices.Any(n => !string.IsNullOrEmpty(n)));
            Assert.AreEqual(1, fx.View.ResetConnectCount);
        }

        [Test]
        public void 같은_색_노드_연결_요청은_간선을_만들고_캔버스에_뜬다()
        {
            var fx = new Fixture();
            fx.RevealClue(MemoryColor.Red, "clue-r", "빨강");
            var clue = fx.Board.NodesOf(MemoryColor.Red).First(n => n.Kind == RestorationNodeKind.ClueLinked);
            var memoResult = fx.Board.AddPlayerNode(MemoryColor.Red, "메모", new BoardPosition(5f, 5f));
            fx.View.EdgesAdded.Clear();

            fx.View.RaiseEdge(clue.Id, memoResult.Node.Id);

            Assert.AreEqual(1, fx.Board.EdgesOf(MemoryColor.Red).Count(e => !e.IsLocked));
            Assert.AreEqual(1, fx.View.EdgesAdded.Count(e => !e.IsLocked));
        }

        [Test]
        public void 이름_변경_요청은_RenamePlayerNode를_태운다()
        {
            var fx = new Fixture();
            fx.RevealClue(MemoryColor.Red, "clue-r", "빨강");
            var memo = fx.Board.AddPlayerNode(MemoryColor.Red, "초안", new BoardPosition(5f, 5f)).Node;

            fx.View.RaiseRename(memo.Id, "유키의 물건");

            fx.Board.TryGetNode(memo.Id, out var after);
            Assert.AreEqual("유키의 물건", after.Label);
            Assert.IsTrue(fx.View.Labeled.Any(l => l.Key == memo.Id && l.Value == "유키의 물건"));
        }

        [Test]
        public void 잠긴_노드_이름_변경_요청은_거부되고_안내가_뜬다()
        {
            var fx = new Fixture();
            fx.RevealClue(MemoryColor.Red, "clue-r", "빨강");
            var clue = fx.Board.NodesOf(MemoryColor.Red).First(n => n.Kind == RestorationNodeKind.ClueLinked);

            fx.View.RaiseRename(clue.Id, "딴 이름");

            fx.Board.TryGetNode(clue.Id, out var after);
            Assert.AreEqual("빨강", after.Label);
            Assert.IsTrue(fx.View.Notices.Any(n => !string.IsNullOrEmpty(n)));
        }

        [Test]
        public void Refresh는_캔버스를_비우고_리더의_모든_노드와_간선을_다시_그린다()
        {
            var fx = new Fixture();
            fx.RevealClue(MemoryColor.Red, "clue-r", "빨강");
            fx.RevealClue(MemoryColor.Blue, "clue-b", "파랑");

            fx.View.Added.Clear();
            fx.View.EdgesAdded.Clear();
            fx.Controller.Refresh();

            Assert.AreEqual(1, fx.View.ClearCount);
            Assert.AreEqual(4, fx.View.Added.Count); // 색마다 뿌리 + 단서 노드
            Assert.AreEqual(2, fx.View.EdgesAdded.Count); // 색마다 잠긴 간선
        }

        [Test]
        public void 노드_제거_이벤트는_캔버스에서도_지운다()
        {
            var fx = new Fixture();
            fx.RevealClue(MemoryColor.Red, "clue-r", "빨강");
            var memo = fx.Board.AddPlayerNode(MemoryColor.Red, "메모", new BoardPosition(5f, 5f)).Node;

            fx.View.RaiseNodeDelete(memo.Id);

            Assert.IsFalse(fx.Board.TryGetNode(memo.Id, out _));
            Assert.Contains(memo.Id, fx.View.Removed);
        }

        [Test]
        public void 삭제_요청은_RemovePlayerNode를_태운다()
        {
            var fx = new Fixture();
            fx.RevealClue(MemoryColor.Blue, "clue-b", "표");
            var memo = fx.Board.AddPlayerNode(MemoryColor.Blue, "유키의 메모", new BoardPosition(9f, 9f)).Node;

            fx.View.RaiseNodeDelete(memo.Id);

            Assert.IsFalse(fx.Board.TryGetNode(memo.Id, out _), "Core에서 지워지지 않았다.");
        }

        [Test]
        public void 삭제한_메모는_재진입_Refresh에서도_다시_나타나지_않는다()
        {
            var fx = new Fixture();
            fx.RevealClue(MemoryColor.Green, "clue-g", "풀");
            var memo = fx.Board.AddPlayerNode(MemoryColor.Green, "메모", new BoardPosition(3f, 3f)).Node;
            fx.View.RaiseNodeDelete(memo.Id);

            // 화면을 나갔다 다시 들어온 것과 같다.
            fx.View.Added.Clear();
            fx.Controller.Refresh();

            Assert.IsFalse(fx.View.Added.Any(n => n.Id == memo.Id), "삭제한 메모가 재진입 후 다시 그려졌다.");
            Assert.IsTrue(fx.View.Added.Any(n => n.Kind == RestorationNodeKind.ColorRoot), "다른 노드는 정상적으로 다시 그려져야 한다.");
        }

        [Test]
        public void 플레이어_메모를_추가하면_그_노드로_뷰를_이동시킨다()
        {
            var fx = new Fixture();
            fx.RevealClue(MemoryColor.Blue, "clue-b", "표");
            fx.View.Focused.Clear();

            fx.View.RaisePlayerNode(MemoryColor.Blue, "유키의 메모");

            var memo = fx.Board.NodesOf(MemoryColor.Blue)
                .Single(n => n.Kind == RestorationNodeKind.PlayerAuthored);
            Assert.Contains(memo.Id, fx.View.Focused);
        }

        [Test]
        public void 자동_노드_등장에는_뷰를_이동시키지_않는다()
        {
            var fx = new Fixture();

            fx.RevealClue(MemoryColor.Red, "clue-r", "리본"); // 루트 + 단서 노드 자동 생성

            CollectionAssert.IsEmpty(fx.View.Focused, "추출 자동 노드에 뷰가 따라가면 안 된다.");
        }
    }
}
