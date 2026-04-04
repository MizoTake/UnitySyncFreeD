using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            var output = behaviour.GetComponent<FreeDUdpOutputBehaviour>();
            var controller = behaviour.GetComponent<FreeDControllerBehaviour>();
            var loopback = behaviour.GetComponent<FreeDLoopbackReceiverBehaviour>();
            snapshot.HasSource = behaviour.SourceProvider != null;
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

            if (behaviour.GetComponent<FreeDControllerBehaviour>() != null)
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

            if (behaviour.GetComponent<FreeDLoopbackReceiverBehaviour>() != null)
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

        public static bool TryOpenSampleScene(SyncFreeDSampleId sampleId)
        {
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

            var controller = behaviour.GetComponent<FreeDControllerBehaviour>();
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
    }
}
