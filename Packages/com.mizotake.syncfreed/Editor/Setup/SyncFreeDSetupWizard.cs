using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MizoTake.SyncFreeD.Editor.Setup
{
    public sealed class SyncFreeDSetupWizard : EditorWindow
    {
        private enum SetupPreset
        {
            BasicVirtualCamera,
            PTZDualDrive,
            ExternalTracker,
            Replay,
            OutputInspector
        }

        private bool createDirectionalLight = true;
        private bool selectCreatedCamera = true;
        private bool createProfileAssets;
        private SetupPreset setupPreset;

        [MenuItem("Tools/SyncFreeD/Setup Wizard")]
        public static void OpenWindow()
        {
            GetWindow<SyncFreeDSetupWizard>("SyncFreeD Setup");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("SyncFreeD Setup", EditorStyles.boldLabel);
            setupPreset = (SetupPreset)EditorGUILayout.EnumPopup("Preset", setupPreset);
            createDirectionalLight = EditorGUILayout.Toggle("Create Directional Light", createDirectionalLight);
            selectCreatedCamera = EditorGUILayout.Toggle("Select Created Camera", selectCreatedCamera);
            createProfileAssets = EditorGUILayout.Toggle("Create Profile Assets", createProfileAssets);
            if (GUILayout.Button("Create SyncFreeD Camera Rig"))
            {
                CreateRig();
            }
        }

        private void CreateRig()
        {
            var cameraObject = new GameObject(GetCameraRigName());
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            AddSourceBehaviour(cameraObject);
            cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            if (setupPreset == SetupPreset.OutputInspector)
            {
                cameraObject.AddComponent<DebugLogOutputBehaviour>();
                cameraObject.AddComponent<RecordingOutputBehaviour>();
            }

            cameraObject.AddComponent<SyncFreeDBehaviour>();
            cameraObject.AddComponent<SyncFreeDPacketPreviewBehaviour>();
            cameraObject.AddComponent<SyncDiagnosticsBehaviour>();
            cameraObject.transform.position = new Vector3(0f, 1f, -10f);
            CreatePresetObjects(cameraObject);
            if (createDirectionalLight && GameObject.Find("Directional Light") == null)
            {
                var lightObject = new GameObject("Directional Light");
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            if (createProfileAssets)
            {
                EnsureProfileAssets();
            }

            Undo.RegisterCreatedObjectUndo(cameraObject, "Create SyncFreeD Camera Rig");
            EditorSceneManager.MarkSceneDirty(cameraObject.scene);
            if (selectCreatedCamera)
            {
                Selection.activeGameObject = cameraObject;
            }
        }

        private string GetCameraRigName()
        {
            switch (setupPreset)
            {
                case SetupPreset.PTZDualDrive:
                    return "PTZ DualDrive Camera";
                case SetupPreset.ExternalTracker:
                    return "External Tracker Camera";
                case SetupPreset.Replay:
                    return "Replay Camera";
                case SetupPreset.OutputInspector:
                    return "Output Inspector Camera";
                case SetupPreset.BasicVirtualCamera:
                default:
                    return "SyncFreeD Camera Rig";
            }
        }

        private void AddSourceBehaviour(GameObject cameraObject)
        {
            switch (setupPreset)
            {
                case SetupPreset.PTZDualDrive:
                    cameraObject.AddComponent<ViscaCameraSourceBehaviour>();
                    cameraObject.AddComponent<ViscaTelemetryProviderBehaviour>();
                    break;
                case SetupPreset.ExternalTracker:
                    cameraObject.AddComponent<TrackerCameraSourceBehaviour>();
                    break;
                case SetupPreset.Replay:
                    cameraObject.AddComponent<ReplayCameraSourceBehaviour>();
                    break;
                case SetupPreset.OutputInspector:
                case SetupPreset.BasicVirtualCamera:
                default:
                    cameraObject.AddComponent<UnityCameraSourceBehaviour>();
                    break;
            }
        }

        private void CreatePresetObjects(GameObject cameraObject)
        {
            if (setupPreset != SetupPreset.PTZDualDrive)
            {
                return;
            }

            if (cameraObject.transform.Find("VISCA Command Target") == null)
            {
                var commandTarget = new GameObject("VISCA Command Target");
                commandTarget.transform.SetParent(cameraObject.transform, false);
                commandTarget.transform.localRotation = Quaternion.Euler(0f, 20f, 0f);
            }

            if (cameraObject.transform.Find("VISCA Observed Target") == null)
            {
                var observedTarget = new GameObject("VISCA Observed Target");
                observedTarget.transform.SetParent(cameraObject.transform, false);
                observedTarget.transform.localPosition = new Vector3(1f, 0f, 0f);
                observedTarget.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
            }
        }

        private static void EnsureProfileAssets()
        {
            const string directoryPath = "Assets/SyncFreeDProfiles";
            if (!AssetDatabase.IsValidFolder(directoryPath))
            {
                AssetDatabase.CreateFolder("Assets", "SyncFreeDProfiles");
            }

            CreateAssetIfMissing<ScriptableObjects.SyncTuningProfileAsset>($"{directoryPath}/DefaultSyncTuningProfile.asset");
            CreateAssetIfMissing<ScriptableObjects.DeviceProfileAsset>($"{directoryPath}/DefaultDeviceProfile.asset");
            CreateAssetIfMissing<ScriptableObjects.FirmwareBehaviorProfileAsset>($"{directoryPath}/DefaultFirmwareBehaviorProfile.asset");
            CreateAssetIfMissing<ScriptableObjects.LensProfileAsset>($"{directoryPath}/DefaultLensProfile.asset");
            CreateAssetIfMissing<ScriptableObjects.MountProfileAsset>($"{directoryPath}/DefaultMountProfile.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateAssetIfMissing<T>(string assetPath) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(assetPath) != null)
            {
                return;
            }

            var asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }
    }
}
