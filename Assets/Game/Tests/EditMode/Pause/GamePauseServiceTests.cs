using System;
using System.Collections.Generic;
using NUnit.Framework;
using ValorChronicle.Core.Pause;

namespace ValorChronicle.Tests.EditMode.Pause
{
    public sealed class GamePauseServiceTests
    {
        [Test]
        public void NestedRequestsRestoreCapturedTimeScaleAfterFinalRelease()
        {
            float timeScale = 1.75f;
            var appliedScales = new List<float>();
            var states = new List<bool>();
            var service = new GamePauseService(
                () => timeScale,
                value =>
                {
                    timeScale = value;
                    appliedScales.Add(value);
                });
            service.PauseStateChanged += states.Add;

            Assert.That(service.IsPaused, Is.False);
            Assert.That(service.ActiveRequestCount, Is.Zero);

            IDisposable first = service.RequestPause();
            IDisposable second = service.RequestPause();

            Assert.That(service.IsPaused, Is.True);
            Assert.That(service.ActiveRequestCount, Is.EqualTo(2));
            Assert.That(timeScale, Is.Zero);
            Assert.That(appliedScales, Is.EqualTo(new[] { 0f }));
            Assert.That(states, Is.EqualTo(new[] { true }));

            first.Dispose();
            first.Dispose();
            Assert.That(service.IsPaused, Is.True);
            Assert.That(service.ActiveRequestCount, Is.EqualTo(1));
            Assert.That(timeScale, Is.Zero);

            second.Dispose();
            Assert.That(service.IsPaused, Is.False);
            Assert.That(service.ActiveRequestCount, Is.Zero);
            Assert.That(timeScale, Is.EqualTo(1.75f));
            Assert.That(appliedScales, Is.EqualTo(new[] { 0f, 1.75f }));
            Assert.That(states, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void ReleaseAllInvalidatesOutstandingHandlesAndRestoresOnce()
        {
            float timeScale = 0.5f;
            int setCount = 0;
            var service = new GamePauseService(
                () => timeScale,
                value =>
                {
                    timeScale = value;
                    setCount++;
                });
            IDisposable first = service.RequestPause();
            IDisposable second = service.RequestPause();

            service.ReleaseAll();
            first.Dispose();
            second.Dispose();
            service.ReleaseAll();

            Assert.That(service.IsPaused, Is.False);
            Assert.That(service.ActiveRequestCount, Is.Zero);
            Assert.That(timeScale, Is.EqualTo(0.5f));
            Assert.That(setCount, Is.EqualTo(2));
        }

        [Test]
        public void DisposeRestoresTimeAndRejectsNewRequests()
        {
            float timeScale = 2f;
            var service = new GamePauseService(
                () => timeScale,
                value => timeScale = value);
            service.RequestPause();

            service.Dispose();
            service.Dispose();

            Assert.That(timeScale, Is.EqualTo(2f));
            Assert.That(service.IsPaused, Is.False);
            Assert.Throws<ObjectDisposedException>(
                () => service.RequestPause());
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-1f)]
        public void InvalidCapturedTimeScaleFallsBackToOne(float initialScale)
        {
            float timeScale = initialScale;
            var service = new GamePauseService(
                () => timeScale,
                value => timeScale = value);

            IDisposable request = service.RequestPause();
            request.Dispose();

            Assert.That(timeScale, Is.EqualTo(1f));
        }
    }
}
