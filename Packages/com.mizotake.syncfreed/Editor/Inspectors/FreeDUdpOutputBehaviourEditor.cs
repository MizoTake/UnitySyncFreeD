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
        private SerializedProperty packetSendModeProperty;
        private SerializedProperty destinationIpAddressProperty;
        private SerializedProperty destinationPortProperty;
        private SerializedProperty additionalDestinationsProperty;
        private SerializedProperty multicastGroupIpAddressProperty;
        private SerializedProperty multicastPortProperty;
        private SerializedProperty multicastTtlProperty;
        private SerializedProperty bindAddressProperty;
        private SerializedProperty socketBufferSizeProperty;
        private SerializedProperty cameraIdFilterProperty;
        private SerializedProperty joinMulticastGroupProperty;
        private SerializedProperty multicastInterfaceAddressProperty;

        private void OnEnable()
        {
            outputProfileAssetProperty = serializedObject.FindProperty("outputProfileAsset");
            applyProfileOnEnableProperty = serializedObject.FindProperty("applyProfileOnEnable");
            packetSendModeProperty = serializedObject.FindProperty("packetSendMode");
            destinationIpAddressProperty = serializedObject.FindProperty("destinationIpAddress");
            destinationPortProperty = serializedObject.FindProperty("destinationPort");
            additionalDestinationsProperty = serializedObject.FindProperty("additionalDestinations");
            multicastGroupIpAddressProperty = serializedObject.FindProperty("multicastGroupIpAddress");
            multicastPortProperty = serializedObject.FindProperty("multicastPort");
            multicastTtlProperty = serializedObject.FindProperty("multicastTtl");
            bindAddressProperty = serializedObject.FindProperty("bindAddress");
            socketBufferSizeProperty = serializedObject.FindProperty("socketBufferSize");
            cameraIdFilterProperty = serializedObject.FindProperty("cameraIdFilter");
            joinMulticastGroupProperty = serializedObject.FindProperty("joinMulticastGroup");
            multicastInterfaceAddressProperty = serializedObject.FindProperty("multicastInterfaceAddress");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("送信 preset", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(outputProfileAssetProperty, new GUIContent("送信設定 Asset"));
            EditorGUILayout.PropertyField(applyProfileOnEnableProperty, new GUIContent("開始時に preset を反映"));
            var profileAssigned = outputProfileAssetProperty.objectReferenceValue != null;
            var profileControlsSettings = profileAssigned && applyProfileOnEnableProperty.boolValue;
            if (profileControlsSettings)
            {
                EditorGUILayout.HelpBox("開始時に preset を反映するため、送信設定は Asset 側で編集します。", MessageType.Info);
            }
            else if (profileAssigned)
            {
                EditorGUILayout.HelpBox("自動反映が無効なため、下の component 設定を直接編集できます。Apply Preset Now を押した時だけ Asset の値で上書きします。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("送信設定 Asset が未設定です。下の component 設定を直接編集できます。", MessageType.Warning);
            }

            DrawCurrentSettings(profileControlsSettings);
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

            if (behaviour.SendMode == Networking.PacketSendMode.Multicast)
            {
                DrawMulticastSupportSummary(behaviour);
            }

            if (EditorApplication.isPlaying)
            {
                DrawRuntime(behaviour);
            }
        }

        private void DrawCurrentSettings(bool readOnly)
        {
            var expanded = BeginSection("current-settings", readOnly ? "現在の適用値" : "Component 送信設定", true);
            if (expanded)
            {
                using (new EditorGUI.DisabledScope(readOnly))
                {
                    EditorGUILayout.PropertyField(packetSendModeProperty, new GUIContent("送信のしかた"));
                    EditorGUILayout.PropertyField(bindAddressProperty, new GUIContent("送信元 IP", "空欄の場合は OS が NIC を選択します。"));
                    EditorGUILayout.PropertyField(socketBufferSizeProperty, new GUIContent("送信バッファサイズ"));
                    EditorGUILayout.PropertyField(cameraIdFilterProperty, new GUIContent("送る Camera ID", "-1 の場合はすべての Camera ID を送信します。"));
                    switch ((Networking.PacketSendMode)packetSendModeProperty.enumValueIndex)
                    {
                        case Networking.PacketSendMode.SingleDestinationUnicast:
                            DrawPrimaryDestination();
                            break;
                        case Networking.PacketSendMode.MultiDestinationUnicast:
                            DrawPrimaryDestination();
                            EditorGUILayout.PropertyField(additionalDestinationsProperty, new GUIContent("追加送信先"), true);
                            break;
                        case Networking.PacketSendMode.Multicast:
                            EditorGUILayout.PropertyField(multicastGroupIpAddressProperty, new GUIContent("Multicast Group"));
                            EditorGUILayout.PropertyField(multicastPortProperty, new GUIContent("Multicast Port"));
                            EditorGUILayout.PropertyField(multicastTtlProperty, new GUIContent("Multicast TTL"));
                            EditorGUILayout.PropertyField(joinMulticastGroupProperty, new GUIContent("Join Multicast Group"));
                            EditorGUILayout.PropertyField(multicastInterfaceAddressProperty, new GUIContent("Multicast Interface", "空欄の場合は OS が NIC を選択します。"));
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

        private void DrawPrimaryDestination()
        {
            EditorGUILayout.PropertyField(destinationIpAddressProperty, new GUIContent("送り先 IP"));
            EditorGUILayout.PropertyField(destinationPortProperty, new GUIContent("送り先 Port"));
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

    }
}
