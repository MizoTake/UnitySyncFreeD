using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Abstractions
{
    public interface ICameraOutput
    {
        void Send(in CameraSyncState state);
    }
}
