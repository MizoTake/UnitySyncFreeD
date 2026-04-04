using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Abstractions
{
    public interface ILensDataSource
    {
        bool TryGetLensState(out LensState lens);
    }
}
