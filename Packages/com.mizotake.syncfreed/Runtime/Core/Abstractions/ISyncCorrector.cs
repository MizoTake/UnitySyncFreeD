using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Abstractions
{
    public interface ISyncCorrector
    {
        PoseState Correct(in PoseState predicted, in PoseState observed, in SyncTuningProfile profile);
    }
}
