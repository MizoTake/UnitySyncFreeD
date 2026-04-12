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
            FreeDController,
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
            EditorGUILayout.HelpBox("sample と同じ構成を Scene に作るための画面です。まず preset を選び、必要なら preset Asset も一緒に作成します。", MessageType.Info);
            setupPreset = (SetupPreset)EditorGUILayout.EnumPopup("作りたい sample", setupPreset);
            EditorGUILayout.HelpBox(GetPresetDescription(setupPreset), MessageType.None);
            createDirectionalLight = EditorGUILayout.Toggle("Directional Light を作る", createDirectionalLight);
            selectCreatedCamera = EditorGUILayout.Toggle("作成後に rig を選択する", selectCreatedCamera);
            createProfileAssets = EditorGUILayout.Toggle("再利用用の preset Asset も作る", createProfileAssets);
            if (GUILayout.Button("Open Operator Window"))
            {
                Windows.SyncFreeDOperatorWindow.OpenWindow();
            }

            if (GUILayout.Button("Open Matching Sample Scene"))
            {
                Support.SyncFreeDSupportSummary.TryOpenSampleScene(ToSampleId(setupPreset));
            }

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
                cameraObject.AddComponent<FreeDLoopbackReceiverBehaviour>();
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
                var assets = EnsureProfileAssets();
                ApplyProfileAssets(cameraObject, assets);
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
                case SetupPreset.FreeDController:
                    return "FreeD Controller Camera";
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

        private static string GetPresetDescription(SetupPreset preset)
        {
            switch (preset)
            {
                case SetupPreset.FreeDController:
                    return "キーボードで camera を動かしながら FreeD を確認したい時に使います。";
                case SetupPreset.OutputInspector:
                    return "送信先、packet、loopback 受信を確認したい時に使います。";
                case SetupPreset.ExternalTracker:
                    return "外部 tracker から姿勢を入れる構成を試したい時に使います。";
                case SetupPreset.Replay:
                    return "再生データを FreeD に変換する流れを確認したい時に使います。";
                case SetupPreset.BasicVirtualCamera:
                default:
                    return "最小構成で Unity Camera を FreeD として送る時に使います。";
            }
        }

        private static Support.SyncFreeDSampleId ToSampleId(SetupPreset preset)
        {
            switch (preset)
            {
                case SetupPreset.FreeDController:
                    return Support.SyncFreeDSampleId.FreeDController;
                case SetupPreset.OutputInspector:
                    return Support.SyncFreeDSampleId.OutputInspector;
                case SetupPreset.ExternalTracker:
                    return Support.SyncFreeDSampleId.ExternalTracker;
                case SetupPreset.Replay:
                    return Support.SyncFreeDSampleId.Replay;
                case SetupPreset.BasicVirtualCamera:
                default:
                    return Support.SyncFreeDSampleId.BasicVirtualCamera;
            }
        }

        private void AddSourceBehaviour(GameObject cameraObject)
        {
            switch (setupPreset)
            {
                case SetupPreset.ExternalTracker:
                    cameraObject.AddComponent<TrackerCameraSourceBehaviour>();
                    break;
                case SetupPreset.Replay:
                    cameraObject.AddComponent<ReplayCameraSourceBehaviour>();
                    break;
                case SetupPreset.OutputInspector:
                case SetupPreset.BasicVirtualCamera:
                case SetupPreset.FreeDController:
                default:
                    cameraObject.AddComponent<UnityCameraSourceBehaviour>();
                    break;
            }

            if (setupPreset == SetupPreset.FreeDController)
            {
                cameraObject.AddComponent<FreeDControllerBehaviour>();
            }
        }

        private void CreatePresetObjects(GameObject cameraObject)
        {
        }

        private static CreatedAssets EnsureProfileAssets()
        {
            const string directoryPath = "Assets/SyncFreeDProfiles";
            if (!AssetDatabase.IsValidFolder(directoryPath))
            {
                AssetDatabase.CreateFolder("Assets", "SyncFreeDProfiles");
            }

            var assets = new CreatedAssets
            {
                SyncTuningProfile = CreateAssetIfMissing<ScriptableObjects.SyncTuningProfileAsset>($"{directoryPath}/DefaultSyncTuningProfile.asset"),
                DeviceProfile = CreateAssetIfMissing<ScriptableObjects.DeviceProfileAsset>($"{directoryPath}/DefaultDeviceProfile.asset"),
                FirmwareBehaviorProfile = CreateAssetIfMissing<ScriptableObjects.FirmwareBehaviorProfileAsset>($"{directoryPath}/DefaultFirmwareBehaviorProfile.asset"),
                LensProfile = CreateAssetIfMissing<ScriptableObjects.LensProfileAsset>($"{directoryPath}/DefaultLensProfile.asset"),
                MountProfile = CreateAssetIfMissing<ScriptableObjects.MountProfileAsset>($"{directoryPath}/DefaultMountProfile.asset"),
                BehaviourProfile = CreateAssetIfMissing<ScriptableObjects.SyncFreeDBehaviourProfileAsset>($"{directoryPath}/DefaultSyncFreeDBehaviourProfile.asset"),
                OutputProfile = CreateAssetIfMissing<ScriptableObjects.FreeDUdpOutputProfileAsset>($"{directoryPath}/DefaultFreeDUdpOutputProfile.asset"),
                ControllerProfile = CreateAssetIfMissing<ScriptableObjects.FreeDControllerProfileAsset>($"{directoryPath}/DefaultFreeDControllerProfile.asset")
            };
            assets.BehaviourProfile.Value.TuningProfileAsset = assets.SyncTuningProfile;
            assets.BehaviourProfile.Value.DeviceProfileAsset = assets.DeviceProfile;
            assets.BehaviourProfile.Value.FirmwareBehaviorProfileAsset = assets.FirmwareBehaviorProfile;
            assets.BehaviourProfile.Value.LensProfileAsset = assets.LensProfile;
            assets.BehaviourProfile.Value.MountProfileAsset = assets.MountProfile;
            EditorUtility.SetDirty(assets.BehaviourProfile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return assets;
        }

        private static T CreateAssetIfMissing<T>(string assetPath) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            var asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static void ApplyProfileAssets(GameObject cameraObject, CreatedAssets assets)
        {
            var syncBehaviour = cameraObject.GetComponent<SyncFreeDBehaviour>();
            if (syncBehaviour != null)
            {
                syncBehaviour.SetProfileAsset(assets.BehaviourProfile, true);
                EditorUtility.SetDirty(syncBehaviour);
            }

            var outputBehaviour = cameraObject.GetComponent<FreeDUdpOutputBehaviour>();
            if (outputBehaviour != null)
            {
                outputBehaviour.SetOutputProfileAsset(assets.OutputProfile, true);
                EditorUtility.SetDirty(outputBehaviour);
            }

            var controllerBehaviour = cameraObject.GetComponent<FreeDControllerBehaviour>();
            if (controllerBehaviour != null)
            {
                controllerBehaviour.SetProfileAsset(assets.ControllerProfile, true);
                EditorUtility.SetDirty(controllerBehaviour);
            }
        }

        private sealed class CreatedAssets
        {
            public ScriptableObjects.SyncTuningProfileAsset SyncTuningProfile;
            public ScriptableObjects.DeviceProfileAsset DeviceProfile;
            public ScriptableObjects.FirmwareBehaviorProfileAsset FirmwareBehaviorProfile;
            public ScriptableObjects.LensProfileAsset LensProfile;
            public ScriptableObjects.MountProfileAsset MountProfile;
            public ScriptableObjects.SyncFreeDBehaviourProfileAsset BehaviourProfile;
            public ScriptableObjects.FreeDUdpOutputProfileAsset OutputProfile;
            public ScriptableObjects.FreeDControllerProfileAsset ControllerProfile;
        }
    }
}
