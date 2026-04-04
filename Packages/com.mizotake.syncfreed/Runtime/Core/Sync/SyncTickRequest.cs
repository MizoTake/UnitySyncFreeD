using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Sync
{
    public readonly struct SyncTickRequest
    {
        public SyncTickRequest(long timestampTicks, CameraObservedFrame observedFrame, CameraCommandFrame commandFrame, SyncMode syncMode, OutputPoseKind outputPoseKind, SyncTuningProfile tuning, LensProfile lensProfile)
        {
            TimestampTicks = timestampTicks;
            ObservedFrame = observedFrame;
            CommandFrame = commandFrame;
            SyncMode = syncMode;
            OutputPoseKind = outputPoseKind;
            Tuning = tuning;
            LensProfile = lensProfile;
        }

        public long TimestampTicks { get; }
        public CameraObservedFrame ObservedFrame { get; }
        public CameraCommandFrame CommandFrame { get; }
        public SyncMode SyncMode { get; }
        public OutputPoseKind OutputPoseKind { get; }
        public SyncTuningProfile Tuning { get; }
        public LensProfile LensProfile { get; }
    }
}
