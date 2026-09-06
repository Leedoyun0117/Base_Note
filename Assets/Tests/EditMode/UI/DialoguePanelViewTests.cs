using System;
using System.Collections.Generic;
using System.Linq;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Memories;
using GameName.UI.MemoryRoom.Dialogue;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // 대화 패널 View의 몫: 원문을 조각으로 잘라, 조각마다 지금 풀렸는지 물어
    // 원문 또는 대체 표기를 그린다. 마스크 구간만 클릭을 받는다.
    //
    // 컨트롤러 테스트는 이 렌더링을 페이크로 건너뛰므로, 여기서 실제
    // VisualElement 트리로 확인한다.
    public class DialoguePanelViewTests
    {
        private sealed class FakeMask : ICensorMaskFormatter
        {
            public string FormatMask(MemoryColor color) => $"<{color}>";
        }

        private static VisualElement MakeRoot()
        {
            var root = new VisualElement();
            root.Add(new Label { name = "dialogue-speaker" });
            root.Add(new VisualElement { name = "dialogue-body" });
            root.Add(new VisualElement { name = "dialogue-choices" });
            root.Add(new Label { name = "dialogue-notice" });

            var prompt = new VisualElement { name = "dialogue-unlock-prompt" };
            prompt.Add(new Label { name = "dialogue-unlock-text" });
            prompt.Add(new VisualElement { name = "dialogue-unlock-options" });
            prompt.Add(new Button { name = "dialogue-unlock-cancel" });
            root.Add(prompt);

            return root;
        }

        private sealed class Fixture
        {
            public readonly VisualElement Root = MakeRoot();
            public readonly CensorUnlockLog Resolver = new CensorUnlockLog();
            public readonly DialoguePanelView View;

            public Fixture()
            {
                View = new DialoguePanelView(Root, new CensoredTextParser(), Resolver, new FakeMask());
            }

            public VisualElement Body => Root.Q<VisualElement>("dialogue-body");
            public VisualElement Choices => Root.Q<VisualElement>("dialogue-choices");
            public Label Notice => Root.Q<Label>("dialogue-notice");
            public VisualElement Prompt => Root.Q<VisualElement>("dialogue-unlock-prompt");
            public VisualElement PromptOptions => Root.Q<VisualElement>("dialogue-unlock-options");
        }

        [Test]
        public void 가려진_구간은_대체_표기로_그리고_마스크_클래스를_붙인다()
        {
            var fx = new Fixture();

            fx.View.SetLine("화자", "우리가 [[B:k:해변의 집]]에서.");

            Assert.AreEqual(3, fx.Body.childCount, "조각 셋(평문·검열·평문)이 각각 Label로 그려져야 한다.");
            var mask = (Label)fx.Body[1];
            Assert.AreEqual("<Blue>", mask.text);
            Assert.IsTrue(mask.ClassListContains("dialogue-segment--mask"));
        }

        [Test]
        public void 풀린_구간은_원문으로_그리고_클릭을_받지_않는다()
        {
            var fx = new Fixture();
            fx.Resolver.Record(new CensorKey("k"));

            fx.View.SetLine("화자", "우리가 [[B:k:해변의 집]]에서.");

            var middle = (Label)fx.Body[1];
            Assert.AreEqual("해변의 집", middle.text);
            Assert.IsFalse(middle.ClassListContains("dialogue-segment--mask"));
            Assert.AreEqual(PickingMode.Ignore, middle.pickingMode);
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
        public void ClueSelection_줄은_단서_버튼과_넘어가기_버튼을_그린다()
        {
            var fx = new Fixture();

            fx.View.SetClueSelection(new[]
            {
                new KeyValuePair<ClueId, string>(new ClueId("clue-1"), "교복 리본"),
                new KeyValuePair<ClueId, string>(new ClueId("clue-2"), "부치지 못한 편지"),
            });

            var buttons = fx.Choices.Children().OfType<Button>().Select(b => b.text).ToArray();
            CollectionAssert.Contains(buttons, "교복 리본");
            CollectionAssert.Contains(buttons, "부치지 못한 편지");
            Assert.AreEqual(3, buttons.Length, "단서 둘 + 넘어가기 하나여야 한다.");
        }

        [Test]
        public void 제시_팝업과_안내는_문구가_있을_때만_보인다()
        {
            var fx = new Fixture();

            Assert.AreEqual(DisplayStyle.None, fx.Prompt.style.display.value);

            fx.View.ShowUnlockPrompt("제시할 기억을 고르세요.", Array.Empty<KeyValuePair<ClueId, string>>());
            Assert.AreEqual(DisplayStyle.Flex, fx.Prompt.style.display.value);

            fx.View.HideUnlockPrompt();
            Assert.AreEqual(DisplayStyle.None, fx.Prompt.style.display.value);

            fx.View.SetNotice(null);
            Assert.AreEqual(DisplayStyle.None, fx.Notice.style.display.value);
            fx.View.SetNotice("제시할 기억이 없습니다.");
            Assert.AreEqual(DisplayStyle.Flex, fx.Notice.style.display.value);
        }

        [Test]
        public void 제시_팝업은_기억마다_버튼을_그린다()
        {
            var fx = new Fixture();

            fx.View.ShowUnlockPrompt("제시할 기억을 고르세요.", new[]
            {
                new KeyValuePair<ClueId, string>(new ClueId("clue-1"), "낡은 모포 (파랑)"),
                new KeyValuePair<ClueId, string>(new ClueId("clue-2"), "부치지 못한 편지 (초록)"),
            });

            var buttons = fx.PromptOptions.Children().OfType<Button>().Select(b => b.text).ToArray();
            Assert.AreEqual(2, buttons.Length);
            CollectionAssert.Contains(buttons, "낡은 모포 (파랑)");
            CollectionAssert.Contains(buttons, "부치지 못한 편지 (초록)");
        }
    }
}
