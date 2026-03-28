using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Sync
{
    public sealed class CameraSyncEngine
    {
        private readonly ICameraSynchronizer synchronizer;
        private readonly ICameraSynchronizer dualDriveSynchronizer;
        private readonly ICameraSynchronizer realMasterSynchronizer;
        private readonly ICameraSynchronizer replaySynchronizer;

        public CameraSyncEngine()
            : this(new DefaultCameraSynchronizer(), new DualDriveCameraSynchronizer(), new RealMasterCameraSynchronizer(), new ReplayCameraSynchronizer())
        {
        }

        public CameraSyncEngine(ICameraSynchronizer synchronizer)
            : this(synchronizer, new DualDriveCameraSynchronizer(), new RealMasterCameraSynchronizer(), new ReplayCameraSynchronizer())
        {
        }

        public CameraSyncEngine(ICameraSynchronizer synchronizer, ICameraSynchronizer dualDriveSynchronizer, ICameraSynchronizer realMasterSynchronizer, ICameraSynchronizer replaySynchronizer)
        {
            this.synchronizer = synchronizer;
            this.dualDriveSynchronizer = dualDriveSynchronizer;
            this.realMasterSynchronizer = realMasterSynchronizer;
            this.replaySynchronizer = replaySynchronizer;
        }

        public CameraSyncState Update(CameraSyncContext context, OutputPoseKind outputPoseKind = OutputPoseKind.Corrected)
        {
            return CameraSyncStateSelector.SelectOutputPose(SelectSynchronizer(context.SyncMode).Update(context), outputPoseKind);
        }

        private ICameraSynchronizer SelectSynchronizer(SyncMode syncMode)
        {
            switch (syncMode)
            {
                case SyncMode.DualDrive:
                    return dualDriveSynchronizer;
                case SyncMode.RealMaster:
                case SyncMode.ExternalTrackingMaster:
                    return realMasterSynchronizer;
                case SyncMode.ReplayMaster:
                    return replaySynchronizer;
                case SyncMode.VirtualMaster:
                default:
                    return synchronizer;
            }
        }
    }
}
