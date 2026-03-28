using System;

namespace MizoTake.SyncFreeD.Core.Models
{
    [Serializable]
    public sealed class SyncTuningProfile
    {
        public double PanSpeedScale = 1d;
        public double TiltSpeedScale = 1d;
        public double ZoomSpeedScale = 1d;
        public double RotationCorrectionGain = 1d;
        public double PositionCorrectionGain = 1d;
        public double ZoomCorrectionGain = 1d;
        public double SnapThresholdDeg = 5d;
        public double SnapThresholdMm = 100d;
        public double SnapThresholdZoom = 0.1d;
        public int InquiryIntervalMs = 100;
        public int TrackingDelayMs;
        public int OutputDelayMs;
        public int VideoAlignmentDelayMs;
    }
}
