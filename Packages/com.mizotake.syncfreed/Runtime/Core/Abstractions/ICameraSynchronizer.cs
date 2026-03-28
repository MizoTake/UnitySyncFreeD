using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Abstractions
{
    public interface ICameraSynchronizer
    {
        CameraSyncState Update(CameraSyncContext context);
    }
}
