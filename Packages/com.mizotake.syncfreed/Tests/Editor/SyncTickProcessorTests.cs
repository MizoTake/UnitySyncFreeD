using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Sync;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class SyncTickProcessorTests
    {
        [Test]
        public void Process_AppliesLensProfileToOutputState()
        {
            var processor = new SyncTickProcessor();
            var request = new SyncTickRequest(
                1000L,
                new CameraObservedFrame
                {
                    SourceId = "obs",
                    CameraId = 3,
                    Pose = new PoseState { TimestampTicks = 1000L },
                    Lens = new LensState { ZoomNormalized = 2d, FocalLengthMm = 150d, FocusNormalized = -1d },
                    Validity = new ValidityState { IsTrackingValid = true, IsLensValid = true }
                },
                new CameraCommandFrame
                {
                    SourceId = "cmd",
                    CameraId = 3,
                    Pose = new PoseState { TimestampTicks = 1000L },
                    Lens = new LensState { ZoomNormalized = 2d, FocalLengthMm = 150d, FocusNormalized = -1d }
                },
                SyncMode.RealMaster,
                OutputPoseKind.Corrected,
                new SyncTuningProfile(),
                new LensProfile { MinFocalLengthMm = 20d, MaxFocalLengthMm = 100d });

            var result = processor.Process(request);

            Assert.That(result.State.CorrectedLens.FocalLengthMm, Is.EqualTo(100d).Within(0.0001d));
            Assert.That(result.State.CorrectedLens.ZoomNormalized, Is.EqualTo(1d).Within(0.0001d));
            Assert.That(result.State.CorrectedLens.FocusNormalized, Is.EqualTo(0d).Within(0.0001d));
        }

        [Test]
        public void Process_WithOutputDelay_UsesHistoricalCorrectedPose()
        {
            var processor = new SyncTickProcessor();
            var tuning = new SyncTuningProfile { OutputDelayMs = 10 };
            var first = processor.Process(new SyncTickRequest(
                1000L,
                new CameraObservedFrame
                {
                    SourceId = "obs",
                    CameraId = 1,
                    Pose = new PoseState { PanDeg = 10d, TimestampTicks = 1000L },
                    Validity = new ValidityState { IsTrackingValid = true, IsLensValid = true }
                },
                new CameraCommandFrame { SourceId = "cmd", CameraId = 1, Pose = new PoseState { PanDeg = 10d, TimestampTicks = 1000L } },
                SyncMode.RealMaster,
                OutputPoseKind.Corrected,
                tuning,
                null));
            var second = processor.Process(new SyncTickRequest(
                1020L,
                new CameraObservedFrame
                {
                    SourceId = "obs",
                    CameraId = 1,
                    Pose = new PoseState { PanDeg = 20d, TimestampTicks = 1020L },
                    Validity = new ValidityState { IsTrackingValid = true, IsLensValid = true }
                },
                new CameraCommandFrame { SourceId = "cmd", CameraId = 1, Pose = new PoseState { PanDeg = 20d, TimestampTicks = 1020L } },
                SyncMode.RealMaster,
                OutputPoseKind.Corrected,
                tuning,
                null));

            Assert.That(first.State.Corrected.PanDeg, Is.EqualTo(10d).Within(0.0001d));
            Assert.That(second.State.Corrected.PanDeg, Is.EqualTo(10d).Within(0.0001d));
        }
    }
}
