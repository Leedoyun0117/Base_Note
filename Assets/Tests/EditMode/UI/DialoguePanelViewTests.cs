using System.Collections.Generic;
using System.Linq;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.UI.MemoryRoom.Dialogue;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // 대화 패널 View의 몫: 화자·원문을 그리고, 선택지/단서 버튼을 만든다.
    //
    // 컨트롤러 테스트는 이 렌더링을 페이크로 건너뛰므로, 여기서 실제
    // VisualElement 트리로 확인한다.
    public class DialoguePanelViewTests
    {
        private static VisualElement MakeRoot()
        {
            var root = new VisualElement();
            root.Add(new Label { name = "dialogue-speaker" });
            root.Add(new VisualElement { name = "dialogue-body" });
            root.Add(new VisualElement { name = "dialogue-choices" });
            root.Add(new Label { name = "dialogue-notice" });
            return root;
        }

        private sealed class Fixture
        {
            public readonly VisualElement Root = MakeRoot();
            public readonly DialoguePanelView View;

            public Fixture()
            {
                View = new DialoguePanelView(Root);
            }

            public VisualElement Body => Root.Q<VisualElement>("dialogue-body");
            public VisualElement Choices => Root.Q<VisualElement>("dialogue-choices");
            public Label Notice => Root.Q<Label>("dialogue-notice");
        }

        [Test]
        public void 원문은_본문에_한_조각으로_그려진다()
        {
            var fx = new Fixture();

            fx.View.SetLine("화자", "우리가 그 해변의 집에서.");

            Assert.AreEqual(1, fx.Body.childCount);
            Assert.AreEqual("우리가 그 해변의 집에서.", ((Label)fx.Body[0]).text);
            Assert.AreEqual(PickingMode.Ignore, fx.Body[0].pickingMode);
        }

        [Test]
        public void 선택지는_버튼으로_그리고_빈_문구는_최소_표기를_준다()
        {
            var fx = new Fixture();

            fx.View.SetChoices(new[]
            {
                new KeyValuePair<ChoiceId, string>(new ChoiceId("a"), "그건 사실이 아니야"),
                new KeyValuePair<ChoiceId, string>(new ChoiceId("b"), ""),
            });

            Assert.AreEqual(2, fx.Choices.childCount);
            Assert.AreEqual("그건 사실이 아니야", ((Button)fx.Choices[0]).text);
            Assert.AreEqual("(선택)", ((Button)fx.Choices[1]).text);
        }

        [Test]
        public void ClueSelection_줄은_단서마다_답하기_추출_버튼과_넘어가기_버튼을_그린다()
        {
            var fx = new Fixture();

            fx.View.SetClueSelection(new[]
            {
                new SelectableClue(new ClueId("clue-1"), "교복 리본", memoryExtracted: false),
                new SelectableClue(new ClueId("clue-2"), "부치지 못한 편지", memoryExtracted: true),
            });

            var buttons = fx.Choices.Query<Button>().ToList().Select(b => b.text).ToList();
            CollectionAssert.Contains(buttons, "교복 리본");
            CollectionAssert.Contains(buttons, "부치지 못한 편지");
            CollectionAssert.Contains(buttons, "잘 기억나지 않는다");

            // clue-1은 아직 안 추출 → 추출 버튼 있음, clue-2는 추출됨 → 없음.
            Assert.AreEqual(1, buttons.Count(t => t == "기억 추출"));
        }

        [Test]
        public void 안내는_문구가_있을_때만_보인다()
        {
            var fx = new Fixture();

            fx.View.SetNotice(null);
            Assert.AreEqual(DisplayStyle.None, fx.Notice.style.display.value);

            fx.View.SetNotice("제시할 기억이 없습니다.");
            Assert.AreEqual(DisplayStyle.Flex, fx.Notice.style.display.value);
        }
    }
}
