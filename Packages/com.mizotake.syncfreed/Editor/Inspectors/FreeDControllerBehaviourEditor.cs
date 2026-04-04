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
            EditorGUILayout.HelpBox("preset を使うと、サンプルごとに同じ操作感を再利用できます。", MessageType.Info);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("基本設定", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(allowKeyboardControlProperty, new GUIContent("キーボード操作を有効にする"));
            EditorGUILayout.PropertyField(useUnscaledTimeProperty, new GUIContent("一時停止に影響されない時間を使う"));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("動きの速さ", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(moveSpeedMetersPerSecondProperty, new GUIContent("移動の速さ"));
            EditorGUILayout.PropertyField(rotateSpeedDegreesPerSecondProperty, new GUIContent("向き変更の速さ"));
            EditorGUILayout.PropertyField(rollSpeedDegreesPerSecondProperty, new GUIContent("傾き変更の速さ"));
            EditorGUILayout.PropertyField(focalLengthSpeedMmPerSecondProperty, new GUIContent("ズーム変更の速さ"));
            EditorGUILayout.PropertyField(focusDistanceSpeedMetersPerSecondProperty, new GUIContent("フォーカス変更の速さ"));
            EditorGUILayout.PropertyField(boostMultiplierProperty, new GUIContent("Shift を押した時の加速倍率"));
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Apply Preset Now"))
            {
                ((FreeDControllerBehaviour)target).ApplyProfile();
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("Open Debug Controller"))
            {
                Windows.FreeDDebugControllerWindow.OpenWindow();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("WASD/QE で移動、矢印キーで向き変更、PageUp/PageDown でズーム、Home/End でフォーカス、R で初期位置に戻します。", MessageType.None);
        }
    }
}
