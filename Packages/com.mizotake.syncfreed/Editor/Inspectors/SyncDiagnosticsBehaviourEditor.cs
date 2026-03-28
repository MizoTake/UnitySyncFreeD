using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using UnityEditor;

namespace MizoTake.SyncFreeD.Editor.Inspectors
{
    [CustomEditor(typeof(SyncDiagnosticsBehaviour))]
    public sealed class SyncDiagnosticsBehaviourEditor : UnityEditor.Editor
    {
        private SerializedProperty syncBehaviourProperty;
        private SerializedProperty logLevelProperty;
        private SerializedProperty showOnGuiProperty;
        private SerializedProperty rectProperty;

        private void OnEnable()
        {
            syncBehaviourProperty = serializedObject.FindProperty("syncBehaviour");
            logLevelProperty = serializedObject.FindProperty("logLevel");
            showOnGuiProperty = serializedObject.FindProperty("showOnGui");
            rectProperty = serializedObject.FindProperty("rect");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(syncBehaviourProperty);
            EditorGUILayout.PropertyField(logLevelProperty);
            EditorGUILayout.PropertyField(showOnGuiProperty);
            EditorGUILayout.PropertyField(rectProperty);
            serializedObject.ApplyModifiedProperties();

            var behaviour = (SyncDiagnosticsBehaviour)target;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
                EditorGUILayout.TextField("Summary", behaviour.LastSummary);
                var syncBehaviour = behaviour.GetComponent<SyncFreeDBehaviour>();
                if (syncBehaviour != null)
                {
                    EditorGUILayout.DoubleField("Zoom Error (mm)", syncBehaviour.LastDiagnostics.ZoomErrorMm);
                    EditorGUILayout.IntField("Tracking Delay (ms)", syncBehaviour.LastDiagnostics.TrackingDelayMs);
                    EditorGUILayout.IntField("Video Delay (ms)", syncBehaviour.LastDiagnostics.VideoAlignmentDelayMs);
                    EditorGUILayout.Toggle("Lens Valid", syncBehaviour.LastDiagnostics.IsLensValid);
                    EditorGUILayout.Toggle("Fallback Mode", syncBehaviour.LastDiagnostics.IsFallbackMode);
                }
            }
        }
    }
}
