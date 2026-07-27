namespace MizoTake.SyncFreeD.Core.Models
{
    public struct CameraObservedFrame
    {
        public string SourceId;
        public int CameraId;
        public CameraCapabilities Capabilities;
        public PoseState Pose;
        public LensState Lens;
        public CameraProjectionState Projection;
        public TimingState Timing;
        public ValidityState Validity;
        public FreeDRawPacketValues RawFreeD;
    }

    public struct CameraProjectionState
    {
        public double SensorWidthMm;
        public double SensorHeightMm;
    }

    public struct FreeDRawPacketValues
    {
        public FreeDMessageType MessageType;
        public int Pan;
        public int Tilt;
        public int Roll;
        public int X;
        public int Y;
        public int Z;
        public int Zoom;
        public int Focus;
        public ushort UserData;
        public byte Checksum;
    }
}
