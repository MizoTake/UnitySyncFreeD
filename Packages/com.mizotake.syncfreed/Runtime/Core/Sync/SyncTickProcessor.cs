using MizoTake.SyncFreeD.Core.Diagnostics;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Sync
{
    public sealed class SyncTickProcessor
    {
        private readonly CameraSyncEngine synchronizer;
        private readonly DelayCompensator outputDelayCompensator;
        private CameraCommandFrame lastCommandFrame;
        private bool hasLastCommandFrame;
        private long lastCommandChangeTimestampTicks;

        public SyncTickProcessor()
            : this(new CameraSyncEngine(), new DelayCompensator())
        {
        }

        public SyncTickProcessor(CameraSyncEngine synchronizer, DelayCompensator outputDelayCompensator)
        {
            this.synchronizer = synchronizer ?? throw new System.ArgumentNullException(nameof(synchronizer));
            this.outputDelayCompensator = outputDelayCompensator ?? throw new System.ArgumentNullException(nameof(outputDelayCompensator));
        }

        public SyncTickResult Process(in SyncTickRequest request)
        {
            var tuning = request.Tuning ?? new SyncTuningProfile();
            TrackCommandChanges(request);
            var context = new CameraSyncContext(request.TimestampTicks, request.ObservedFrame, request.CommandFrame, request.SyncMode, tuning);
            var state = synchronizer.Update(context, request.OutputPoseKind);
            state = LensProfileApplicator.Apply(state, request.LensProfile);
            state = ApplyDualDriveFinalObservedSettle(state, request, tuning);
            state = ApplyOutputDelay(state, tuning.OutputDelayMs);
            return new SyncTickResult(state, SyncDiagnosticsEvaluator.Evaluate(state));
        }

        public void Reset()
        {
            outputDelayCompensator.Clear();
            hasLastCommandFrame = false;
            lastCommandChangeTimestampTicks = 0L;
            lastCommandFrame = default;
        }

        private CameraSyncState ApplyOutputDelay(in CameraSyncState state, int outputDelayMs)
        {
            outputDelayCompensator.Push(state.Corrected);
            if (outputDelayMs <= 0)
            {
                return state;
            }

            var delayedState = state;
            delayedState.Corrected = outputDelayCompensator.SampleDelayed(state.Corrected.TimestampTicks, outputDelayMs);
            return delayedState;
        }

        private void TrackCommandChanges(in SyncTickRequest request)
        {
            if (request.SyncMode != SyncMode.DualDrive)
            {
                hasLastCommandFrame = false;
                lastCommandChangeTimestampTicks = request.TimestampTicks;
                lastCommandFrame = default;
                return;
            }

            if (!hasLastCommandFrame || HasCommandChanged(lastCommandFrame, request.CommandFrame))
            {
                lastCommandChangeTimestampTicks = request.TimestampTicks;
            }

            lastCommandFrame = request.CommandFrame;
            hasLastCommandFrame = true;
        }

        private CameraSyncState ApplyDualDriveFinalObservedSettle(in CameraSyncState state, in SyncTickRequest request, SyncTuningProfile tuning)
        {
            if (request.SyncMode != SyncMode.DualDrive || request.OutputPoseKind != OutputPoseKind.Corrected || !hasLastCommandFrame)
            {
                return state;
            }

            if (request.TimestampTicks - lastCommandChangeTimestampTicks < MillisecondsToTicks(tuning.IdleToSettleDelayMs))
            {
                return state;
            }

            var settledState = state;
            if (request.ObservedFrame.Validity.IsTrackingValid)
            {
                settledState.Corrected = settledState.Observed;
            }

            if (request.ObservedFrame.Validity.IsLensValid)
            {
                settledState.CorrectedLens = settledState.ObservedLens;
                settledState.Lens = settledState.CorrectedLens;
            }

            return settledState;
        }

        private static long MillisecondsToTicks(int milliseconds)
        {
            return milliseconds <= 0 ? 0L : milliseconds * System.TimeSpan.TicksPerMillisecond;
        }

        private static bool HasCommandChanged(in CameraCommandFrame previous, in CameraCommandFrame current)
        {
            return !AreNearlyEqual(previous.Pose.PanDeg, current.Pose.PanDeg)
                || !AreNearlyEqual(previous.Pose.TiltDeg, current.Pose.TiltDeg)
                || !AreNearlyEqual(previous.Pose.RollDeg, current.Pose.RollDeg)
                || !AreNearlyEqual(previous.Pose.Xmm, current.Pose.Xmm)
                || !AreNearlyEqual(previous.Pose.Ymm, current.Pose.Ymm)
                || !AreNearlyEqual(previous.Pose.Zmm, current.Pose.Zmm)
                || !AreNearlyEqual(previous.Lens.FocalLengthMm, current.Lens.FocalLengthMm)
                || !AreNearlyEqual(previous.Lens.EffectiveFocalLengthMm, current.Lens.EffectiveFocalLengthMm)
                || !AreNearlyEqual(previous.Lens.FocusDistanceMeters, current.Lens.FocusDistanceMeters)
                || !AreNearlyEqual(previous.Lens.IrisFNumber, current.Lens.IrisFNumber)
                || !AreNearlyEqual(previous.Lens.ZoomNormalized, current.Lens.ZoomNormalized)
                || !AreNearlyEqual(previous.Lens.FocusNormalized, current.Lens.FocusNormalized)
                || previous.CameraId != current.CameraId
                || !string.Equals(previous.SourceId, current.SourceId, System.StringComparison.Ordinal);
        }

        private static bool AreNearlyEqual(double left, double right)
        {
            return System.Math.Abs(left - right) <= 0.0001d;
        }
    }
}
