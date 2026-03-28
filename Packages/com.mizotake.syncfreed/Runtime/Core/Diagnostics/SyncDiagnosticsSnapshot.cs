using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Diagnostics
{
    public readonly struct SyncDiagnosticsSnapshot
    {
        public SyncDiagnosticsSnapshot(double panErrorDeg, double tiltErrorDeg, double rollErrorDeg, double positionErrorMm, double zoomErrorMm, int trackingDelayMs, int videoAlignmentDelayMs, bool isTrackingValid, bool isLensValid, bool isDegraded, bool isFallbackMode, bool correctionApplied)
        {
            PanErrorDeg = panErrorDeg;
            TiltErrorDeg = tiltErrorDeg;
            RollErrorDeg = rollErrorDeg;
            PositionErrorMm = positionErrorMm;
            ZoomErrorMm = zoomErrorMm;
            TrackingDelayMs = trackingDelayMs;
            VideoAlignmentDelayMs = videoAlignmentDelayMs;
            IsTrackingValid = isTrackingValid;
            IsLensValid = isLensValid;
            IsDegraded = isDegraded;
            IsFallbackMode = isFallbackMode;
            CorrectionApplied = correctionApplied;
        }

        public double PanErrorDeg { get; }
        public double TiltErrorDeg { get; }
        public double RollErrorDeg { get; }
        public double PositionErrorMm { get; }
        public double ZoomErrorMm { get; }
        public int TrackingDelayMs { get; }
        public int VideoAlignmentDelayMs { get; }
        public bool IsTrackingValid { get; }
        public bool IsLensValid { get; }
        public bool IsDegraded { get; }
        public bool IsFallbackMode { get; }
        public bool CorrectionApplied { get; }
    }
}
