namespace MizoTake.SyncFreeD.Core.Models
{
    public struct CameraCommandFrame
    {
        public string SourceId;
        public int CameraId;
        public PoseState Pose;
        public LensState Lens;
        public TimingState Timing;
    }
}
