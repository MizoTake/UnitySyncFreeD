using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Sync;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class DefaultCameraSynchronizerTests
    {
        [Test]
        public void Update_InDualDrive_UsesCorrectedPoseAndTuningDelays()
        {
            var synchronizer = new DefaultCameraSynchronizer();
            var context = new CameraSyncContext(
                100L,
                new CameraObservedFrame
                {
                    SourceId = "observed",
                    CameraId = 7,
                    Pose = new PoseState { PanDeg = 14d, Xmm = 1040d, TimestampTicks = 100L },
                    Lens = new LensState { FocalLengthMm = 50d, ZoomNormalized = 0.8d, FocusNormalized = 0.6d },
                    Timing = new TimingState { FrameModulo16 = 3, CommandDelayMs = 12 },
                    Validity = new ValidityState { IsTrackingValid = true, IsLensValid = true }
                },
                new CameraCommandFrame
                {
                    SourceId = "command",
                    CameraId = 7,
                    Pose = new PoseState { PanDeg = 10d, Xmm = 1000d, TimestampTicks = 90L },
                    Lens = new LensState { FocalLengthMm = 35d, ZoomNormalized = 0.5d, FocusNormalized = 0.2d },
                    Timing = new TimingState { CommandDelayMs = 8 }
                },
                SyncMode.DualDrive,
                new SyncTuningProfile
                {
                    RotationCorrectionGain = 0.5d,
                    PositionCorrectionGain = 0.25d,
                    ZoomCorrectionGain = 0.5d,
                    SnapThresholdDeg = 10d,
                    SnapThresholdMm = 100d,
                    SnapThresholdZoom = 0.5d,
                    TrackingDelayMs = 22,
                    OutputDelayMs = 33,
                    VideoAlignmentDelayMs = 44
                });

            var state = synchronizer.Update(context);

            Assert.That(state.SourceId, Is.EqualTo("observed"));
            Assert.That(state.CameraId, Is.EqualTo(7));
            Assert.That(state.Command.PanDeg, Is.EqualTo(10d).Within(0.0001d));
            Assert.That(state.Predicted.PanDeg, Is.EqualTo(10d).Within(0.0001d));
            Assert.That(state.Observed.PanDeg, Is.EqualTo(14d).Within(0.0001d));
            Assert.That(state.Corrected.PanDeg, Is.EqualTo(12d).Within(0.0001d));
            Assert.That(state.Corrected.Xmm, Is.EqualTo(1010d).Within(0.0001d));
            Assert.That(state.CommandLens.FocalLengthMm, Is.EqualTo(35d).Within(0.0001d));
            Assert.That(state.ObservedLens.FocalLengthMm, Is.EqualTo(50d).Within(0.0001d));
            Assert.That(state.CorrectedLens.FocalLengthMm, Is.EqualTo(42.5d).Within(0.0001d));
            Assert.That(state.CorrectedLens.ZoomNormalized, Is.EqualTo(0.65d).Within(0.0001d));
            Assert.That(state.CorrectedLens.FocusNormalized, Is.EqualTo(0.4d).Within(0.0001d));
            Assert.That(state.Lens.FocalLengthMm, Is.EqualTo(42.5d).Within(0.0001d));
            Assert.That(state.Timing.FrameModulo16, Is.EqualTo(3));
            Assert.That(state.Timing.TrackingDelayMs, Is.EqualTo(22));
            Assert.That(state.Timing.OutputDelayMs, Is.EqualTo(33));
            Assert.That(state.Timing.VideoAlignmentDelayMs, Is.EqualTo(44));
            Assert.That(state.Validity.IsTrackingValid, Is.True);
        }

        [Test]
        public void Update_InVirtualMaster_KeepsPredictedPoseWhenObservedExists()
        {
            var synchronizer = new DefaultCameraSynchronizer();
            var context = new CameraSyncContext(
                100L,
                new CameraObservedFrame
                {
                    SourceId = "observed",
                    CameraId = 2,
                    Pose = new PoseState { PanDeg = 30d, TimestampTicks = 100L },
                    Validity = new ValidityState { IsTrackingValid = true, IsLensValid = false }
                },
                new CameraCommandFrame
                {
                    SourceId = "command",
                    CameraId = 2,
                    Pose = new PoseState { PanDeg = 10d, TimestampTicks = 90L }
                },
                SyncMode.VirtualMaster,
                new SyncTuningProfile());

            var state = synchronizer.Update(context);

            Assert.That(state.Corrected.PanDeg, Is.EqualTo(10d).Within(0.0001d));
            Assert.That(state.Observed.PanDeg, Is.EqualTo(30d).Within(0.0001d));
        }

        [Test]
        public void Update_InDualDrive_SnapsLensWhenZoomDeltaExceedsThreshold()
        {
            var synchronizer = new DefaultCameraSynchronizer();
            var context = new CameraSyncContext(
                100L,
                new CameraObservedFrame
                {
                    Lens = new LensState { FocalLengthMm = 100d, ZoomNormalized = 1d, FocusNormalized = 1d },
                    Validity = new ValidityState { IsTrackingValid = true, IsLensValid = true }
                },
                new CameraCommandFrame
                {
                    Lens = new LensState { FocalLengthMm = 20d, ZoomNormalized = 0d, FocusNormalized = 0d }
                },
                SyncMode.DualDrive,
                new SyncTuningProfile
                {
                    ZoomCorrectionGain = 0.5d,
                    SnapThresholdZoom = 0.1d
                });

            var state = synchronizer.Update(context);

            Assert.That(state.CorrectedLens.FocalLengthMm, Is.EqualTo(100d).Within(0.0001d));
            Assert.That(state.CorrectedLens.ZoomNormalized, Is.EqualTo(1d).Within(0.0001d));
            Assert.That(state.CorrectedLens.FocusNormalized, Is.EqualTo(1d).Within(0.0001d));
        }
    }
}
