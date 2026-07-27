namespace MizoTake.SyncFreeD.Core.Models
{
    public enum FreeDMessageType : byte
    {
        Unknown = 0x00,
        PollOrCommand = 0xD0,
        CameraPositionAndOrientation = 0xD1,
        SystemStatus = 0xD2,
        ControlParameters = 0xD3,
        FirstTargetData = 0xD4,
        NextTargetData = 0xD5,
        FirstImagePoint = 0xD6,
        NextImagePoint = 0xD7,
        EepromData = 0xD8,
        CameraCalibration = 0xDA,
        DiagnosticMode = 0xDB
    }
}
