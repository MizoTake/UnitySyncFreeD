using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Sync;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class DelayCompensatorTests
    {
        [Test]
        public void SampleDelayed_ReturnsClosestPoseForRequestedDelay()
        {
            var compensator = new DelayCompensator();
            compensator.Push(new PoseState { PanDeg = 1d, TimestampTicks = 1000L });
            compensator.Push(new PoseState { PanDeg = 2d, TimestampTicks = 2000L });
            compensator.Push(new PoseState { PanDeg = 3d, TimestampTicks = 3000L });

            var result = compensator.SampleDelayed(3000L, 0);

            Assert.That(result.PanDeg, Is.EqualTo(3d).Within(0.0001d));
        }
    }
}
