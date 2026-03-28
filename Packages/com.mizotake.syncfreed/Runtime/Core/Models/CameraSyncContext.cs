namespace MizoTake.SyncFreeD.Core.Models
{
    public readonly struct CameraSyncContext
    {
        public CameraSyncContext(long timestampTicks, CameraObservedFrame? observedFrame, CameraCommandFrame? commandFrame, SyncMode syncMode, SyncTuningProfile tuning)
        {
            TimestampTicks = timestampTicks;
            ObservedFrame = observedFrame;
            CommandFrame = commandFrame;
            SyncMode = syncMode;
            Tuning = tuning;
        }

        public long TimestampTicks { get; }
        public CameraObservedFrame? ObservedFrame { get; }
        public CameraCommandFrame? CommandFrame { get; }
        public SyncMode SyncMode { get; }
        public SyncTuningProfile Tuning { get; }
    }
}
