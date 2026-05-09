using System.Globalization;
using MizoTake.SyncFreeD.Core.Diagnostics;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class SyncDiagnosticsBehaviour : MonoBehaviour
    {
        [SerializeField] private SyncFreeDBehaviour syncBehaviour;
        [SerializeField] private SyncLogLevel logLevel = SyncLogLevel.Info;
        [SerializeField] private bool showOnGui = true;
        [SerializeField] private Rect rect = new Rect(16f, 104f, 420f, 96f);

        public string LastSummary { get; private set; } = "No data";

        private void Reset()
        {
            syncBehaviour = GetComponent<SyncFreeDBehaviour>();
        }

        private void Awake()
        {
            if (syncBehaviour == null)
            {
                syncBehaviour = GetComponent<SyncFreeDBehaviour>();
            }
        }

        private void LateUpdate()
        {
            if (syncBehaviour == null)
            {
                syncBehaviour = GetComponent<SyncFreeDBehaviour>();
            }

            if (syncBehaviour == null)
            {
                LastSummary = "SyncFreeDBehaviour missing";
                return;
            }

            var diagnostics = syncBehaviour.LastDiagnostics;
            var output = syncBehaviour.OutputBehaviour;
            var infoText = logLevel >= SyncLogLevel.Info ? string.Format(CultureInfo.InvariantCulture, "{0} Cam {1} Mode {2}", syncBehaviour.LastState.SourceId ?? string.Empty, syncBehaviour.LastState.CameraId, syncBehaviour.SyncMode) : string.Empty;
            var zoomText = logLevel >= SyncLogLevel.Info ? string.Format(CultureInfo.InvariantCulture, " Zoom {0:F1}mm", diagnostics.ZoomErrorMm) : string.Empty;
            var packetText = output != null && logLevel >= SyncLogLevel.Packet ? string.Format(CultureInfo.InvariantCulture, " Ck {0:X2} Ua {1:X4} Sent {2} Fail {3} SpreadUs {4}", output.LastChecksum, output.LastUserArea, output.LastSendSuccessCount, output.TotalSendFailureCount, output.LastDestinationSpreadMicroseconds) : string.Empty;
            var destinationText = output != null && logLevel >= SyncLogLevel.Verbose ? string.Format(CultureInfo.InvariantCulture, " DestSpreadUs {0} Dest {1}", output.LastDestinationSpreadMicroseconds, output.LastDestinationDiagnosticCount) : string.Empty;
            var correctionText = logLevel >= SyncLogLevel.Info ? string.Format(CultureInfo.InvariantCulture, " Corr {0}", syncBehaviour.CorrectionAppliedCount) : string.Empty;
            var verboseText = logLevel >= SyncLogLevel.Verbose ? string.Format(CultureInfo.InvariantCulture, " TrackDelay {0} VideoDelay {1} Lens {2} Fallback {3} Frame {4}", diagnostics.TrackingDelayMs, diagnostics.VideoAlignmentDelayMs, diagnostics.IsLensValid, diagnostics.IsFallbackMode, syncBehaviour.LastState.Timing.FrameModulo16) : string.Empty;
            LastSummary = string.Format(CultureInfo.InvariantCulture, "{0} Pan {1:F2} Tilt {2:F2} Roll {3:F2} Pos {4:F1}mm Tracking {5} Degraded {6}{7}{8}{9}{10}{11}", infoText, diagnostics.PanErrorDeg, diagnostics.TiltErrorDeg, diagnostics.RollErrorDeg, diagnostics.PositionErrorMm, diagnostics.IsTrackingValid, diagnostics.IsDegraded, zoomText, correctionText, packetText, verboseText, destinationText);
        }

        private void OnGUI()
        {
            if (!showOnGui)
            {
                return;
            }

            GUI.Box(rect, LastSummary);
        }
    }
}
