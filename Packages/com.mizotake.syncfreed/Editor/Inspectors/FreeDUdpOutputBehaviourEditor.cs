using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using UnityEditor;
using UnityEngine;
using Support = MizoTake.SyncFreeD.Editor.Support;
using Networking = MizoTake.SyncFreeD.Networking;

namespace MizoTake.SyncFreeD.Editor.Inspectors
{
    [CustomEditor(typeof(FreeDUdpOutputBehaviour))]
    public sealed class FreeDUdpOutputBehaviourEditor : UnityEditor.Editor
    {
        private SerializedProperty outputProfileAssetProperty;
        private SerializedProperty applyProfileOnEnableProperty;

        private void OnEnable()
        {
            outputProfileAssetProperty = serializedObject.FindProperty("outputProfileAsset");
            applyProfileOnEnableProperty = serializedObject.FindProperty("applyProfileOnEnable");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("送信 preset", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(outputProfileAssetProperty, new GUIContent("送信設定 Asset"));
            EditorGUILayout.PropertyField(applyProfileOnEnableProperty, new GUIContent("開始時に preset を反映"));
            EditorGUILayout.HelpBox(outputProfileAssetProperty.objectReferenceValue != null ? "送信先や NIC 設定は送信設定 Asset 側で編集します。" : "送信設定 Asset が未設定です。component の現在値で動作しますが、運用では Asset 参照を推奨します。", outputProfileAssetProperty.objectReferenceValue != null ? MessageType.Info : MessageType.Warning);
            serializedObject.ApplyModifiedProperties();

            using (new EditorGUI.DisabledScope(outputProfileAssetProperty.objectReferenceValue == null))
            {
                if (GUILayout.Button("Apply Preset Now"))
                {
                    ((FreeDUdpOutputBehaviour)target).ApplyProfile();
                    EditorUtility.SetDirty(target);
                }
            }

            var behaviour = (FreeDUdpOutputBehaviour)target;
            if (behaviour.HasConfigurationWarning)
            {
                EditorGUILayout.HelpBox(behaviour.ConfigurationWarning, MessageType.Warning);
            }

            DrawCurrentSettings(behaviour);
            if (behaviour.SendMode == Networking.PacketSendMode.Multicast)
            {
                DrawMulticastSupportSummary(behaviour);
            }

            if (EditorApplication.isPlaying)
            {
                DrawRuntime(behaviour);
            }
        }

        private void DrawCurrentSettings(FreeDUdpOutputBehaviour behaviour)
        {
            var expanded = BeginSection("current-settings", "現在の適用値", true);
            if (expanded)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.EnumPopup("送信のしかた", behaviour.SendMode);
                    EditorGUILayout.TextField("送信元 IP", FormatOptionalValue(behaviour.BindAddress, "自動"));
                    EditorGUILayout.IntField("送信バッファサイズ", behaviour.SocketBufferSize);
                    EditorGUILayout.TextField("送る Camera ID", behaviour.CameraIdFilter >= 0 ? behaviour.CameraIdFilter.ToString() : "すべて");
                    switch (behaviour.SendMode)
                    {
                        case Networking.PacketSendMode.SingleDestinationUnicast:
                            DrawPrimaryDestination(behaviour);
                            break;
                        case Networking.PacketSendMode.MultiDestinationUnicast:
                            DrawPrimaryDestination(behaviour);
                            DrawAdditionalDestinations(behaviour);
                            break;
                        case Networking.PacketSendMode.Multicast:
                            EditorGUILayout.TextField("Multicast Group", behaviour.MulticastGroupIpAddress);
                            EditorGUILayout.IntField("Multicast Port", behaviour.MulticastPort);
                            EditorGUILayout.IntField("Multicast TTL", behaviour.MulticastTtl);
                            EditorGUILayout.Toggle("Join Multicast Group", behaviour.JoinMulticastGroup);
                            EditorGUILayout.TextField("Multicast Interface", FormatOptionalValue(behaviour.MulticastInterfaceAddress, "自動"));
                            break;
                    }
                }
            }

            EndSection();
        }

        private void DrawMulticastSupportSummary(FreeDUdpOutputBehaviour behaviour)
        {
            var expanded = BeginSection("multicast-support", "Multicast 診断", behaviour.HasConfigurationWarning);
            if (!expanded)
            {
                EndSection();
                return;
            }

            var snapshot = Support.FreeDUdpMulticastSupportSummary.Build(behaviour);
            for (var i = 0; i < snapshot.Lines.Length; i++)
            {
                EditorGUILayout.LabelField(snapshot.Lines[i], EditorStyles.wordWrappedMiniLabel);
            }

            if (snapshot.HasActionNeeded)
            {
                EditorGUILayout.HelpBox("複数 NIC / join / bind の組み合わせを確認してください。", MessageType.Warning);
            }

            EndSection();
        }

        private void DrawRuntime(FreeDUdpOutputBehaviour behaviour)
        {
            var expanded = BeginSection("runtime", "Runtime", true);
            if (expanded)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField("Packet Length", behaviour.PacketLength);
                    EditorGUILayout.IntField("Configured Destinations", behaviour.GetConfiguredDestinationCount());
                    EditorGUILayout.IntField("Last Requested Destinations", behaviour.LastRequestedDestinationCount);
                    EditorGUILayout.EnumPopup("Effective Send Mode", behaviour.LastEffectiveSendMode);
                    EditorGUILayout.IntField("直近成功送信数", behaviour.LastSendSuccessCount);
                    EditorGUILayout.IntField("累計送信失敗数", behaviour.TotalSendFailureCount);
                    EditorGUILayout.Toggle("Camera ID Filter で skip", behaviour.LastSendSkippedByFilter);
                    if (behaviour.SendMode == Networking.PacketSendMode.Multicast)
                    {
                        EditorGUILayout.Toggle("Multicast Configured", behaviour.IsMulticastConfigured);
                    }

                    if (string.IsNullOrEmpty(behaviour.LastPacketHex))
                    {
                        EditorGUILayout.TextField("直近 packet", "まだ送信していません");
                    }
                    else
                    {
                        EditorGUILayout.TextField("Checksum", behaviour.LastChecksum.ToString("X2"));
                        EditorGUILayout.TextField("User Area", behaviour.LastUserArea.ToString("X4"));
                        EditorGUILayout.TextField("Packet Hex", behaviour.LastPacketHex);
                    }
                }
            }

            EndSection();
        }

        private static void DrawPrimaryDestination(FreeDUdpOutputBehaviour behaviour)
        {
            EditorGUILayout.TextField("送り先 IP", behaviour.DestinationIpAddress);
            EditorGUILayout.IntField("送り先 Port", behaviour.DestinationPort);
        }

        private static void DrawAdditionalDestinations(FreeDUdpOutputBehaviour behaviour)
        {
            var destinations = behaviour.AdditionalDestinations;
            if (destinations.Length == 0)
            {
                EditorGUILayout.LabelField("追加送信先", "なし");
                return;
            }

            for (var i = 0; i < destinations.Length; i++)
            {
                var destination = destinations[i];
                EditorGUILayout.TextField($"追加送信先 {i + 1}", $"{destination.IpAddress}:{destination.Port} {(destination.Enabled ? "Enabled" : "Disabled")}");
            }
        }

        private bool BeginSection(string key, string label, bool defaultExpanded)
        {
            var stateKey = $"{nameof(FreeDUdpOutputBehaviourEditor)}.{target.GetInstanceID()}.{key}";
            var expanded = SessionState.GetBool(stateKey, defaultExpanded);
            expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, label);
            SessionState.SetBool(stateKey, expanded);
            return expanded;
        }

        private static void EndSection()
        {
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static string FormatOptionalValue(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
