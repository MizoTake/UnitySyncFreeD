using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Sync
{
    public static class CameraSyncStateSelector
    {
        public static CameraSyncState SelectOutputPose(in CameraSyncState state, OutputPoseKind outputPoseKind)
        {
            var selectedState = state;
            switch (outputPoseKind)
            {
                case OutputPoseKind.Command:
                    selectedState.Corrected = state.Command;
                    selectedState.CorrectedLens = state.CommandLens;
                    selectedState.Lens = state.CommandLens;
                    break;
                case OutputPoseKind.Predicted:
                    selectedState.Corrected = state.Predicted;
                    selectedState.CorrectedLens = state.PredictedLens;
                    selectedState.Lens = state.PredictedLens;
                    break;
                case OutputPoseKind.Observed:
                    selectedState.Corrected = state.Observed;
                    selectedState.CorrectedLens = state.ObservedLens;
                    selectedState.Lens = state.ObservedLens;
                    break;
                case OutputPoseKind.Blended:
                    selectedState.Corrected = StateInterpolator.Lerp(state.Predicted, state.Observed, 0.5d);
                    selectedState.CorrectedLens = state.ObservedLens.FocalLengthMm != 0d ? state.ObservedLens : state.PredictedLens;
                    selectedState.Lens = selectedState.CorrectedLens;
                    break;
                case OutputPoseKind.Corrected:
                default:
                    selectedState.Corrected = state.Corrected;
                    selectedState.CorrectedLens = state.CorrectedLens.FocalLengthMm != 0d ? state.CorrectedLens : state.Lens;
                    selectedState.Lens = selectedState.CorrectedLens;
                    break;
            }

            return selectedState;
        }
    }
}
