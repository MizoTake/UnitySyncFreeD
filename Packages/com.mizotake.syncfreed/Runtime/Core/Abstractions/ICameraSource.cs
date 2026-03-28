using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Abstractions
{
    public interface ICameraSource
    {
        string SourceId { get; }
        CameraCapabilities Capabilities { get; }
        bool TryGetObservedState(out CameraObservedFrame frame);
    }
}
