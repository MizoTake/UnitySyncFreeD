using MizoTake.SyncFreeD.Core.Diagnostics;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Sync
{
    public sealed class SyncTickProcessor
    {
        private readonly CameraSyncEngine synchronizer;
        private readonly DelayCompensator outputDelayCompensator;

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
            var context = new CameraSyncContext(request.TimestampTicks, request.ObservedFrame, request.CommandFrame, request.SyncMode, tuning);
            var state = synchronizer.Update(context, request.OutputPoseKind);
            state = LensProfileApplicator.Apply(state, request.LensProfile);
            state = ApplyOutputDelay(state, tuning.OutputDelayMs);
            return new SyncTickResult(state, SyncDiagnosticsEvaluator.Evaluate(state));
        }

        public void Reset()
        {
            outputDelayCompensator.Clear();
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
    }
}
