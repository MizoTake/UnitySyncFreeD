using System;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Correction
{
    public sealed class DefaultSyncCorrector : ISyncCorrector
    {
        public PoseState Correct(in PoseState predicted, in PoseState observed, in SyncTuningProfile profile)
        {
            var corrected = predicted;
            corrected.PanDeg = CorrectAngle(predicted.PanDeg, observed.PanDeg, profile.SnapThresholdDeg, profile.RotationCorrectionGain);
            corrected.TiltDeg = CorrectAngle(predicted.TiltDeg, observed.TiltDeg, profile.SnapThresholdDeg, profile.RotationCorrectionGain);
            corrected.RollDeg = CorrectAngle(predicted.RollDeg, observed.RollDeg, profile.SnapThresholdDeg, profile.RotationCorrectionGain);
            corrected.Xmm = CorrectLinear(predicted.Xmm, observed.Xmm, profile.SnapThresholdMm, profile.PositionCorrectionGain);
            corrected.Ymm = CorrectLinear(predicted.Ymm, observed.Ymm, profile.SnapThresholdMm, profile.PositionCorrectionGain);
            corrected.Zmm = CorrectLinear(predicted.Zmm, observed.Zmm, profile.SnapThresholdMm, profile.PositionCorrectionGain);
            corrected.TimestampTicks = Math.Max(predicted.TimestampTicks, observed.TimestampTicks);
            return corrected;
        }

        private static double CorrectAngle(double predicted, double observed, double snapThresholdDeg, double gain)
        {
            var delta = NormalizeAngle(observed - predicted);
            if (Math.Abs(delta) >= snapThresholdDeg)
            {
                return observed;
            }

            return NormalizeAngle(predicted + (delta * gain));
        }

        private static double CorrectLinear(double predicted, double observed, double snapThreshold, double gain)
        {
            var delta = observed - predicted;
            if (Math.Abs(delta) >= snapThreshold)
            {
                return observed;
            }

            return predicted + (delta * gain);
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
