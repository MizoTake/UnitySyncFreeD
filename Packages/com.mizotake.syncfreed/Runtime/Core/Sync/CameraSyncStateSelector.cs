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
                    selectedState.CorrectedLens = LensStateUtility.MergePhysicalValues(state.CommandLens, state.Lens);
                    selectedState.Lens = selectedState.CorrectedLens;
                    break;
                case OutputPoseKind.Predicted:
                    selectedState.Corrected = state.Predicted;
                    selectedState.CorrectedLens = LensStateUtility.MergePhysicalValues(state.PredictedLens, state.Lens);
                    selectedState.Lens = selectedState.CorrectedLens;
                    break;
                case OutputPoseKind.Observed:
                    selectedState.Corrected = state.Observed;
                    selectedState.CorrectedLens = LensStateUtility.MergePhysicalValues(state.ObservedLens, state.PredictedLens);
                    selectedState.Lens = selectedState.CorrectedLens;
                    break;
                case OutputPoseKind.Blended:
                    selectedState.Corrected = StateInterpolator.Lerp(state.Predicted, state.Observed, 0.5d);
                    selectedState.CorrectedLens = LensStateUtility.MergePhysicalValues(state.ObservedLens, state.PredictedLens);
                    selectedState.Lens = selectedState.CorrectedLens;
                    break;
                case OutputPoseKind.Corrected:
                default:
                    selectedState.Corrected = state.Corrected;
                    selectedState.CorrectedLens = LensStateUtility.MergePhysicalValues(state.CorrectedLens, state.Lens);
                    selectedState.Lens = selectedState.CorrectedLens;
                    break;
            }

            return selectedState;
        }
    }
}
