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
            var commandLens = commandFrame?.Lens ?? observedFrame?.Lens ?? default;
            var observedLens = observedFrame?.Lens ?? commandLens;
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
                    return predictedLens;
                case SyncMode.RealMaster:
                case SyncMode.ExternalTrackingMaster:
                    return hasObservedFrame ? observedLens : predictedLens;
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
            corrected.FocusDistanceMeters = CorrectLinear(predictedLens.FocusDistanceMeters, observedLens.FocusDistanceMeters, System.Math.Max(0.001d, tuning.SnapThresholdZoom), tuning.ZoomCorrectionGain);
            corrected.IrisFNumber = observedLens.IrisFNumber != 0d ? observedLens.IrisFNumber : predictedLens.IrisFNumber;
            return corrected;
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
}
