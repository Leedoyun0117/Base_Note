using System;
using System.Collections.Generic;
using GameName.Core.Events;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class EventBusTests
    {
        // 이 계층 전용 이벤트에 얽매이지 않고 IEventBus가 임의의 이벤트 타입에
        // 대해 동작함을 보여주기 위한 테스트 전용 이벤트.
        private readonly struct SampleEvent
        {
            public int Value { get; }

            public SampleEvent(int value)
            {
                Value = value;
            }
        }

        // 예외 처리 정책이 실제로 호출되는지 확인하기 위한 테스트 전용 스파이.
        private sealed class RecordingExceptionHandler : IEventExceptionHandler
        {
            public readonly List<Exception> Handled = new List<Exception>();

            public void Handle(Type eventType, Exception exception)
            {
                Handled.Add(exception);
            }
        }

        [Test]
        public void 발행_도중_구독을_해제해도_예외가_나지_않는다()
        {
            var bus = new EventBus(new NoOpEventExceptionHandler());
            var callOrder = new List<string>();
            IDisposable subscriptionB = null;

            bus.Subscribe<SampleEvent>(e =>
            {
                callOrder.Add("A");
                subscriptionB.Dispose();
            });
            subscriptionB = bus.Subscribe<SampleEvent>(e => callOrder.Add("B"));

            Assert.DoesNotThrow(() => bus.Publish(new SampleEvent(1)));
            CollectionAssert.AreEqual(new[] { "A", "B" }, callOrder);

            callOrder.Clear();
            bus.Publish(new SampleEvent(2));

            // B는 첫 발행 도중 해제되었으므로 두 번째 발행부터는 호출되지 않는다.
            CollectionAssert.AreEqual(new[] { "A" }, callOrder);
        }

        [Test]
        public void 한_구독자가_예외를_던져도_나머지_구독자는_알림을_받고_발행은_예외_없이_끝난다()
        {
            var exceptionHandler = new RecordingExceptionHandler();
            var bus = new EventBus(exceptionHandler);
            var secondCalled = false;

            bus.Subscribe<SampleEvent>(e => throw new InvalidOperationException("boom"));
            bus.Subscribe<SampleEvent>(e => secondCalled = true);

            Assert.DoesNotThrow(() => bus.Publish(new SampleEvent(1)));
            Assert.IsTrue(secondCalled);
            Assert.AreEqual(1, exceptionHandler.Handled.Count);
        }

        [Test]
        public void 구독_해제_후에는_더_이상_알림을_받지_않는다()
        {
            var bus = new EventBus(new NoOpEventExceptionHandler());
            var callCount = 0;

            var subscription = bus.Subscribe<SampleEvent>(e => callCount++);
            bus.Publish(new SampleEvent(1));
            subscription.Dispose();
            bus.Publish(new SampleEvent(2));

            Assert.AreEqual(1, callCount);
        }
    }
}
