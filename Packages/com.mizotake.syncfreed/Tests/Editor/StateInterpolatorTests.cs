using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Sync;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class StateInterpolatorTests
    {
        [Test]
        public void Lerp_InterpolatesPoseFields()
        {
            var from = new PoseState { PanDeg = 0d, Xmm = 0d, TimestampTicks = 0L };
            var to = new PoseState { PanDeg = 10d, Xmm = 100d, TimestampTicks = 100L };

            var result = StateInterpolator.Lerp(from, to, 0.5d);

            Assert.That(result.PanDeg, Is.EqualTo(5d).Within(0.0001d));
            Assert.That(result.Xmm, Is.EqualTo(50d).Within(0.0001d));
            Assert.That(result.TimestampTicks, Is.EqualTo(50L));
        }
    }
}
