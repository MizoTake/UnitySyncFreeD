using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using UnityEditor;
using UnityEngine;

namespace MizoTake.SyncFreeD.Editor.Inspectors
{
    [CustomEditor(typeof(FreeDControllerBehaviour))]
    public sealed class FreeDControllerBehaviourEditor : UnityEditor.Editor
    {
        private SerializedProperty controlledTransformProperty;
        private SerializedProperty controlledCameraProperty;
        private SerializedProperty profileAssetProperty;
        private SerializedProperty applyProfileOnAwakeProperty;

        private void OnEnable()
        {
            controlledTransformProperty = serializedObject.FindProperty("controlledTransform");
            controlledCameraProperty = serializedObject.FindProperty("controlledCamera");
            profileAssetProperty = serializedObject.FindProperty("profileAsset");
            applyProfileOnAwakeProperty = serializedObject.FindProperty("applyProfileOnAwake");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("操作対象", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(controlledTransformProperty, new GUIContent("動かす Transform"));
            EditorGUILayout.PropertyField(controlledCameraProperty, new GUIContent("レンズを変える Camera"));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("操作 preset", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(profileAssetProperty, new GUIContent("操作 preset Asset"));
            EditorGUILayout.PropertyField(applyProfileOnAwakeProperty, new GUIContent("開始時に preset を反映"));
            EditorGUILayout.HelpBox(profileAssetProperty.objectReferenceValue != null ? "操作パラメーターは操作 preset Asset 側で編集します。" : "操作 preset Asset が未設定です。component の現在値で動作しますが、運用では Asset 参照を推奨します。", profileAssetProperty.objectReferenceValue != null ? MessageType.Info : MessageType.Warning);
            serializedObject.ApplyModifiedProperties();

            using (new EditorGUI.DisabledScope(profileAssetProperty.objectReferenceValue == null))
            {
                if (GUILayout.Button("Apply Preset Now"))
                {
                    ((FreeDControllerBehaviour)target).ApplyProfile();
                    EditorUtility.SetDirty(target);
                }
            }

            var behaviour = (FreeDControllerBehaviour)target;
            DrawPresetSnapshot(behaviour);
            if (behaviour.AllowKeyboardControl)
            {
                DrawKeyboardHelp();
            }

            DrawTools();
        }

        private void DrawPresetSnapshot(FreeDControllerBehaviour behaviour)
        {
            var expanded = BeginSection("current-settings", "現在の適用値", true);
            if (expanded)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Toggle("キーボード操作", behaviour.AllowKeyboardControl);
                    EditorGUILayout.Toggle("Unscaled Time", behaviour.UseUnscaledTime);
                    EditorGUILayout.FloatField("移動の速さ", behaviour.MoveSpeedMetersPerSecond);
                    EditorGUILayout.FloatField("向き変更の速さ", behaviour.RotateSpeedDegreesPerSecond);
                    EditorGUILayout.FloatField("傾き変更の速さ", behaviour.RollSpeedDegreesPerSecond);
                    EditorGUILayout.FloatField("ズーム変更の速さ", behaviour.FocalLengthSpeedMmPerSecond);
                    EditorGUILayout.FloatField("フォーカス変更の速さ", behaviour.FocusDistanceSpeedMetersPerSecond);
                    EditorGUILayout.FloatField("Shift 加速倍率", behaviour.BoostMultiplier);
                }
            }

            EndSection();
        }

        private void DrawKeyboardHelp()
        {
            var expanded = BeginSection("keyboard-help", "キーボード操作", false);
            if (expanded)
            {
                EditorGUILayout.HelpBox("WASD/QE で移動、矢印キーで向き変更、PageUp/PageDown でズーム、Home/End でフォーカス、R で初期位置に戻します。", MessageType.None);
            }

            EndSection();
        }

        private void DrawTools()
        {
            var expanded = BeginSection("tools", "ツール", false);
            if (expanded && GUILayout.Button("Open Debug Controller"))
            {
                var window = Windows.FreeDDebugControllerWindow.OpenWindow();
                window.SetTargetForDebug(((FreeDControllerBehaviour)target).gameObject);
            }

            EndSection();
        }

        private bool BeginSection(string key, string label, bool defaultExpanded)
        {
            var stateKey = $"{nameof(FreeDControllerBehaviourEditor)}.{target.GetInstanceID()}.{key}";
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
