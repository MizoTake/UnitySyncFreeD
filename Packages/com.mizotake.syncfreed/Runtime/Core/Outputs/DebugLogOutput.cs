using System;
using System.Globalization;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Outputs
{
    public sealed class DebugLogOutput : ICameraOutput
    {
        private readonly Action<string> sink;

        public DebugLogOutput(Action<string> sink)
        {
            this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public string LastMessage { get; private set; } = string.Empty;

        public void Send(in CameraSyncState state)
        {
            LastMessage = string.Format(CultureInfo.InvariantCulture, "CameraId={0} SourceId={1} Pan={2:F2} Tilt={3:F2} Roll={4:F2} X={5:F1} Y={6:F1} Z={7:F1}", state.CameraId, state.SourceId ?? string.Empty, state.Corrected.PanDeg, state.Corrected.TiltDeg, state.Corrected.RollDeg, state.Corrected.Xmm, state.Corrected.Ymm, state.Corrected.Zmm);
            sink(LastMessage);
        }
    }
}
