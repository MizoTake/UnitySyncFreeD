using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    public interface ICameraFrameProvider : ICameraSource
    {
        bool TryGetObservedFrame(out CameraObservedFrame frame);
        CameraCommandFrame CaptureCommandFrame();
    }
}
