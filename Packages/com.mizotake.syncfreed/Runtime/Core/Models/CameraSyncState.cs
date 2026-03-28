namespace MizoTake.SyncFreeD.Core.Models
{
    public struct CameraSyncState
    {
        public string SourceId;
        public int CameraId;
        public PoseState Command;
        public PoseState Predicted;
        public PoseState Observed;
        public PoseState Corrected;
        public LensState CommandLens;
        public LensState PredictedLens;
        public LensState ObservedLens;
        public LensState CorrectedLens;
        public LensState Lens;
        public TimingState Timing;
        public ValidityState Validity;
    }
}
