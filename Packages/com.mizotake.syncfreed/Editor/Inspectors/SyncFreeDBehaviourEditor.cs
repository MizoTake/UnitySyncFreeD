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

        private void OnEnable()
        {
            sourceBehaviourProperty = serializedObject.FindProperty("sourceBehaviour");
            outputBehaviourProperty = serializedObject.FindProperty("outputBehaviour");
            debugLogOutputBehaviourProperty = serializedObject.FindProperty("debugLogOutputBehaviour");
            recordingOutputBehaviourProperty = serializedObject.FindProperty("recordingOutputBehaviour");
            profileAssetProperty = serializedObject.FindProperty("profileAsset");
            applyProfileOnAwakeProperty = serializedObject.FindProperty("applyProfileOnAwake");
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
            EditorGUILayout.HelpBox(profileAssetProperty.objectReferenceValue != null ? "同期方法や delay などの挙動設定は preset Asset 側で編集します。" : "挙動設定 Asset が未設定です。component の現在値で動作しますが、運用では preset Asset 参照を推奨します。", profileAssetProperty.objectReferenceValue != null ? MessageType.Info : MessageType.Warning);
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

            DrawCurrentSettings(behaviour);
            if (EditorApplication.isPlaying)
            {
                DrawRuntimeStatus(behaviour);
            }

            DrawTools();
        }

        private void DrawCurrentSettings(SyncFreeDBehaviour behaviour)
        {
            var expanded = BeginSection("current-settings", "現在の適用値", true);
            if (expanded)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.EnumPopup("同期方法", behaviour.SyncMode);
                    EditorGUILayout.EnumPopup("どの姿勢を送るか", behaviour.OutputPoseKind);
                    EditorGUILayout.EnumPopup("送信タイミング", behaviour.OutputTickMode);
                    if (behaviour.OutputTickMode == OutputTickMode.FixedInterval)
                    {
                        EditorGUILayout.IntField("一定周期送信(ms)", behaviour.FixedIntervalMs);
                    }

                    EditorGUILayout.Toggle("Packet Hex を Console に出す", behaviour.LogPacketHex);
                    EditorGUILayout.ObjectField("Tuning Asset", behaviour.TuningProfileAsset, typeof(SyncTuningProfileAsset), false);
                    EditorGUILayout.ObjectField("Device Asset", behaviour.DeviceProfileAsset, typeof(DeviceProfileAsset), false);
                    EditorGUILayout.ObjectField("Firmware Asset", behaviour.FirmwareBehaviorProfileAsset, typeof(FirmwareBehaviorProfileAsset), false);
                    EditorGUILayout.ObjectField("Lens Asset", behaviour.LensProfileAsset, typeof(LensProfileAsset), false);
                    EditorGUILayout.ObjectField("Mount Asset", behaviour.MountProfileAsset, typeof(MountProfileAsset), false);
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
