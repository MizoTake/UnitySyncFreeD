namespace MizoTake.SyncFreeD.Core.Models
{
    public sealed class DeviceProfile
    {
        public string DeviceName = string.Empty;
        public CameraCapabilities Capabilities;
        public bool SupportsInquiry;
        public bool SupportsRoll;
        public bool SupportsPosition;
    }
}
