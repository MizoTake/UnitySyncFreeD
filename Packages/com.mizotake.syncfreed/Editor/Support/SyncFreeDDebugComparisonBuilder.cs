using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Editor.Support
{
    public static class SyncFreeDDebugComparisonBuilder
    {
        public static SyncFreeDDebugComparisonRow[] BuildRows(in CameraSyncState state)
        {
            return new[]
            {
                new SyncFreeDDebugComparisonRow("PRY", FormatPose(state.Command), FormatPose(state.Predicted), FormatPose(state.Observed), FormatPose(state.Corrected)),
                new SyncFreeDDebugComparisonRow("XYZ(mm)", FormatPosition(state.Command), FormatPosition(state.Predicted), FormatPosition(state.Observed), FormatPosition(state.Corrected)),
                new SyncFreeDDebugComparisonRow("Lens", FormatLens(state.CommandLens), FormatLens(state.PredictedLens), FormatLens(state.ObservedLens), FormatLens(state.CorrectedLens)),
                new SyncFreeDDebugComparisonRow("Frame", state.Timing.FrameModulo16.ToString(), state.Timing.FrameModulo16.ToString(), state.Timing.FrameModulo16.ToString(), state.Timing.FrameModulo16.ToString())
            };
        }

        private static string FormatPose(in PoseState pose)
        {
            return $"{pose.PanDeg:F2}, {pose.TiltDeg:F2}, {pose.RollDeg:F2}";
        }

        private static string FormatPosition(in PoseState pose)
        {
            return $"{pose.Xmm:F1}, {pose.Ymm:F1}, {pose.Zmm:F1}";
        }

        private static string FormatLens(in LensState lens)
        {
            return $"{lens.FocalLengthMm:F2}mm / {lens.FocusDistanceMeters:F2}m / F{lens.IrisFNumber:F2}";
        }
    }

    public readonly struct SyncFreeDDebugComparisonRow
    {
        public SyncFreeDDebugComparisonRow(string label, string command, string predicted, string observed, string corrected)
        {
            Label = label ?? string.Empty;
            Command = command ?? string.Empty;
            Predicted = predicted ?? string.Empty;
            Observed = observed ?? string.Empty;
            Corrected = corrected ?? string.Empty;
        }

        public string Label { get; }
        public string Command { get; }
        public string Predicted { get; }
        public string Observed { get; }
        public string Corrected { get; }
    }
}
