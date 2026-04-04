using MizoTake.SyncFreeD.Core.Diagnostics;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;

namespace MizoTake.SyncFreeD.Editor.Support
{
    public readonly struct SyncFreeDDebugValueRow
    {
        public SyncFreeDDebugValueRow(string label, string command, string predicted, string observed, string corrected)
        {
            Label = label;
            Command = command;
            Predicted = predicted;
            Observed = observed;
            Corrected = corrected;
        }

        public string Label { get; }
        public string Command { get; }
        public string Predicted { get; }
        public string Observed { get; }
        public string Corrected { get; }
    }

    public readonly struct SyncFreeDDebugSnapshot
    {
        public SyncFreeDDebugSnapshot(SyncFreeDDebugValueRow[] poseRows, SyncFreeDDebugValueRow[] lensRows, string[] destinationLines, long destinationSpreadMicroseconds)
        {
            PoseRows = poseRows;
            LensRows = lensRows;
            DestinationLines = destinationLines;
            DestinationSpreadMicroseconds = destinationSpreadMicroseconds;
        }

        public SyncFreeDDebugValueRow[] PoseRows { get; }
        public SyncFreeDDebugValueRow[] LensRows { get; }
        public string[] DestinationLines { get; }
        public long DestinationSpreadMicroseconds { get; }
    }

    public static class SyncFreeDDebugSnapshotBuilder
    {
        public static SyncFreeDDebugSnapshot Build(in CameraSyncState state, in SyncDiagnosticsSnapshot diagnostics, FreeDUdpOutputBehaviour output)
        {
            var poseRows = new[]
            {
                new SyncFreeDDebugValueRow("PRY", FormatPose(state.Command), FormatPose(state.Predicted), FormatPose(state.Observed), FormatPose(state.Corrected)),
                new SyncFreeDDebugValueRow("XYZ(mm)", FormatPosition(state.Command), FormatPosition(state.Predicted), FormatPosition(state.Observed), FormatPosition(state.Corrected))
            };
            var lensRows = new[]
            {
                new SyncFreeDDebugValueRow("Zoom(mm)", FormatLens(state.CommandLens.FocalLengthMm), FormatLens(state.PredictedLens.FocalLengthMm), FormatLens(state.ObservedLens.FocalLengthMm), FormatLens(state.CorrectedLens.FocalLengthMm)),
                new SyncFreeDDebugValueRow("Focus(m)", FormatLens(state.CommandLens.FocusDistanceMeters), FormatLens(state.PredictedLens.FocusDistanceMeters), FormatLens(state.ObservedLens.FocusDistanceMeters), FormatLens(state.CorrectedLens.FocusDistanceMeters)),
                new SyncFreeDDebugValueRow("Iris(F)", FormatLens(state.CommandLens.IrisFNumber), FormatLens(state.PredictedLens.IrisFNumber), FormatLens(state.ObservedLens.IrisFNumber), FormatLens(state.CorrectedLens.IrisFNumber)),
                new SyncFreeDDebugValueRow("Frame", state.Timing.FrameModulo16.ToString(), state.Timing.FrameModulo16.ToString(), state.Timing.FrameModulo16.ToString(), state.Timing.FrameModulo16.ToString())
            };
            var destinationLines = BuildDestinationLines(output);
            return new SyncFreeDDebugSnapshot(poseRows, lensRows, destinationLines, output != null ? output.LastDestinationSpreadMicroseconds : 0L);
        }

        public static string[] BuildDestinationLines(FreeDUdpOutputBehaviour output)
        {
            if (output == null)
            {
                return new[] { "Output not found" };
            }

            return BuildDestinationLines(output.LastDestinationDiagnostics);
        }

        public static string[] BuildDestinationLines(FreeDUdpDestinationDiagnostic[] diagnostics)
        {
            if (diagnostics == null || diagnostics.Length == 0)
            {
                return new[] { "No destination diagnostics" };
            }

            var lines = new string[diagnostics.Length];
            for (var i = 0; i < diagnostics.Length; i++)
            {
                var destination = diagnostics[i];
                lines[i] = string.Format("#{0} {1} {2} {3}us", destination.Order + 1, destination.Endpoint, destination.Success ? "OK" : "FAIL", destination.ElapsedMicroseconds);
            }

            return lines;
        }

        public static string[] BuildDestinationLines(FreeDUdpDestinationDiagnostics[] diagnostics)
        {
            if (diagnostics == null || diagnostics.Length == 0)
            {
                return new[] { "No destination diagnostics" };
            }

            var lines = new string[diagnostics.Length];
            for (var i = 0; i < diagnostics.Length; i++)
            {
                var destination = diagnostics[i];
                lines[i] = string.Format("#{0} {1} {2}:{3} {4} {5}us", destination.Order + 1, destination.Label, destination.IpAddress, destination.Port, destination.Succeeded ? "OK" : "FAIL", destination.OffsetMicroseconds);
            }

            return lines;
        }

        private static string FormatPose(in PoseState pose)
        {
            return string.Format("{0:F2}, {1:F2}, {2:F2}", pose.PanDeg, pose.TiltDeg, pose.RollDeg);
        }

        private static string FormatPosition(in PoseState pose)
        {
            return string.Format("{0:F1}, {1:F1}, {2:F1}", pose.Xmm, pose.Ymm, pose.Zmm);
        }

        private static string FormatLens(double value)
        {
            return value.ToString("F2");
        }
    }
}
