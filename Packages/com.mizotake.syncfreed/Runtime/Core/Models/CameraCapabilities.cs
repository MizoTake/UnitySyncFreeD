using System;

namespace MizoTake.SyncFreeD.Core.Models
{
    [Flags]
    public enum CameraCapabilities
    {
        None = 0,
        PanTilt = 1 << 0,
        Roll = 1 << 1,
        Position = 1 << 2,
        Zoom = 1 << 3,
        Focus = 1 << 4,
        Iris = 1 << 5,
        Preset = 1 << 6,
        ExternalTracking = 1 << 7
    }
}
