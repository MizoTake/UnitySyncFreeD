using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Sync;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class SpecializedCameraSynchronizerTests
    {
        [Test]
        public void DualDriveCameraSynchronizer_UsesCorrectedPose()
        {
            var synchronizer = new DualDriveCameraSynchronizer();
            var state = synchronizer.Update(CreateContext(SyncMode.VirtualMaster));

            Assert.That(state.Corrected.PanDeg, Is.EqualTo(12d).Within(0.0001d));
        }

        [Test]
        public void RealMasterCameraSynchronizer_UsesObservedPose()
        {
            var synchronizer = new RealMasterCameraSynchronizer();
            var state = synchronizer.Update(CreateContext(SyncMode.DualDrive));

            Assert.That(state.Corrected.PanDeg, Is.EqualTo(14d).Within(0.0001d));
        }

        [Test]
        public void ReplayCameraSynchronizer_UsesPredictedPose()
        {
            var synchronizer = new ReplayCameraSynchronizer();
            var state = synchronizer.Update(CreateContext(SyncMode.DualDrive));

            Assert.That(state.Corrected.PanDeg, Is.EqualTo(10d).Within(0.0001d));
        }

        private static CameraSyncContext CreateContext(SyncMode syncMode)
        {
            return new CameraSyncContext(
                100L,
                new CameraObservedFrame
                {
                    SourceId = "observed",
                    CameraId = 7,
                    Pose = new PoseState { PanDeg = 14d, Xmm = 1040d, TimestampTicks = 100L },
                    Validity = new ValidityState { IsTrackingValid = true, IsLensValid = true }
                },
                new CameraCommandFrame
                {
                    SourceId = "command",
                    CameraId = 7,
                    Pose = new PoseState { PanDeg = 10d, Xmm = 1000d, TimestampTicks = 90L }
                },
                syncMode,
                new SyncTuningProfile
                {
                    RotationCorrectionGain = 0.5d,
                    PositionCorrectionGain = 0.25d,
                    SnapThresholdDeg = 10d,
                    SnapThresholdMm = 100d
                });
        }
    }
}
