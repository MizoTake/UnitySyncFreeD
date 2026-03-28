using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using UnityEditor;
using UnityEngine;

namespace MizoTake.SyncFreeD.Editor.Inspectors
{
    [CustomEditor(typeof(SyncFreeDBehaviour))]
    public sealed class SyncFreeDBehaviourEditor : UnityEditor.Editor
    {
        private SerializedProperty sourceBehaviourProperty;
        private SerializedProperty outputBehaviourProperty;
        private SerializedProperty debugLogOutputBehaviourProperty;
        private SerializedProperty recordingOutputBehaviourProperty;
        private SerializedProperty syncModeProperty;
        private SerializedProperty outputPoseKindProperty;
        private SerializedProperty tuningProfileAssetProperty;
        private SerializedProperty deviceProfileAssetProperty;
        private SerializedProperty firmwareBehaviorProfileAssetProperty;
        private SerializedProperty lensProfileAssetProperty;
        private SerializedProperty mountProfileAssetProperty;
        private SerializedProperty outputTickModeProperty;
        private SerializedProperty fixedIntervalMsProperty;
        private SerializedProperty tuningProperty;
        private SerializedProperty logPacketHexProperty;

        private void OnEnable()
        {
            sourceBehaviourProperty = serializedObject.FindProperty("sourceBehaviour");
            outputBehaviourProperty = serializedObject.FindProperty("outputBehaviour");
            debugLogOutputBehaviourProperty = serializedObject.FindProperty("debugLogOutputBehaviour");
            recordingOutputBehaviourProperty = serializedObject.FindProperty("recordingOutputBehaviour");
            syncModeProperty = serializedObject.FindProperty("syncMode");
            outputPoseKindProperty = serializedObject.FindProperty("outputPoseKind");
            tuningProfileAssetProperty = serializedObject.FindProperty("tuningProfileAsset");
            deviceProfileAssetProperty = serializedObject.FindProperty("deviceProfileAsset");
            firmwareBehaviorProfileAssetProperty = serializedObject.FindProperty("firmwareBehaviorProfileAsset");
            lensProfileAssetProperty = serializedObject.FindProperty("lensProfileAsset");
            mountProfileAssetProperty = serializedObject.FindProperty("mountProfileAsset");
            outputTickModeProperty = serializedObject.FindProperty("outputTickMode");
            fixedIntervalMsProperty = serializedObject.FindProperty("fixedIntervalMs");
            tuningProperty = serializedObject.FindProperty("tuning");
            logPacketHexProperty = serializedObject.FindProperty("logPacketHex");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sourceBehaviourProperty);
            EditorGUILayout.PropertyField(outputBehaviourProperty);
            EditorGUILayout.PropertyField(debugLogOutputBehaviourProperty);
            EditorGUILayout.PropertyField(recordingOutputBehaviourProperty);
            EditorGUILayout.PropertyField(syncModeProperty);
            EditorGUILayout.PropertyField(outputPoseKindProperty);
            EditorGUILayout.PropertyField(outputTickModeProperty);
            if ((Core.Models.OutputTickMode)outputTickModeProperty.enumValueIndex == Core.Models.OutputTickMode.FixedInterval)
            {
                EditorGUILayout.PropertyField(fixedIntervalMsProperty);
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Profiles", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(tuningProfileAssetProperty);
            EditorGUILayout.PropertyField(deviceProfileAssetProperty);
            EditorGUILayout.PropertyField(firmwareBehaviorProfileAssetProperty);
            EditorGUILayout.PropertyField(lensProfileAssetProperty);
            EditorGUILayout.PropertyField(mountProfileAssetProperty);
            EditorGUILayout.PropertyField(tuningProperty, true);
            EditorGUILayout.PropertyField(logPacketHexProperty);
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            DrawRuntimeStatus((SyncFreeDBehaviour)target);
        }

        private static void DrawRuntimeStatus(SyncFreeDBehaviour behaviour)
        {
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Source ID", behaviour.LastState.SourceId ?? string.Empty);
                EditorGUILayout.IntField("Camera ID", behaviour.LastState.CameraId);
                EditorGUILayout.Toggle("Tracking Valid", behaviour.LastState.Validity.IsTrackingValid);
                EditorGUILayout.Toggle("Degraded", behaviour.LastState.Validity.IsDegraded);
                EditorGUILayout.DoubleField("Pan Error", behaviour.LastDiagnostics.PanErrorDeg);
                EditorGUILayout.DoubleField("Tilt Error", behaviour.LastDiagnostics.TiltErrorDeg);
                EditorGUILayout.DoubleField("Roll Error", behaviour.LastDiagnostics.RollErrorDeg);
                EditorGUILayout.DoubleField("Position Error (mm)", behaviour.LastDiagnostics.PositionErrorMm);
                EditorGUILayout.DoubleField("Zoom Error (mm)", behaviour.LastDiagnostics.ZoomErrorMm);
                EditorGUILayout.IntField("Tracking Delay (ms)", behaviour.LastDiagnostics.TrackingDelayMs);
                EditorGUILayout.IntField("Video Delay (ms)", behaviour.LastDiagnostics.VideoAlignmentDelayMs);
                EditorGUILayout.Toggle("Correction Applied", behaviour.LastDiagnostics.CorrectionApplied);
                EditorGUILayout.Toggle("Lens Valid", behaviour.LastDiagnostics.IsLensValid);
                EditorGUILayout.Toggle("Fallback Mode", behaviour.LastDiagnostics.IsFallbackMode);
                EditorGUILayout.IntField("Correction Count", behaviour.CorrectionAppliedCount);
                EditorGUILayout.Toggle("Tuning Asset", behaviour.TuningProfileAsset != null);
                EditorGUILayout.Toggle("Device Asset", behaviour.DeviceProfileAsset != null);
                EditorGUILayout.Toggle("Firmware Asset", behaviour.FirmwareBehaviorProfileAsset != null);
                EditorGUILayout.Toggle("Lens Asset", behaviour.LensProfileAsset != null);
                EditorGUILayout.Toggle("Mount Asset", behaviour.MountProfileAsset != null);
                var output = behaviour.GetComponent<FreeDUdpOutputBehaviour>();
                if (output != null)
                {
                    EditorGUILayout.IntField("Send Success", output.LastSendSuccessCount);
                    EditorGUILayout.IntField("Send Failures", output.TotalSendFailureCount);
                    EditorGUILayout.TextField("Checksum", output.LastChecksum.ToString("X2"));
                }
            }

            if (GUILayout.Button("Open Debug Window"))
            {
                Windows.SyncFreeDDebugWindow.OpenWindow();
            }

            if (GUILayout.Button("Open Setup Wizard"))
            {
                Setup.SyncFreeDSetupWizard.OpenWindow();
            }
        }
    }
}
