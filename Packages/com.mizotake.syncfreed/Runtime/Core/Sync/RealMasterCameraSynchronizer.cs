using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Correction;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Sync
{
    public sealed class RealMasterCameraSynchronizer : ICameraSynchronizer
    {
        private readonly DefaultCameraSynchronizer innerSynchronizer;

        public RealMasterCameraSynchronizer()
            : this(new DefaultSyncCorrector())
        {
        }

        public RealMasterCameraSynchronizer(ISyncCorrector corrector)
        {
            innerSynchronizer = new DefaultCameraSynchronizer(corrector);
        }

        public CameraSyncState Update(CameraSyncContext context)
        {
            return innerSynchronizer.Update(new CameraSyncContext(context.TimestampTicks, context.ObservedFrame, context.CommandFrame, SyncMode.RealMaster, context.Tuning));
        }
    }
}
