using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace AbsoluteTimer.Tests
{
    [TestFixture]
    public class AbsoluteTimerTests
    {
        static readonly TimeSpan Delay = TimeSpan.FromSeconds(0.1);

        [TestCase(2)]
        [TestCase(5)]
        //[TestCase(120)]
        public void CallsCallbackOnExpectedTime(int seconds)
        {
            var interval = TimeSpan.FromSeconds(seconds);
            var sw = Stopwatch.StartNew();
            Helper(interval,
                afterTick: () => Assert.That(sw.Elapsed, Is.EqualTo(interval).Within(Delay)));
        }

        [Test]
        public void CallsCallbackProperlyOnHistory()
        {
            var sw = Stopwatch.StartNew();
            Helper(TimeSpan.FromSeconds(-1),
                afterTick: () => Assert.That(sw.Elapsed, Is.LessThanOrEqualTo(Delay)));
        }

        [Test]
        public void PassesProperlyState()
        {
            var state = new object();
            Helper(TimeSpan.FromSeconds(0),
                state: state,
                callback: o => Assert.That(o, Is.SameAs(state)));
        }

        static void Helper(TimeSpan interval, object state = null, Action<object> callback = null, Action afterTick = null)
        {
            using (var mre = new ManualResetEvent(false))
            {
                using (var timer = new AbsoluteTimer(DateTime.UtcNow.Add(interval), o =>
                {
                    callback?.Invoke(o);
                    mre.Set();
                }, state))
                {
                    mre.WaitOne();
                    afterTick?.Invoke();
                }
            }
        }
    }
}
