namespace MizoTake.SyncFreeD.Core.Models
{
    public struct ViscaTelemetryFrame
    {
        public PoseState ObservedPose;
        public LensState Lens;
        public TimingState Timing;
        public ValidityState Validity;
    }
}
