using MizoTake.SyncFreeD.Core.Diagnostics;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Sync
{
    public readonly struct SyncTickResult
    {
        public SyncTickResult(CameraSyncState state, SyncDiagnosticsSnapshot diagnostics)
        {
            State = state;
            Diagnostics = diagnostics;
        }

        public CameraSyncState State { get; }
        public SyncDiagnosticsSnapshot Diagnostics { get; }
    }
}
