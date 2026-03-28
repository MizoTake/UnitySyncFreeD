using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Sync;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class CameraSyncEngineTests
    {
        [Test]
        public void Update_AppliesSelectedOutputPose()
        {
            var engine = new CameraSyncEngine();
            var context = new CameraSyncContext(
                100L,
                new CameraObservedFrame { SourceId = "obs", CameraId = 1, Pose = new PoseState { PanDeg = 30d }, Validity = new ValidityState { IsTrackingValid = true, IsLensValid = true } },
                new CameraCommandFrame { SourceId = "cmd", CameraId = 1, Pose = new PoseState { PanDeg = 10d } },
                SyncMode.VirtualMaster,
                new SyncTuningProfile());

            var state = engine.Update(context, OutputPoseKind.Observed);

            Assert.That(state.Corrected.PanDeg, Is.EqualTo(30d).Within(0.0001d));
        }

        [Test]
        public void Update_InDualDrive_UsesDualDriveSynchronizer()
        {
            var engine = new CameraSyncEngine(new StubSynchronizer(1d), new StubSynchronizer(2d), new StubSynchronizer(3d), new StubSynchronizer(4d));
            var context = new CameraSyncContext(100L, default, default, SyncMode.DualDrive, new SyncTuningProfile());

            var state = engine.Update(context);

            Assert.That(state.Corrected.PanDeg, Is.EqualTo(2d).Within(0.0001d));
        }

        [Test]
        public void Update_InReplayMaster_UsesReplaySynchronizer()
        {
            var engine = new CameraSyncEngine(new StubSynchronizer(1d), new StubSynchronizer(2d), new StubSynchronizer(3d), new StubSynchronizer(4d));
            var context = new CameraSyncContext(100L, default, default, SyncMode.ReplayMaster, new SyncTuningProfile());

            var state = engine.Update(context);

            Assert.That(state.Corrected.PanDeg, Is.EqualTo(4d).Within(0.0001d));
        }

        private sealed class StubSynchronizer : Core.Abstractions.ICameraSynchronizer
        {
            private readonly double panDeg;

            public StubSynchronizer(double panDeg)
            {
                this.panDeg = panDeg;
            }

            public CameraSyncState Update(CameraSyncContext context)
            {
                return new CameraSyncState
                {
                    Corrected = new PoseState { PanDeg = panDeg }
                };
            }
        }
    }
}
