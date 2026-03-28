namespace MizoTake.SyncFreeD.Core.Models
{
    public struct CameraObservedFrame
    {
        public string SourceId;
        public int CameraId;
        public CameraCapabilities Capabilities;
        public PoseState Pose;
        public LensState Lens;
        public TimingState Timing;
        public ValidityState Validity;
    }
}
