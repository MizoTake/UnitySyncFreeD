using MizoTake.SyncFreeD.Core.Correction;
using MizoTake.SyncFreeD.Core.Models;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class DefaultSyncCorrectorTests
    {
        [Test]
        public void Correct_WhenDeltaIsWithinThreshold_BlendsByGain()
        {
            var corrector = new DefaultSyncCorrector();
            var profile = new SyncTuningProfile
            {
                RotationCorrectionGain = 0.5d,
                PositionCorrectionGain = 0.25d,
                SnapThresholdDeg = 10d,
                SnapThresholdMm = 100d
            };
            var predicted = new PoseState { PanDeg = 10d, Xmm = 1000d, TimestampTicks = 100L };
            var observed = new PoseState { PanDeg = 14d, Xmm = 1040d, TimestampTicks = 120L };

            var corrected = corrector.Correct(predicted, observed, profile);

            Assert.That(corrected.PanDeg, Is.EqualTo(12d).Within(0.0001d));
            Assert.That(corrected.Xmm, Is.EqualTo(1010d).Within(0.0001d));
            Assert.That(corrected.TimestampTicks, Is.EqualTo(120L));
        }

        [Test]
        public void Correct_WhenDeltaExceedsThreshold_SnapsToObserved()
        {
            var corrector = new DefaultSyncCorrector();
            var profile = new SyncTuningProfile
            {
                RotationCorrectionGain = 0.5d,
                PositionCorrectionGain = 0.25d,
                SnapThresholdDeg = 5d,
                SnapThresholdMm = 100d
            };
            var predicted = new PoseState { PanDeg = 10d, Xmm = 1000d };
            var observed = new PoseState { PanDeg = 30d, Xmm = 1200d };

            var corrected = corrector.Correct(predicted, observed, profile);

            Assert.That(corrected.PanDeg, Is.EqualTo(30d).Within(0.0001d));
            Assert.That(corrected.Xmm, Is.EqualTo(1200d).Within(0.0001d));
        }
    }
}
