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
        private SerializedProperty allowKeyboardControlProperty;
        private SerializedProperty useUnscaledTimeProperty;
        private SerializedProperty moveSpeedMetersPerSecondProperty;
        private SerializedProperty rotateSpeedDegreesPerSecondProperty;
        private SerializedProperty rollSpeedDegreesPerSecondProperty;
        private SerializedProperty focalLengthSpeedMmPerSecondProperty;
        private SerializedProperty focusDistanceSpeedMetersPerSecondProperty;
        private SerializedProperty boostMultiplierProperty;

        private void OnEnable()
        {
            controlledTransformProperty = serializedObject.FindProperty("controlledTransform");
            controlledCameraProperty = serializedObject.FindProperty("controlledCamera");
            profileAssetProperty = serializedObject.FindProperty("profileAsset");
            applyProfileOnAwakeProperty = serializedObject.FindProperty("applyProfileOnAwake");
            allowKeyboardControlProperty = serializedObject.FindProperty("allowKeyboardControl");
            useUnscaledTimeProperty = serializedObject.FindProperty("useUnscaledTime");
            moveSpeedMetersPerSecondProperty = serializedObject.FindProperty("moveSpeedMetersPerSecond");
            rotateSpeedDegreesPerSecondProperty = serializedObject.FindProperty("rotateSpeedDegreesPerSecond");
            rollSpeedDegreesPerSecondProperty = serializedObject.FindProperty("rollSpeedDegreesPerSecond");
            focalLengthSpeedMmPerSecondProperty = serializedObject.FindProperty("focalLengthSpeedMmPerSecond");
            focusDistanceSpeedMetersPerSecondProperty = serializedObject.FindProperty("focusDistanceSpeedMetersPerSecond");
            boostMultiplierProperty = serializedObject.FindProperty("boostMultiplier");
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
            var profileAssigned = profileAssetProperty.objectReferenceValue != null;
            var profileControlsSettings = profileAssigned && applyProfileOnAwakeProperty.boolValue;
            if (profileControlsSettings)
            {
                EditorGUILayout.HelpBox("開始時に preset を反映するため、操作設定は Asset 側で編集します。", MessageType.Info);
            }
            else if (profileAssigned)
            {
                EditorGUILayout.HelpBox("自動反映が無効なため、下の component 設定を直接編集できます。Apply Preset Now を押した時だけ Asset の値で上書きします。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("操作 preset Asset が未設定です。下の component 設定を直接編集できます。", MessageType.Warning);
            }

            DrawPresetSnapshot(profileControlsSettings);
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
            if (behaviour.AllowKeyboardControl)
            {
                DrawKeyboardHelp();
            }

            DrawTools();
        }

        private void DrawPresetSnapshot(bool readOnly)
        {
            var expanded = BeginSection("current-settings", readOnly ? "現在の適用値" : "Component 操作設定", true);
            if (expanded)
            {
                using (new EditorGUI.DisabledScope(readOnly))
                {
                    EditorGUILayout.PropertyField(allowKeyboardControlProperty, new GUIContent("キーボード操作"));
                    EditorGUILayout.PropertyField(useUnscaledTimeProperty, new GUIContent("Unscaled Time"));
                    EditorGUILayout.PropertyField(moveSpeedMetersPerSecondProperty, new GUIContent("移動の速さ"));
                    EditorGUILayout.PropertyField(rotateSpeedDegreesPerSecondProperty, new GUIContent("向き変更の速さ"));
                    EditorGUILayout.PropertyField(rollSpeedDegreesPerSecondProperty, new GUIContent("傾き変更の速さ"));
                    EditorGUILayout.PropertyField(focalLengthSpeedMmPerSecondProperty, new GUIContent("ズーム変更の速さ"));
                    EditorGUILayout.PropertyField(focusDistanceSpeedMetersPerSecondProperty, new GUIContent("フォーカス変更の速さ"));
                    EditorGUILayout.PropertyField(boostMultiplierProperty, new GUIContent("Shift 加速倍率"));
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
