using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Networking;
using MizoTake.SyncFreeD.ScriptableObjects;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MizoTake.SyncFreeD.Editor.Support
{
    public enum SyncFreeDStatusTone
    {
        Info,
        Ready,
        ActionNeeded,
        Warning
    }

    public enum SyncFreeDSampleId
    {
        BasicVirtualCamera,
        FreeDController,
        OutputInspector,
        ExternalTracker,
        Replay
    }

    public sealed class SyncFreeDSupportSnapshot
    {
        public bool HasSource;
        public bool HasOutput;
        public bool HasOutputPreset;
        public bool HasControllerPreset;
        public bool HasLoopbackReceiver;
        public bool IsLoopbackReceiving;
        public bool HasConfigurationWarning;
        public SyncFreeDStatusTone Tone = SyncFreeDStatusTone.Info;
        public string StatusTitle = string.Empty;
        public string GuidanceMessage = string.Empty;
        public string ConfigurationWarning = string.Empty;
    }

    public readonly struct FreeDUdpMulticastSupportSnapshot
    {
        public FreeDUdpMulticastSupportSnapshot(string[] lines, bool hasActionNeeded)
        {
            Lines = lines ?? Array.Empty<string>();
            HasActionNeeded = hasActionNeeded;
        }

        public string[] Lines { get; }
        public bool HasActionNeeded { get; }
    }

    public static class FreeDUdpMulticastSupportSummary
    {
        public static FreeDUdpMulticastSupportSnapshot Build(FreeDUdpOutputBehaviour output)
        {
            if (output == null)
            {
                return new FreeDUdpMulticastSupportSnapshot(new[] { "Output not found" }, true);
            }

            return Build(output.SendMode, output.BindAddress, output.MulticastInterfaceAddress, output.JoinMulticastGroup, FreeDUdpNetworkInterfaceUtility.GetIPv4Interfaces());
        }

        public static FreeDUdpMulticastSupportSnapshot Build(PacketSendMode sendMode, string bindAddress, string multicastInterfaceAddress, bool joinMulticastGroup, IReadOnlyList<FreeDUdpNetworkInterfaceInfo> interfaces)
        {
            if (sendMode != PacketSendMode.Multicast)
            {
                return new FreeDUdpMulticastSupportSnapshot(new[] { "PacketSendMode is not Multicast." }, false);
            }

            var lines = new List<string>();
            var hasActionNeeded = false;
            var interfaceList = interfaces ?? Array.Empty<FreeDUdpNetworkInterfaceInfo>();
            lines.Add(joinMulticastGroup ? "Join Multicast Group: enabled" : "Join Multicast Group: disabled");
            if (!joinMulticastGroup)
            {
                lines.Add("Receiver 側で join が必要な環境では手動設定が必要です。");
                hasActionNeeded = true;
            }

            if (interfaceList.Count == 0)
            {
                lines.Add("Local IPv4 NIC: none detected");
                hasActionNeeded = true;
            }
            else
            {
                lines.Add($"Local IPv4 NICs: {string.Join(", ", interfaceList.Select(info => info.ToString()).ToArray())}");
                var selectedNic = FindInterface(interfaceList, multicastInterfaceAddress);
                if (string.IsNullOrWhiteSpace(multicastInterfaceAddress))
                {
                    if (interfaceList.Count == 1)
                    {
                        lines.Add($"Multicast Interface: auto ({interfaceList[0]})");
                    }
                    else
                    {
                        lines.Add("Multicast Interface: auto selection with multiple NICs");
                        hasActionNeeded = true;
                    }
                }
                else if (selectedNic.HasValue)
                {
                    lines.Add($"Multicast Interface: {selectedNic.Value}");
                }
                else
                {
                    lines.Add($"Multicast Interface: {multicastInterfaceAddress} (not found locally)");
                    hasActionNeeded = true;
                }
            }

            if (string.IsNullOrWhiteSpace(bindAddress))
            {
                if (interfaceList.Count == 1)
                {
                    lines.Add($"Bind Address: auto ({interfaceList[0]})");
                }
                else if (interfaceList.Count > 1)
                {
                    lines.Add("Bind Address: auto selection with multiple NICs");
                    hasActionNeeded = true;
                }
                else
                {
                    lines.Add("Bind Address: auto");
                }
            }
            else
            {
                var selectedBind = FindInterface(interfaceList, bindAddress);
                if (selectedBind.HasValue)
                {
                    lines.Add($"Bind Address: {bindAddress}");
                }
                else
                {
                    lines.Add($"Bind Address: {bindAddress} (not found locally)");
                    hasActionNeeded = true;
                }

                if (!string.IsNullOrWhiteSpace(multicastInterfaceAddress) && !string.Equals(bindAddress, multicastInterfaceAddress, StringComparison.OrdinalIgnoreCase))
                {
                    lines.Add("Bind Address と Multicast Interface Address が一致していません。");
                    hasActionNeeded = true;
                }
            }

            return new FreeDUdpMulticastSupportSnapshot(lines.ToArray(), hasActionNeeded);
        }

        private static FreeDUdpNetworkInterfaceInfo? FindInterface(IReadOnlyList<FreeDUdpNetworkInterfaceInfo> interfaces, string address)
        {
            if (interfaces == null || string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            for (var i = 0; i < interfaces.Count; i++)
            {
                if (string.Equals(interfaces[i].Address, address, StringComparison.OrdinalIgnoreCase))
                {
                    return interfaces[i];
                }
            }

            return null;
        }
    }

    public static class SyncFreeDSupportSummary
    {
        public const string SharedOutputPresetPath = "Assets/Samples/SyncFreeD/SharedPresets/LocalLoopbackOutput.asset";
        public const string SharedControllerPresetPath = "Assets/Samples/SyncFreeD/SharedPresets/ComfortController.asset";

        public static Color GetBackgroundColor(SyncFreeDStatusTone tone)
        {
            var isProSkin = EditorGUIUtility.isProSkin;
            switch (tone)
            {
                case SyncFreeDStatusTone.Ready:
                    return isProSkin ? new Color(0.12f, 0.32f, 0.19f, 0.92f) : new Color(0.84f, 0.95f, 0.87f, 1f);
                case SyncFreeDStatusTone.ActionNeeded:
                    return isProSkin ? new Color(0.42f, 0.24f, 0.08f, 0.92f) : new Color(1f, 0.92f, 0.8f, 1f);
                case SyncFreeDStatusTone.Warning:
                    return isProSkin ? new Color(0.38f, 0.12f, 0.12f, 0.94f) : new Color(0.98f, 0.84f, 0.84f, 1f);
                case SyncFreeDStatusTone.Info:
                default:
                    return isProSkin ? new Color(0.12f, 0.22f, 0.36f, 0.92f) : new Color(0.84f, 0.91f, 0.98f, 1f);
            }
        }

        public static Color GetAccentColor(SyncFreeDStatusTone tone)
        {
            switch (tone)
            {
                case SyncFreeDStatusTone.Ready:
                    return new Color(0.25f, 0.74f, 0.39f, 1f);
                case SyncFreeDStatusTone.ActionNeeded:
                    return new Color(0.93f, 0.63f, 0.2f, 1f);
                case SyncFreeDStatusTone.Warning:
                    return new Color(0.91f, 0.29f, 0.24f, 1f);
                case SyncFreeDStatusTone.Info:
                default:
                    return new Color(0.25f, 0.56f, 0.89f, 1f);
            }
        }

        public static SyncFreeDSupportSnapshot Build(SyncFreeDBehaviour behaviour)
        {
            var snapshot = new SyncFreeDSupportSnapshot();
            if (behaviour == null)
            {
                snapshot.StatusTitle = "まず対象を選択";
                snapshot.Tone = SyncFreeDStatusTone.Info;
                snapshot.GuidanceMessage = "SyncFreeDBehaviour を選択すると、設定状況と次の作業を表示します。";
                return snapshot;
            }

            var sourceProvider = FindSourceProvider(behaviour);
            var output = FindOutputBehaviour(behaviour);
            var controller = FindControllerBehaviour(behaviour);
            var loopback = FindLoopbackReceiverBehaviour(behaviour);
            snapshot.HasSource = sourceProvider != null;
            snapshot.HasOutput = output != null;
            snapshot.HasOutputPreset = output != null && output.OutputProfileAsset != null;
            snapshot.HasControllerPreset = controller == null || controller.ProfileAsset != null;
            snapshot.HasLoopbackReceiver = loopback != null;
            snapshot.IsLoopbackReceiving = loopback != null && loopback.ReceivedCount > 0;
            var outputWarning = output != null ? output.ConfigurationWarning : string.Empty;
            var firmwareWarning = behaviour.HasFirmwareBehaviorWarning ? behaviour.FirmwareBehaviorWarning : string.Empty;
            snapshot.ConfigurationWarning = BuildCombinedWarning(outputWarning, firmwareWarning);
            snapshot.HasConfigurationWarning = !string.IsNullOrEmpty(snapshot.ConfigurationWarning);
            snapshot.StatusTitle = BuildStatusTitle(snapshot, controller != null);
            snapshot.Tone = BuildTone(snapshot, controller != null);
            snapshot.GuidanceMessage = BuildGuidanceMessage(snapshot, controller != null);
            return snapshot;
        }

        public static string GetSampleScenePath(SyncFreeDSampleId sampleId)
        {
            switch (sampleId)
            {
                case SyncFreeDSampleId.FreeDController:
                    return "Assets/Samples/SyncFreeD/FreeDControllerSample/Scenes/FreeDControllerSample.unity";
                case SyncFreeDSampleId.OutputInspector:
                    return "Assets/Samples/SyncFreeD/OutputInspectorSample/Scenes/OutputInspectorSample.unity";
                case SyncFreeDSampleId.ExternalTracker:
                    return "Assets/Samples/SyncFreeD/ExternalTrackerSample/Scenes/ExternalTrackerSample.unity";
                case SyncFreeDSampleId.Replay:
                    return "Assets/Samples/SyncFreeD/ReplaySample/Scenes/ReplaySample.unity";
                case SyncFreeDSampleId.BasicVirtualCamera:
                default:
                    return "Assets/Samples/SyncFreeD/BasicVirtualCamera/Scenes/BasicVirtualCamera.unity";
            }
        }

        public static string GetSampleDescription(SyncFreeDSampleId sampleId)
        {
            switch (sampleId)
            {
                case SyncFreeDSampleId.FreeDController:
                    return "キーボードで動かしながら FreeD を確認します。";
                case SyncFreeDSampleId.OutputInspector:
                    return "送信できているか、受信できているかを確認します。";
                case SyncFreeDSampleId.ExternalTracker:
                    return "外部 tracker 起点の姿勢入力を確認します。";
                case SyncFreeDSampleId.Replay:
                    return "再生された動きが packet に変わる流れを確認します。";
                case SyncFreeDSampleId.BasicVirtualCamera:
                default:
                    return "Unity Camera をそのまま FreeD として送ります。";
            }
        }

        public static SyncFreeDSampleId GetRecommendedSampleId(SyncFreeDBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return SyncFreeDSampleId.BasicVirtualCamera;
            }

            if (FindControllerBehaviour(behaviour) != null)
            {
                return SyncFreeDSampleId.FreeDController;
            }

            if (behaviour.GetComponent<ReplayCameraSourceBehaviour>() != null)
            {
                return SyncFreeDSampleId.Replay;
            }

            if (behaviour.GetComponent<TrackerCameraSourceBehaviour>() != null)
            {
                return SyncFreeDSampleId.ExternalTracker;
            }

            if (FindLoopbackReceiverBehaviour(behaviour) != null)
            {
                return SyncFreeDSampleId.OutputInspector;
            }

            return SyncFreeDSampleId.BasicVirtualCamera;
        }

        public static string GetRecommendedSampleReason(SyncFreeDBehaviour behaviour)
        {
            var sampleId = GetRecommendedSampleId(behaviour);
            switch (sampleId)
            {
                case SyncFreeDSampleId.FreeDController:
                    return "controller が付いているので、操作確認の基準として FreeDControllerSample が最も近い構成です。";
                case SyncFreeDSampleId.Replay:
                    return "Replay source が付いているので、ReplaySample と見比べると差分を追いやすいです。";
                case SyncFreeDSampleId.ExternalTracker:
                    return "Tracker source が付いているので、ExternalTrackerSample が比較対象になります。";
                case SyncFreeDSampleId.OutputInspector:
                    return "loopback receiver があるので、OutputInspectorSample と同じ観点で確認できます。";
                case SyncFreeDSampleId.BasicVirtualCamera:
                default:
                    return "最小構成なので、BasicVirtualCamera から確認するのが最も分かりやすいです。";
            }
        }

        public static SyncFreeDBehaviour FindSyncBehaviour(GameObject context)
        {
            var direct = FindNearbyComponent<SyncFreeDBehaviour>(context);
            if (direct != null)
            {
                return direct;
            }

            var matched = FindMatchingSyncBehaviour(context);
            if (matched != null)
            {
                return matched;
            }

            var sceneComponent = FindNearestSceneComponent<SyncFreeDBehaviour>(context);
            return sceneComponent != null ? sceneComponent : UnityEngine.Object.FindFirstObjectByType<SyncFreeDBehaviour>();
        }

        public static FreeDControllerBehaviour FindControllerBehaviour(GameObject context)
        {
            return FindNearbyOrSceneComponent<FreeDControllerBehaviour>(context);
        }

        public static FreeDControllerBehaviour FindControllerBehaviour(SyncFreeDBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return null;
            }

            var direct = behaviour.GetComponent<FreeDControllerBehaviour>();
            if (direct != null)
            {
                return direct;
            }

            var matched = FindMatchingControllerInScene(behaviour);
            return matched != null ? matched : FindUniqueSceneComponent<FreeDControllerBehaviour>(behaviour.gameObject);
        }

        public static FreeDInputSourceBehaviour FindInputSourceBehaviour(GameObject context)
        {
            return FindNearbyOrSceneComponent<FreeDInputSourceBehaviour>(context);
        }

        public static FreeDLoopbackReceiverBehaviour FindLoopbackReceiverBehaviour(GameObject context)
        {
            return FindNearbyOrSceneComponent<FreeDLoopbackReceiverBehaviour>(context);
        }

        public static FreeDLoopbackReceiverBehaviour FindLoopbackReceiverBehaviour(SyncFreeDBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return null;
            }

            var direct = behaviour.GetComponent<FreeDLoopbackReceiverBehaviour>();
            return direct != null ? direct : FindUniqueSceneComponent<FreeDLoopbackReceiverBehaviour>(behaviour.gameObject);
        }

        public static DebugLogOutputBehaviour FindDebugLogOutputBehaviour(SyncFreeDBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return null;
            }

            if (behaviour.DebugLogOutputBehaviour != null)
            {
                return behaviour.DebugLogOutputBehaviour;
            }

            var direct = behaviour.GetComponent<DebugLogOutputBehaviour>();
            return direct != null ? direct : FindUniqueSceneComponent<DebugLogOutputBehaviour>(behaviour.gameObject);
        }

        public static RecordingOutputBehaviour FindRecordingOutputBehaviour(SyncFreeDBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return null;
            }

            if (behaviour.RecordingOutputBehaviour != null)
            {
                return behaviour.RecordingOutputBehaviour;
            }

            var direct = behaviour.GetComponent<RecordingOutputBehaviour>();
            return direct != null ? direct : FindUniqueSceneComponent<RecordingOutputBehaviour>(behaviour.gameObject);
        }

        public static FreeDDrivenCameraBehaviour FindDrivenCameraBehaviour(GameObject context)
        {
            return FindNearbyOrSceneComponent<FreeDDrivenCameraBehaviour>(context);
        }

        public static FreeDUdpOutputBehaviour FindOutputBehaviour(SyncFreeDBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return null;
            }

            if (behaviour.OutputBehaviour != null)
            {
                return behaviour.OutputBehaviour;
            }

            var direct = behaviour.GetComponent<FreeDUdpOutputBehaviour>();
            return direct != null ? direct : FindUniqueSceneComponent<FreeDUdpOutputBehaviour>(behaviour.gameObject);
        }

        public static bool CanOpenSampleScene()
        {
            return CanOpenSampleScene(EditorApplication.isPlayingOrWillChangePlaymode);
        }

        public static bool CanOpenSampleScene(bool isPlayingOrWillChangePlaymode)
        {
            return !isPlayingOrWillChangePlaymode;
        }

        public static string GetSampleSceneOpenBlockedReason()
        {
            return GetSampleSceneOpenBlockedReason(EditorApplication.isPlayingOrWillChangePlaymode);
        }

        public static string GetSampleSceneOpenBlockedReason(bool isPlayingOrWillChangePlaymode)
        {
            return CanOpenSampleScene(isPlayingOrWillChangePlaymode) ? string.Empty : "Play Mode 中は scene を切り替えできません。停止してから sample を開いてください。";
        }

        public static bool TryOpenSampleScene(SyncFreeDSampleId sampleId)
        {
            if (!CanOpenSampleScene())
            {
                Debug.LogWarning(GetSampleSceneOpenBlockedReason());
                return false;
            }

            var scenePath = GetSampleScenePath(sampleId);
            if (!File.Exists(scenePath))
            {
                return false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            EditorSceneManager.OpenScene(scenePath);
            return true;
        }

        public static FreeDUdpOutputProfileAsset LoadSharedOutputPreset()
        {
            return AssetDatabase.LoadAssetAtPath<FreeDUdpOutputProfileAsset>(SharedOutputPresetPath);
        }

        public static FreeDControllerProfileAsset LoadSharedControllerPreset()
        {
            return AssetDatabase.LoadAssetAtPath<FreeDControllerProfileAsset>(SharedControllerPresetPath);
        }

        public static bool ApplySharedOutputPreset(FreeDUdpOutputBehaviour outputBehaviour)
        {
            if (outputBehaviour == null)
            {
                return false;
            }

            var preset = LoadSharedOutputPreset();
            if (preset == null)
            {
                return false;
            }

            outputBehaviour.SetOutputProfileAsset(preset, true);
            EditorUtility.SetDirty(outputBehaviour);
            return true;
        }

        public static bool ApplySharedControllerPreset(FreeDControllerBehaviour controllerBehaviour)
        {
            if (controllerBehaviour == null)
            {
                return false;
            }

            var preset = LoadSharedControllerPreset();
            if (preset == null)
            {
                return false;
            }

            controllerBehaviour.SetProfileAsset(preset, true);
            EditorUtility.SetDirty(controllerBehaviour);
            return true;
        }

        public static FreeDControllerBehaviour EnsureController(SyncFreeDBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return null;
            }

            var controller = FindControllerBehaviour(behaviour);
            if (controller != null)
            {
                return controller;
            }

            controller = Undo.AddComponent<FreeDControllerBehaviour>(behaviour.gameObject);
            ApplySharedControllerPreset(controller);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(behaviour.gameObject);
            return controller;
        }

        public static string EnsureProjectPresetFolder()
        {
            const string directoryPath = "Assets/SyncFreeDProfiles";
            if (!AssetDatabase.IsValidFolder(directoryPath))
            {
                AssetDatabase.CreateFolder("Assets", "SyncFreeDProfiles");
            }

            return directoryPath;
        }

        public static FreeDUdpOutputProfileAsset CreateOutputPresetFromBehaviour(FreeDUdpOutputBehaviour outputBehaviour, string assetName)
        {
            if (outputBehaviour == null)
            {
                return null;
            }

            var folder = EnsureProjectPresetFolder();
            var safeAssetName = string.IsNullOrWhiteSpace(assetName) ? "FreeDUdpOutputProfile" : assetName.Trim();
            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{safeAssetName}.asset");
            var asset = ScriptableObject.CreateInstance<FreeDUdpOutputProfileAsset>();
            asset.Value.PacketSendMode = outputBehaviour.SendMode;
            asset.Value.DestinationIpAddress = outputBehaviour.DestinationIpAddress;
            asset.Value.DestinationPort = outputBehaviour.DestinationPort;
            asset.Value.AdditionalDestinations = outputBehaviour.AdditionalDestinations;
            asset.Value.MulticastGroupIpAddress = outputBehaviour.MulticastGroupIpAddress;
            asset.Value.MulticastPort = outputBehaviour.MulticastPort;
            asset.Value.BindAddress = outputBehaviour.BindAddress;
            asset.Value.SocketBufferSize = outputBehaviour.SocketBufferSize;
            asset.Value.CameraIdFilter = outputBehaviour.CameraIdFilter;
            asset.Value.JoinMulticastGroup = outputBehaviour.JoinMulticastGroup;
            asset.Value.MulticastInterfaceAddress = outputBehaviour.MulticastInterfaceAddress;
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            outputBehaviour.SetOutputProfileAsset(asset, true);
            EditorUtility.SetDirty(outputBehaviour);
            return asset;
        }

        public static FreeDControllerProfileAsset CreateControllerPresetFromBehaviour(FreeDControllerBehaviour controllerBehaviour, string assetName)
        {
            if (controllerBehaviour == null)
            {
                return null;
            }

            var folder = EnsureProjectPresetFolder();
            var safeAssetName = string.IsNullOrWhiteSpace(assetName) ? "FreeDControllerProfile" : assetName.Trim();
            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{safeAssetName}.asset");
            var asset = ScriptableObject.CreateInstance<FreeDControllerProfileAsset>();
            asset.Value.AllowKeyboardControl = controllerBehaviour.AllowKeyboardControl;
            asset.Value.UseUnscaledTime = controllerBehaviour.UseUnscaledTime;
            asset.Value.MoveSpeedMetersPerSecond = controllerBehaviour.MoveSpeedMetersPerSecond;
            asset.Value.RotateSpeedDegreesPerSecond = controllerBehaviour.RotateSpeedDegreesPerSecond;
            asset.Value.RollSpeedDegreesPerSecond = controllerBehaviour.RollSpeedDegreesPerSecond;
            asset.Value.FocalLengthSpeedMmPerSecond = controllerBehaviour.FocalLengthSpeedMmPerSecond;
            asset.Value.FocusDistanceSpeedMetersPerSecond = controllerBehaviour.FocusDistanceSpeedMetersPerSecond;
            asset.Value.BoostMultiplier = controllerBehaviour.BoostMultiplier;
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            controllerBehaviour.SetProfileAsset(asset, true);
            EditorUtility.SetDirty(controllerBehaviour);
            return asset;
        }

        private static string BuildGuidanceMessage(SyncFreeDSupportSnapshot snapshot, bool hasController)
        {
            if (!snapshot.HasSource)
            {
                return "入力元 Behaviour が未設定です。Setup Wizard か sample scene から始めてください。";
            }

            if (!snapshot.HasOutput)
            {
                return "FreeD 出力 Behaviour が未設定です。FreeDUdpOutputBehaviour を追加してください。";
            }

            if (!snapshot.HasOutputPreset)
            {
                return "送信設定 Asset を割り当てると、scene を複製せずに設定を再利用できます。";
            }

            if (hasController && !snapshot.HasControllerPreset)
            {
                return "controller 操作 preset を割り当てると、操作感を他の sample と揃えられます。";
            }

            if (snapshot.HasConfigurationWarning)
            {
                return "送信設定に warning があります。内容を確認してから Play Mode に入ってください。";
            }

            if (snapshot.HasLoopbackReceiver && !snapshot.IsLoopbackReceiving)
            {
                return "Play Mode に入って、loopback 受信が増えるか確認してください。";
            }

            return "設定は概ね整っています。Play Mode で packet preview と diagnostics を確認してください。";
        }

        private static string BuildStatusTitle(SyncFreeDSupportSnapshot snapshot, bool hasController)
        {
            if (!snapshot.HasSource || !snapshot.HasOutput)
            {
                return "設定不足";
            }

            if (snapshot.HasConfigurationWarning)
            {
                return "送信設定を確認";
            }

            if (!snapshot.HasOutputPreset || (hasController && !snapshot.HasControllerPreset))
            {
                return "preset を追加すると分かりやすい状態";
            }

            if (snapshot.HasLoopbackReceiver && !snapshot.IsLoopbackReceiving)
            {
                return "再生して受信確認";
            }

            return "確認しやすい状態";
        }

        private static SyncFreeDStatusTone BuildTone(SyncFreeDSupportSnapshot snapshot, bool hasController)
        {
            if (!snapshot.HasSource || !snapshot.HasOutput)
            {
                return SyncFreeDStatusTone.Warning;
            }

            if (snapshot.HasConfigurationWarning)
            {
                return SyncFreeDStatusTone.Warning;
            }

            if (!snapshot.HasOutputPreset || (hasController && !snapshot.HasControllerPreset))
            {
                return SyncFreeDStatusTone.ActionNeeded;
            }

            if (snapshot.HasLoopbackReceiver && !snapshot.IsLoopbackReceiving)
            {
                return SyncFreeDStatusTone.Info;
            }

            return SyncFreeDStatusTone.Ready;
        }

        private static string BuildCombinedWarning(string outputWarning, string firmwareWarning)
        {
            if (string.IsNullOrEmpty(outputWarning))
            {
                return firmwareWarning ?? string.Empty;
            }

            if (string.IsNullOrEmpty(firmwareWarning))
            {
                return outputWarning;
            }

            return $"{outputWarning}\n{firmwareWarning}";
        }

        private static ICameraFrameProvider FindSourceProvider(SyncFreeDBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return null;
            }

            if (behaviour.SourceProvider != null)
            {
                return behaviour.SourceProvider;
            }

            var components = behaviour.GetComponents<MonoBehaviour>();
            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] is ICameraFrameProvider provider)
                {
                    return provider;
                }
            }

            return null;
        }

        private static FreeDControllerBehaviour FindMatchingControllerInScene(SyncFreeDBehaviour behaviour)
        {
            if (behaviour == null || !behaviour.gameObject.scene.IsValid())
            {
                return null;
            }

            var camera = behaviour.GetComponent<Camera>();
            var controllers = EnumerateSceneComponents<FreeDControllerBehaviour>(behaviour.gameObject).ToArray();
            for (var i = 0; i < controllers.Length; i++)
            {
                var controller = controllers[i];
                if (controller == null)
                {
                    continue;
                }

                if (controller.gameObject == behaviour.gameObject || controller.ControlledTransform == behaviour.transform || (camera != null && controller.ControlledCamera == camera))
                {
                    return controller;
                }
            }

            return null;
        }

        private static T FindUniqueSceneComponent<T>(GameObject context) where T : Component
        {
            if (context == null)
            {
                return null;
            }

            var candidates = EnumerateSceneComponents<T>(context).Distinct().ToArray();
            return candidates.Length == 1 ? candidates[0] : null;
        }

        private static T FindNearestSceneComponent<T>(GameObject context) where T : Component
        {
            if (context == null || !context.scene.IsValid())
            {
                return null;
            }

            var candidates = EnumerateSceneComponents<T>(context).Distinct().ToArray();
            if (candidates.Length == 0)
            {
                return null;
            }

            var bestCandidate = default(T);
            var bestScore = int.MaxValue;
            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (candidate == null)
                {
                    continue;
                }

                var score = ComputeSceneDistanceScore(context.transform, candidate.transform);
                if (score >= bestScore)
                {
                    continue;
                }

                bestCandidate = candidate;
                bestScore = score;
            }

            return bestCandidate;
        }

        private static IEnumerable<T> EnumerateSceneComponents<T>(GameObject context) where T : Component
        {
            if (context == null || !context.scene.IsValid())
            {
                yield break;
            }

            var roots = context.scene.GetRootGameObjects();
            for (var rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                var components = roots[rootIndex].GetComponentsInChildren<T>(true);
                for (var componentIndex = 0; componentIndex < components.Length; componentIndex++)
                {
                    if (components[componentIndex] != null)
                    {
                        yield return components[componentIndex];
                    }
                }
            }
        }

        private static T FindNearbyOrSceneComponent<T>(GameObject context) where T : Component
        {
            var direct = FindNearbyComponent<T>(context);
            if (direct != null)
            {
                return direct;
            }

            if (context != null)
            {
                var sceneComponent = FindNearestSceneComponent<T>(context);
                if (sceneComponent != null)
                {
                    return sceneComponent;
                }
            }

            return UnityEngine.Object.FindFirstObjectByType<T>();
        }

        private static T FindNearbyComponent<T>(GameObject context) where T : Component
        {
            if (context == null)
            {
                return null;
            }

            var direct = context.GetComponent<T>();
            if (direct != null)
            {
                return direct;
            }

            var parent = context.GetComponentInParent<T>();
            if (parent != null)
            {
                return parent;
            }

            var children = context.GetComponentsInChildren<T>(true);
            for (var i = 0; i < children.Length; i++)
            {
                if (children[i] != null)
                {
                    return children[i];
                }
            }

            return null;
        }

        private static int ComputeSceneDistanceScore(Transform context, Transform candidate)
        {
            if (context == null || candidate == null)
            {
                return int.MaxValue;
            }

            var rootDistance = Mathf.Abs(context.root.GetSiblingIndex() - candidate.root.GetSiblingIndex());
            return (rootDistance * 1000) + ComputeHierarchyDistance(context, candidate);
        }

        private static int ComputeHierarchyDistance(Transform from, Transform to)
        {
            if (from == null || to == null)
            {
                return int.MaxValue;
            }

            if (from == to)
            {
                return 0;
            }

            var ancestorDepths = new Dictionary<Transform, int>();
            var current = from;
            var depth = 0;
            while (current != null)
            {
                ancestorDepths[current] = depth;
                current = current.parent;
                depth++;
            }

            current = to;
            depth = 0;
            while (current != null)
            {
                if (ancestorDepths.TryGetValue(current, out var fromDepth))
                {
                    return fromDepth + depth;
                }

                current = current.parent;
                depth++;
            }

            return int.MaxValue / 2;
        }

        private static SyncFreeDBehaviour FindMatchingSyncBehaviour(GameObject context)
        {
            if (context == null || !context.scene.IsValid())
            {
                return null;
            }

            var output = FindNearbyComponent<FreeDUdpOutputBehaviour>(context);
            if (output != null)
            {
                var matchedByOutput = FindNearestSyncMatch(context, sync => FindOutputBehaviour(sync) == output);
                if (matchedByOutput != null)
                {
                    return matchedByOutput;
                }
            }

            var controller = FindNearbyComponent<FreeDControllerBehaviour>(context);
            if (controller != null)
            {
                var matchedByController = FindNearestSyncMatch(context, sync => sync != null && (sync.gameObject == controller.gameObject || sync.transform == controller.ControlledTransform || (controller.ControlledCamera != null && sync.GetComponent<Camera>() == controller.ControlledCamera)));
                if (matchedByController != null)
                {
                    return matchedByController;
                }
            }

            var sourceProvider = FindNearbyCameraFrameProvider(context);
            if (sourceProvider != null)
            {
                var matchedBySource = FindNearestSyncMatch(context, sync => sync != null && sync.SourceProvider == sourceProvider);
                if (matchedBySource != null)
                {
                    return matchedBySource;
                }
            }

            var debugOutput = FindNearbyComponent<DebugLogOutputBehaviour>(context);
            if (debugOutput != null)
            {
                var matchedByDebugOutput = FindNearestSyncMatch(context, sync => FindDebugLogOutputBehaviour(sync) == debugOutput);
                if (matchedByDebugOutput != null)
                {
                    return matchedByDebugOutput;
                }
            }

            var recordingOutput = FindNearbyComponent<RecordingOutputBehaviour>(context);
            if (recordingOutput != null)
            {
                var matchedByRecordingOutput = FindNearestSyncMatch(context, sync => FindRecordingOutputBehaviour(sync) == recordingOutput);
                if (matchedByRecordingOutput != null)
                {
                    return matchedByRecordingOutput;
                }
            }

            return null;
        }

        private static SyncFreeDBehaviour FindNearestSyncMatch(GameObject context, Func<SyncFreeDBehaviour, bool> predicate)
        {
            if (context == null || predicate == null)
            {
                return null;
            }

            SyncFreeDBehaviour bestCandidate = null;
            var bestScore = int.MaxValue;
            var candidates = EnumerateSceneComponents<SyncFreeDBehaviour>(context).Distinct().ToArray();
            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (candidate == null || !predicate(candidate))
                {
                    continue;
                }

                var score = ComputeSceneDistanceScore(context.transform, candidate.transform);
                if (score >= bestScore)
                {
                    continue;
                }

                bestCandidate = candidate;
                bestScore = score;
            }

            return bestCandidate;
        }

        private static ICameraFrameProvider FindNearbyCameraFrameProvider(GameObject context)
        {
            if (context == null)
            {
                return null;
            }

            var direct = FindCameraFrameProvider(context.GetComponents<MonoBehaviour>());
            if (direct != null)
            {
                return direct;
            }

            var parents = FindCameraFrameProvider(context.GetComponentsInParent<MonoBehaviour>(true));
            if (parents != null)
            {
                return parents;
            }

            return FindCameraFrameProvider(context.GetComponentsInChildren<MonoBehaviour>(true));
        }

        private static ICameraFrameProvider FindCameraFrameProvider(MonoBehaviour[] behaviours)
        {
            if (behaviours == null)
            {
                return null;
            }

            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ICameraFrameProvider provider)
                {
                    return provider;
                }
            }

            return null;
        }
    }
}
