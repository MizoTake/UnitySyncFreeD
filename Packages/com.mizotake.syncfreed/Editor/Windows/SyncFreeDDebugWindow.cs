using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using UnityEditor;
using UnityEngine;

namespace MizoTake.SyncFreeD.Editor.Windows
{
    public sealed class SyncFreeDDebugWindow : EditorWindow
    {
        private SyncFreeDBehaviour targetBehaviour;
        private Vector2 scrollPosition;

        [MenuItem("Tools/SyncFreeD/Debug Window")]
        public static void OpenWindow()
        {
            GetWindow<SyncFreeDDebugWindow>("SyncFreeD Debug");
        }

        private void OnSelectionChange()
        {
            if (Selection.activeGameObject != null)
            {
                targetBehaviour = Selection.activeGameObject.GetComponent<SyncFreeDBehaviour>();
            }

            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
            targetBehaviour = (SyncFreeDBehaviour)EditorGUILayout.ObjectField(targetBehaviour, typeof(SyncFreeDBehaviour), true);
            if (targetBehaviour == null)
            {
                EditorGUILayout.HelpBox("SyncFreeDBehaviour を選択するか ObjectField に指定してください。", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            var state = targetBehaviour.LastState;
            var diagnostics = targetBehaviour.LastDiagnostics;
            EditorGUILayout.LabelField("State", EditorStyles.boldLabel);
            EditorGUILayout.TextField("SourceId", state.SourceId ?? string.Empty);
            EditorGUILayout.IntField("CameraId", state.CameraId);
            EditorGUILayout.Vector3Field("Command PRY", new Vector3((float)state.Command.PanDeg, (float)state.Command.TiltDeg, (float)state.Command.RollDeg));
            EditorGUILayout.Vector3Field("Observed PRY", new Vector3((float)state.Observed.PanDeg, (float)state.Observed.TiltDeg, (float)state.Observed.RollDeg));
            EditorGUILayout.Vector3Field("Corrected PRY", new Vector3((float)state.Corrected.PanDeg, (float)state.Corrected.TiltDeg, (float)state.Corrected.RollDeg));
            EditorGUILayout.Vector3Field("Corrected XYZ(mm)", new Vector3((float)state.Corrected.Xmm, (float)state.Corrected.Ymm, (float)state.Corrected.Zmm));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.DoubleField("Pan Error", diagnostics.PanErrorDeg);
            EditorGUILayout.DoubleField("Tilt Error", diagnostics.TiltErrorDeg);
            EditorGUILayout.DoubleField("Roll Error", diagnostics.RollErrorDeg);
            EditorGUILayout.DoubleField("Position Error (mm)", diagnostics.PositionErrorMm);
            EditorGUILayout.DoubleField("Zoom Error (mm)", diagnostics.ZoomErrorMm);
            EditorGUILayout.IntField("Tracking Delay (ms)", diagnostics.TrackingDelayMs);
            EditorGUILayout.IntField("Video Delay (ms)", diagnostics.VideoAlignmentDelayMs);
            EditorGUILayout.Toggle("Tracking Valid", diagnostics.IsTrackingValid);
            EditorGUILayout.Toggle("Lens Valid", diagnostics.IsLensValid);
            EditorGUILayout.Toggle("Degraded", diagnostics.IsDegraded);
            EditorGUILayout.Toggle("Fallback Mode", diagnostics.IsFallbackMode);
            EditorGUILayout.Toggle("Correction Applied", diagnostics.CorrectionApplied);
            EditorGUILayout.IntField("Correction Count", targetBehaviour.CorrectionAppliedCount);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Profiles", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Tuning", targetBehaviour.TuningProfileAsset, typeof(ScriptableObjects.SyncTuningProfileAsset), false);
            EditorGUILayout.ObjectField("Device", targetBehaviour.DeviceProfileAsset, typeof(ScriptableObjects.DeviceProfileAsset), false);
            EditorGUILayout.ObjectField("Firmware", targetBehaviour.FirmwareBehaviorProfileAsset, typeof(ScriptableObjects.FirmwareBehaviorProfileAsset), false);
            EditorGUILayout.ObjectField("Lens", targetBehaviour.LensProfileAsset, typeof(ScriptableObjects.LensProfileAsset), false);
            EditorGUILayout.ObjectField("Mount", targetBehaviour.MountProfileAsset, typeof(ScriptableObjects.MountProfileAsset), false);
            var output = targetBehaviour.GetComponent<FreeDUdpOutputBehaviour>();
            var debugOutput = targetBehaviour.GetComponent<DebugLogOutputBehaviour>();
            var recordingOutput = targetBehaviour.GetComponent<RecordingOutputBehaviour>();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Packet", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(output != null ? output.LastPacketHex : "Output not found", EditorStyles.textArea, GUILayout.Height(48f));
            if (output != null)
            {
                EditorGUILayout.IntField("Packet Length", output.PacketLength);
                EditorGUILayout.IntField("Configured Destinations", output.GetConfiguredDestinationCount());
                EditorGUILayout.IntField("Last Requested Destinations", output.LastRequestedDestinationCount);
                EditorGUILayout.IntField("Send Success", output.LastSendSuccessCount);
                EditorGUILayout.IntField("Send Failures", output.TotalSendFailureCount);
                EditorGUILayout.Toggle("Skipped By Filter", output.LastSendSkippedByFilter);
                EditorGUILayout.Toggle("Multicast Configured", output.IsMulticastConfigured);
                EditorGUILayout.TextField("Checksum", output.LastChecksum.ToString("X2"));
                EditorGUILayout.TextField("User Area", output.LastUserArea.ToString("X4"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Optional Outputs", EditorStyles.boldLabel);
            EditorGUILayout.TextField("Debug Log", debugOutput != null ? debugOutput.LastMessage : "Not attached");
            EditorGUILayout.TextField("Recording", recordingOutput != null ? recordingOutput.LastCsvLine : "Not attached");
            EditorGUILayout.EndScrollView();
            Repaint();
        }
    }
}
