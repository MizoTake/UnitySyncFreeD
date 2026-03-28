namespace MizoTake.SyncFreeD.Core.Models
{
    public sealed class FirmwareBehaviorProfile
    {
        public string VersionLabel = string.Empty;
        public bool SupportsMultiUnicast;
        public bool SupportsMulticast;
        public bool UsesImageSensorBasedOrientation;
        public bool UsesImageSensorBasedPosition;
        public bool SupportsSlideBase;
    }
}
