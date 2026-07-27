using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Correction;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Sync
{
    public sealed class DefaultCameraSynchronizer : ICameraSynchronizer
    {
        private readonly ISyncCorrector corrector;

        public DefaultCameraSynchronizer()
            : this(new DefaultSyncCorrector())
        {
        }

        public DefaultCameraSynchronizer(ISyncCorrector corrector)
        {
            this.corrector = corrector;
        }

        public CameraSyncState Update(CameraSyncContext context)
        {
            var commandFrame = context.CommandFrame;
            var observedFrame = context.ObservedFrame;
            var commandPose = commandFrame?.Pose ?? observedFrame?.Pose ?? default;
            var observedPose = observedFrame?.Pose ?? commandPose;
            var predictedPose = commandPose;
            var correctedPose = SelectCorrectedPose(context.SyncMode, predictedPose, observedPose, observedFrame.HasValue, context.Tuning);
            var fallbackObservedLens = observedFrame?.Lens ?? default;
            var commandLens = commandFrame.HasValue ? LensStateUtility.MergePhysicalValues(commandFrame.Value.Lens, fallbackObservedLens) : fallbackObservedLens;
            var observedLens = observedFrame.HasValue ? LensStateUtility.MergePhysicalValues(observedFrame.Value.Lens, commandLens) : commandLens;
            var predictedLens = commandLens;
            var correctedLens = SelectCorrectedLens(context.SyncMode, predictedLens, observedLens, observedFrame.HasValue, context.Tuning);
            return new CameraSyncState
            {
                SourceId = observedFrame?.SourceId ?? commandFrame?.SourceId ?? string.Empty,
                CameraId = observedFrame?.CameraId ?? commandFrame?.CameraId ?? 0,
                Command = commandPose,
                Predicted = predictedPose,
                Observed = observedPose,
                Corrected = correctedPose,
                CommandLens = commandLens,
                PredictedLens = predictedLens,
                ObservedLens = observedLens,
                CorrectedLens = correctedLens,
                Lens = correctedLens,
                Timing = MergeTiming(commandFrame, observedFrame, context.Tuning),
                Validity = MergeValidity(observedFrame, commandFrame)
            };
        }

        private PoseState SelectCorrectedPose(SyncMode syncMode, in PoseState predictedPose, in PoseState observedPose, bool hasObservedFrame, SyncTuningProfile tuning)
        {
            switch (syncMode)
            {
                case SyncMode.VirtualMaster:
                case SyncMode.ReplayMaster:
                    return predictedPose;
                case SyncMode.RealMaster:
                case SyncMode.ExternalTrackingMaster:
                    return hasObservedFrame ? observedPose : predictedPose;
                case SyncMode.DualDrive:
                default:
                    return hasObservedFrame ? corrector.Correct(predictedPose, observedPose, tuning) : predictedPose;
            }
        }

        private static LensState SelectCorrectedLens(SyncMode syncMode, in LensState predictedLens, in LensState observedLens, bool hasObservedFrame, SyncTuningProfile tuning)
        {
            switch (syncMode)
            {
                case SyncMode.VirtualMaster:
                case SyncMode.ReplayMaster:
                    return LensStateUtility.MergePhysicalValues(predictedLens, observedLens);
                case SyncMode.RealMaster:
                case SyncMode.ExternalTrackingMaster:
                    return hasObservedFrame ? LensStateUtility.MergePhysicalValues(observedLens, predictedLens) : predictedLens;
                case SyncMode.DualDrive:
                default:
                    return hasObservedFrame ? CorrectLens(predictedLens, observedLens, tuning) : predictedLens;
            }
        }

        private static LensState CorrectLens(in LensState predictedLens, in LensState observedLens, SyncTuningProfile tuning)
        {
            var corrected = predictedLens;
            corrected.ZoomNormalized = CorrectLinear(predictedLens.ZoomNormalized, observedLens.ZoomNormalized, tuning.SnapThresholdZoom, tuning.ZoomCorrectionGain);
            corrected.FocusNormalized = CorrectLinear(predictedLens.FocusNormalized, observedLens.FocusNormalized, tuning.SnapThresholdZoom, tuning.ZoomCorrectionGain);
            corrected.FocalLengthMm = CorrectLinear(predictedLens.FocalLengthMm, observedLens.FocalLengthMm, CalculateFocalLengthSnapThreshold(predictedLens, observedLens, tuning.SnapThresholdZoom), tuning.ZoomCorrectionGain);
            corrected.EffectiveFocalLengthMm = CorrectLinear(predictedLens.EffectiveFocalLengthMm, observedLens.EffectiveFocalLengthMm, CalculateEffectiveFocalLengthSnapThreshold(predictedLens, observedLens, tuning.SnapThresholdZoom), tuning.ZoomCorrectionGain);
            corrected.FocusDistanceMeters = CorrectLinear(predictedLens.FocusDistanceMeters, observedLens.FocusDistanceMeters, System.Math.Max(0.001d, tuning.SnapThresholdZoom), tuning.ZoomCorrectionGain);
            corrected.IrisFNumber = observedLens.IrisFNumber != 0d ? observedLens.IrisFNumber : predictedLens.IrisFNumber;
            return LensStateUtility.MergePhysicalValues(corrected, predictedLens);
        }

        private static double CorrectLinear(double predicted, double observed, double snapThreshold, double gain)
        {
            var delta = observed - predicted;
            if (System.Math.Abs(delta) >= snapThreshold)
            {
                return observed;
            }

            return predicted + (delta * gain);
        }

        private static double CalculateFocalLengthSnapThreshold(in LensState predictedLens, in LensState observedLens, double snapThresholdZoom)
        {
            var maxFocalLength = System.Math.Max(System.Math.Abs(predictedLens.FocalLengthMm), System.Math.Abs(observedLens.FocalLengthMm));
            return System.Math.Max(0.001d, maxFocalLength * snapThresholdZoom);
        }

        private static double CalculateEffectiveFocalLengthSnapThreshold(in LensState predictedLens, in LensState observedLens, double snapThresholdZoom)
        {
            var maxFocalLength = System.Math.Max(System.Math.Abs(predictedLens.EffectiveFocalLengthMm), System.Math.Abs(observedLens.EffectiveFocalLengthMm));
            return System.Math.Max(0.001d, maxFocalLength * snapThresholdZoom);
        }

        private static TimingState MergeTiming(CameraCommandFrame? commandFrame, CameraObservedFrame? observedFrame, SyncTuningProfile tuning)
        {
            var timing = observedFrame?.Timing ?? commandFrame?.Timing ?? default;
            timing.TrackingDelayMs = tuning.TrackingDelayMs;
            timing.OutputDelayMs = tuning.OutputDelayMs;
            timing.VideoAlignmentDelayMs = tuning.VideoAlignmentDelayMs;
            return timing;
        }

        private static ValidityState MergeValidity(CameraObservedFrame? observedFrame, CameraCommandFrame? commandFrame)
        {
            if (observedFrame.HasValue)
            {
                return observedFrame.Value.Validity;
            }

            return new ValidityState
            {
                IsTrackingValid = commandFrame.HasValue,
                IsLensValid = commandFrame.HasValue,
                IsDegraded = !commandFrame.HasValue,
                IsFallbackMode = !observedFrame.HasValue
            };
        }
    }

    public static class LensProfileApplicator
    {
        public static CameraSyncState Apply(in CameraSyncState state, LensProfile profile)
        {
            if (profile == null)
            {
                return state;
            }

            var adjustedState = state;
            adjustedState.CommandLens = Apply(state.CommandLens, profile);
            adjustedState.PredictedLens = Apply(state.PredictedLens, profile);
            adjustedState.ObservedLens = Apply(state.ObservedLens, profile);
            adjustedState.CorrectedLens = Apply(state.CorrectedLens, profile);
            adjustedState.Lens = Apply(state.Lens, profile);
            return adjustedState;
        }

        public static LensState Apply(in LensState lens, LensProfile profile)
        {
            if (profile == null)
            {
                return lens;
            }

            var adjustedLens = lens;
            adjustedLens.ZoomNormalized = Clamp01(adjustedLens.ZoomNormalized);
            adjustedLens.FocusNormalized = Clamp01(adjustedLens.FocusNormalized);
            if (HasFocalLengthRange(profile))
            {
                if (adjustedLens.FocalLengthMm > 0d)
                {
                    adjustedLens.FocalLengthMm = Clamp(adjustedLens.FocalLengthMm, profile.MinFocalLengthMm, profile.MaxFocalLengthMm);
                    adjustedLens.ZoomNormalized = EstimateZoomNormalized(adjustedLens.FocalLengthMm, profile);
                }
                else
                {
                    adjustedLens.FocalLengthMm = EvaluateFocalLength(adjustedLens.ZoomNormalized, profile);
                }
            }

            if (adjustedLens.FocusDistanceMeters <= 0d && profile.FocusCurve != null)
            {
                adjustedLens.FocusDistanceMeters = System.Math.Max(0d, profile.FocusCurve.Evaluate(adjustedLens.FocusNormalized));
            }

            return adjustedLens;
        }

        private static bool HasFocalLengthRange(LensProfile profile)
        {
            return profile != null && profile.MaxFocalLengthMm > profile.MinFocalLengthMm;
        }

        private static double EvaluateFocalLength(double zoomNormalized, LensProfile profile)
        {
            var normalized = Clamp01(zoomNormalized);
            var focalRangeNormalized = profile.ZoomCurve != null ? Clamp01(profile.ZoomCurve.Evaluate(normalized)) : normalized;
            return Lerp(profile.MinFocalLengthMm, profile.MaxFocalLengthMm, focalRangeNormalized);
        }

        private static double EstimateZoomNormalized(double focalLengthMm, LensProfile profile)
        {
            var bestNormalized = 0d;
            var bestDistance = double.MaxValue;
            for (var step = 0; step <= 100; step++)
            {
                var normalized = step / 100d;
                var candidate = EvaluateFocalLength(normalized, profile);
                var distance = System.Math.Abs(candidate - focalLengthMm);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestNormalized = normalized;
            }

            return bestNormalized;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static double Clamp01(double value)
        {
            return Clamp(value, 0d, 1d);
        }

        private static double Lerp(double min, double max, double t)
        {
            return min + ((max - min) * t);
        }
    }
}
