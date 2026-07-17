using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using MizoTake.SyncFreeD.ScriptableObjects;
using MizoTake.SyncFreeD.Core.Models;
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
        private SerializedProperty profileAssetProperty;
        private SerializedProperty applyProfileOnAwakeProperty;
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
            profileAssetProperty = serializedObject.FindProperty("profileAsset");
            applyProfileOnAwakeProperty = serializedObject.FindProperty("applyProfileOnAwake");
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
            if (sourceBehaviourProperty.objectReferenceValue != null && !(sourceBehaviourProperty.objectReferenceValue is ICameraFrameProvider))
            {
                EditorGUILayout.HelpBox("入力元 Behaviour は ICameraFrameProvider を実装している必要があります。", MessageType.Error);
            }

            EditorGUILayout.PropertyField(outputBehaviourProperty, new GUIContent("FreeD 出力 Behaviour"));
            EditorGUILayout.PropertyField(debugLogOutputBehaviourProperty, new GUIContent("Debug 出力 Behaviour"));
            EditorGUILayout.PropertyField(recordingOutputBehaviourProperty, new GUIContent("記録出力 Behaviour"));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("挙動 preset", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(profileAssetProperty, new GUIContent("挙動設定 Asset"));
            EditorGUILayout.PropertyField(applyProfileOnAwakeProperty, new GUIContent("開始時に preset を反映"));
            var profileAssigned = profileAssetProperty.objectReferenceValue != null;
            var profileControlsSettings = profileAssigned && applyProfileOnAwakeProperty.boolValue;
            if (profileControlsSettings)
            {
                EditorGUILayout.HelpBox("開始時に preset を反映するため、同期設定は Asset 側で編集します。", MessageType.Info);
            }
            else if (profileAssigned)
            {
                EditorGUILayout.HelpBox("自動反映が無効なため、下の component 設定を直接編集できます。Apply Preset Now を押した時だけ Asset の値で上書きします。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("挙動設定 Asset が未設定です。下の component 設定を直接編集できます。", MessageType.Warning);
            }

            DrawCurrentSettings(profileControlsSettings);
            serializedObject.ApplyModifiedProperties();

            using (new EditorGUI.DisabledScope(profileAssetProperty.objectReferenceValue == null))
            {
                if (GUILayout.Button("Apply Preset Now"))
                {
                    ((SyncFreeDBehaviour)target).ApplyProfile();
                    EditorUtility.SetDirty(target);
                }
            }

            var behaviour = (SyncFreeDBehaviour)target;
            if (behaviour.HasFirmwareBehaviorWarning)
            {
                EditorGUILayout.HelpBox(behaviour.FirmwareBehaviorWarning, MessageType.Warning);
            }

            if (EditorApplication.isPlaying)
            {
                DrawRuntimeStatus(behaviour);
            }

            DrawTools();
        }

        private void DrawCurrentSettings(bool readOnly)
        {
            var expanded = BeginSection("current-settings", readOnly ? "現在の適用値" : "Component 同期設定", true);
            if (expanded)
            {
                using (new EditorGUI.DisabledScope(readOnly))
                {
                    EditorGUILayout.PropertyField(syncModeProperty, new GUIContent("同期方法"));
                    EditorGUILayout.PropertyField(outputPoseKindProperty, new GUIContent("どの姿勢を送るか"));
                    EditorGUILayout.PropertyField(outputTickModeProperty, new GUIContent("送信タイミング"));
                    if ((OutputTickMode)outputTickModeProperty.enumValueIndex == OutputTickMode.FixedInterval)
                    {
                        EditorGUILayout.PropertyField(fixedIntervalMsProperty, new GUIContent("一定周期送信(ms)"));
                    }

                    EditorGUILayout.PropertyField(logPacketHexProperty, new GUIContent("Packet Hex を Console に出す"));
                    EditorGUILayout.PropertyField(tuningProfileAssetProperty, new GUIContent("Tuning Asset"));
                    if (tuningProfileAssetProperty.objectReferenceValue == null)
                    {
                        EditorGUILayout.PropertyField(tuningProperty, new GUIContent("Component Tuning"), true);
                    }

                    EditorGUILayout.PropertyField(deviceProfileAssetProperty, new GUIContent("Device Asset"));
                    EditorGUILayout.PropertyField(firmwareBehaviorProfileAssetProperty, new GUIContent("Firmware Asset"));
                    EditorGUILayout.PropertyField(lensProfileAssetProperty, new GUIContent("Lens Asset"));
                    EditorGUILayout.PropertyField(mountProfileAssetProperty, new GUIContent("Mount Asset"));
                }
            }

            EndSection();
        }

        private void DrawRuntimeStatus(SyncFreeDBehaviour behaviour)
        {
            var expanded = BeginSection("runtime", "Runtime", true);
            if (expanded)
            {
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
                    var output = behaviour.OutputBehaviour;
                    if (output != null)
                    {
                        EditorGUILayout.IntField("Send Success", output.LastSendSuccessCount);
                        EditorGUILayout.IntField("Send Failures", output.TotalSendFailureCount);
                        EditorGUILayout.TextField("Checksum", output.LastChecksum.ToString("X2"));
                    }
                }
            }

            EndSection();
        }

        private void DrawTools()
        {
            var expanded = BeginSection("tools", "ツール", false);
            if (expanded)
            {
                if (GUILayout.Button("Open Debug Window"))
                {
                    Windows.SyncFreeDDebugWindow.OpenWindow();
                }

                if (GUILayout.Button("Open Operator Window"))
                {
                    var window = Windows.SyncFreeDOperatorWindow.OpenWindow();
                    window.SetTarget((SyncFreeDBehaviour)target);
                }

                if (GUILayout.Button("Open Setup Wizard"))
                {
                    Setup.SyncFreeDSetupWizard.OpenWindow();
                }
            }

            EndSection();
        }

        private bool BeginSection(string key, string label, bool defaultExpanded)
        {
            var stateKey = $"{nameof(SyncFreeDBehaviourEditor)}.{target.GetInstanceID()}.{key}";
            var expanded = SessionState.GetBool(stateKey, defaultExpanded);
            expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, label);
            SessionState.SetBool(stateKey, expanded);
            return expanded;
        }

        private static void EndSection()
        {
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
