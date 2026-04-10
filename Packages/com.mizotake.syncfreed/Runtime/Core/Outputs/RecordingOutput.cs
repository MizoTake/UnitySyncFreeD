using System.Collections.Generic;
using System.Globalization;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Outputs
{
    public sealed class RecordingOutput : ICameraOutput
    {
        private readonly List<CameraSyncState> frames = new List<CameraSyncState>();

        public IReadOnlyList<CameraSyncState> Frames => frames;

        public string LastCsvLine { get; private set; } = string.Empty;

        public void Send(in CameraSyncState state)
        {
            frames.Add(state);
            LastCsvLine = ToCsvLine(state);
        }

        public void Clear()
        {
            frames.Clear();
            LastCsvLine = string.Empty;
        }

        public static string ToCsvLine(in CameraSyncState state)
        {
            var outputLens = LensStateUtility.MergePhysicalValues(state.CorrectedLens, state.Lens);
            return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2:F6},{3:F6},{4:F6},{5:F3},{6:F3},{7:F3},{8:F3}", state.SourceId ?? string.Empty, state.CameraId, state.Corrected.PanDeg, state.Corrected.TiltDeg, state.Corrected.RollDeg, state.Corrected.Xmm, state.Corrected.Ymm, state.Corrected.Zmm, outputLens.FocalLengthMm);
        }
    }
}
