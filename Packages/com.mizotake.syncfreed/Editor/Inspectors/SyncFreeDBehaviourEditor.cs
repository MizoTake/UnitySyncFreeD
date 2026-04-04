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
            EditorGUILayout.LabelField("基本設定", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sourceBehaviourProperty, new GUIContent("入力元 Behaviour"));
            EditorGUILayout.PropertyField(outputBehaviourProperty, new GUIContent("FreeD 出力 Behaviour"));
            EditorGUILayout.PropertyField(debugLogOutputBehaviourProperty, new GUIContent("Debug 出力 Behaviour"));
            EditorGUILayout.PropertyField(recordingOutputBehaviourProperty, new GUIContent("記録出力 Behaviour"));
            EditorGUILayout.PropertyField(syncModeProperty, new GUIContent("同期方法"));
            EditorGUILayout.PropertyField(outputPoseKindProperty, new GUIContent("どの姿勢を送るか"));
            EditorGUILayout.PropertyField(outputTickModeProperty, new GUIContent("送信タイミング"));
            if ((Core.Models.OutputTickMode)outputTickModeProperty.enumValueIndex == Core.Models.OutputTickMode.FixedInterval)
            {
                EditorGUILayout.PropertyField(fixedIntervalMsProperty, new GUIContent("一定周期送信(ms)"));
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("保存して再利用する設定", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(tuningProfileAssetProperty, new GUIContent("同期 preset Asset"));
            EditorGUILayout.PropertyField(deviceProfileAssetProperty, new GUIContent("機材 preset Asset"));
            EditorGUILayout.PropertyField(firmwareBehaviorProfileAssetProperty, new GUIContent("firmware preset Asset"));
            EditorGUILayout.PropertyField(lensProfileAssetProperty, new GUIContent("レンズ preset Asset"));
            EditorGUILayout.PropertyField(mountProfileAssetProperty, new GUIContent("設置 preset Asset"));
            EditorGUILayout.PropertyField(tuningProperty, true);
            EditorGUILayout.PropertyField(logPacketHexProperty, new GUIContent("Packet Hex を Console に出す"));
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            DrawRuntimeStatus((SyncFreeDBehaviour)target);
        }

        private static void DrawRuntimeStatus(SyncFreeDBehaviour behaviour)
        {
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            if (behaviour.HasFirmwareBehaviorWarning)
            {
                EditorGUILayout.HelpBox(behaviour.FirmwareBehaviorWarning, MessageType.Warning);
            }
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

            if (GUILayout.Button("Open Operator Window"))
            {
                Windows.SyncFreeDOperatorWindow.OpenWindow();
            }

            if (GUILayout.Button("Open Setup Wizard"))
            {
                Setup.SyncFreeDSetupWizard.OpenWindow();
            }
        }
    }
}
