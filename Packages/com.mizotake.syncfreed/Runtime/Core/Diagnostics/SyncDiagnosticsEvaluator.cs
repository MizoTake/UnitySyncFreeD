using System;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Diagnostics
{
    public static class SyncDiagnosticsEvaluator
    {
        public static SyncDiagnosticsSnapshot Evaluate(in CameraSyncState state)
        {
            var dx = state.Corrected.Xmm - state.Observed.Xmm;
            var dy = state.Corrected.Ymm - state.Observed.Ymm;
            var dz = state.Corrected.Zmm - state.Observed.Zmm;
            var observedLens = LensStateUtility.MergePhysicalValues(state.ObservedLens, state.PredictedLens);
            var correctedLens = LensStateUtility.MergePhysicalValues(state.CorrectedLens, state.Lens);
            var zoomErrorMm = correctedLens.FocalLengthMm - observedLens.FocalLengthMm;
            var correctionApplied = Math.Abs(NormalizeAngle(state.Corrected.PanDeg - state.Predicted.PanDeg)) > 0.0001d || Math.Abs(NormalizeAngle(state.Corrected.TiltDeg - state.Predicted.TiltDeg)) > 0.0001d || Math.Abs(NormalizeAngle(state.Corrected.RollDeg - state.Predicted.RollDeg)) > 0.0001d || Math.Abs(state.Corrected.Xmm - state.Predicted.Xmm) > 0.0001d || Math.Abs(state.Corrected.Ymm - state.Predicted.Ymm) > 0.0001d || Math.Abs(state.Corrected.Zmm - state.Predicted.Zmm) > 0.0001d || Math.Abs(zoomErrorMm) > 0.0001d;
            return new SyncDiagnosticsSnapshot(NormalizeAngle(state.Corrected.PanDeg - state.Observed.PanDeg), NormalizeAngle(state.Corrected.TiltDeg - state.Observed.TiltDeg), NormalizeAngle(state.Corrected.RollDeg - state.Observed.RollDeg), Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz)), zoomErrorMm, state.Timing.TrackingDelayMs, state.Timing.VideoAlignmentDelayMs, state.Validity.IsTrackingValid, state.Validity.IsLensValid, state.Validity.IsDegraded, state.Validity.IsFallbackMode, correctionApplied);
        }

        private static double NormalizeAngle(double angle)
        {
            var normalized = angle % 360d;
            if (normalized > 180d)
            {
                normalized -= 360d;
            }
            else if (normalized <= -180d)
            {
                normalized += 360d;
            }

            return normalized;
        }
    }
}
