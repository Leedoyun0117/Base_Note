using System;
using System.Collections.Generic;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Journal;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class DialogueProgressorTests
    {
        private static DialogueScript MakeLinearScript()
        {
            var nodes = new List<DialogueNode>
            {
                new DialogueNode(new DialogueLine("A", "첫"), new[] { new DialogueOption(1) }),
                new DialogueNode(new DialogueLine("B", "둘"), new[] { new DialogueOption(2) }),
                new DialogueNode(new DialogueLine("C", "끝"), Array.Empty<DialogueOption>()),
            };
            return new DialogueScript(nodes);
        }

        private static DialogueProgressor MakeProgressor(out EventBus eventBus)
        {
            eventBus = new EventBus(new NoOpEventExceptionHandler());
            return new DialogueProgressor(MakeLinearScript(), eventBus);
        }

        [Test]
        public void 시작하면_0번_노드의_대사를_보여준다()
        {
            var progressor = MakeProgressor(out _);

            Assert.AreEqual("첫", progressor.CurrentLine.Text);
            Assert.IsFalse(progressor.IsFinished);
        }

        [Test]
        public void Advance하면_다음_노드로_넘어간다()
        {
            var progressor = MakeProgressor(out _);

            var succeeded = progressor.Advance(0);

            Assert.IsTrue(succeeded);
            Assert.AreEqual("둘", progressor.CurrentLine.Text);
        }

        [Test]
        public void 마지막_노드는_옵션이_없어_끝난_것으로_판정된다()
        {
            var progressor = MakeProgressor(out _);

            progressor.Advance(0);
            progressor.Advance(0);

            Assert.AreEqual("끝", progressor.CurrentLine.Text);
            Assert.IsTrue(progressor.IsFinished);
        }

        [Test]
        public void 끝난_뒤_Advance를_시도하면_아무_일도_없고_false를_반환한다()
        {
            var progressor = MakeProgressor(out _);
            progressor.Advance(0);
            progressor.Advance(0);

            var succeeded = progressor.Advance(0);

            Assert.IsFalse(succeeded);
            Assert.AreEqual("끝", progressor.CurrentLine.Text);
        }

        [Test]
        public void LoadScript하면_새_대본의_처음_노드로_되돌아간다()
        {
            var progressor = MakeProgressor(out _);
            progressor.Advance(0);
            progressor.Advance(0);

            progressor.LoadScript(MakeLinearScript());

            Assert.AreEqual("첫", progressor.CurrentLine.Text);
            Assert.IsFalse(progressor.IsFinished);
        }

        [Test]
        public void Advance할_때마다_대화_표시_이벤트가_새_줄과_함께_발행된다()
        {
            var progressor = MakeProgressor(out var eventBus);

            DialogueLineShownEvent? received = null;
            using (eventBus.Subscribe<DialogueLineShownEvent>(e => received = e))
            {
                progressor.Advance(0);
            }

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual("둘", received.Value.Line.Text);
        }

        [Test]
        public void LoadScript하면_처음_줄의_표시_이벤트가_한_번만_발행된다()
        {
            var progressor = MakeProgressor(out var eventBus);
            progressor.Advance(0);

            var receivedCount = 0;
            using (eventBus.Subscribe<DialogueLineShownEvent>(e => receivedCount++))
            {
                progressor.LoadScript(MakeLinearScript());
            }

            Assert.AreEqual(1, receivedCount);
        }

        [Test]
        public void 범위를_벗어난_옵션_인덱스는_false를_반환하고_그대로다()
        {
            var progressor = MakeProgressor(out _);

            var succeeded = progressor.Advance(5);

            Assert.IsFalse(succeeded);
            Assert.AreEqual("첫", progressor.CurrentLine.Text);
        }
    }
}
