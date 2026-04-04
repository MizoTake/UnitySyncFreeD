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
        private SerializedProperty packetSendModeProperty;
        private SerializedProperty outputProfileAssetProperty;
        private SerializedProperty applyProfileOnEnableProperty;
        private SerializedProperty destinationIpAddressProperty;
        private SerializedProperty destinationPortProperty;
        private SerializedProperty additionalDestinationsProperty;
        private SerializedProperty multicastGroupIpAddressProperty;
        private SerializedProperty multicastPortProperty;
        private SerializedProperty bindAddressProperty;
        private SerializedProperty socketBufferSizeProperty;
        private SerializedProperty cameraIdFilterProperty;
        private SerializedProperty joinMulticastGroupProperty;
        private SerializedProperty multicastInterfaceAddressProperty;

        private void OnEnable()
        {
            packetSendModeProperty = serializedObject.FindProperty("packetSendMode");
            outputProfileAssetProperty = serializedObject.FindProperty("outputProfileAsset");
            applyProfileOnEnableProperty = serializedObject.FindProperty("applyProfileOnEnable");
            destinationIpAddressProperty = serializedObject.FindProperty("destinationIpAddress");
            destinationPortProperty = serializedObject.FindProperty("destinationPort");
            additionalDestinationsProperty = serializedObject.FindProperty("additionalDestinations");
            multicastGroupIpAddressProperty = serializedObject.FindProperty("multicastGroupIpAddress");
            multicastPortProperty = serializedObject.FindProperty("multicastPort");
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
            EditorGUILayout.HelpBox("同じ送信先を複数の sample で使う場合は、scene ではなく preset Asset に保存します。", MessageType.Info);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("送信方法", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(packetSendModeProperty, new GUIContent("送信のしかた"));
            EditorGUILayout.PropertyField(bindAddressProperty, new GUIContent("送信元 IP"));
            EditorGUILayout.PropertyField(socketBufferSizeProperty, new GUIContent("送信バッファサイズ"));
            EditorGUILayout.PropertyField(cameraIdFilterProperty, new GUIContent("送る Camera ID"));
            DrawAvailableInterfaces("利用可能な IPv4 NIC", bindAddressProperty, multicastInterfaceAddressProperty, true, false);
            if (cameraIdFilterProperty.intValue < 0)
            {
                EditorGUILayout.HelpBox("送る Camera ID が -1 の場合は、すべての Camera ID を送信します。", MessageType.None);
            }
            var mode = (Networking.PacketSendMode)packetSendModeProperty.enumValueIndex;
            switch (mode)
            {
                case Networking.PacketSendMode.SingleDestinationUnicast:
                    DrawPrimaryDestination();
                    break;
                case Networking.PacketSendMode.MultiDestinationUnicast:
                    DrawPrimaryDestination();
                    EditorGUILayout.PropertyField(additionalDestinationsProperty, true);
                    break;
                case Networking.PacketSendMode.Multicast:
                    EditorGUILayout.PropertyField(multicastGroupIpAddressProperty);
                    EditorGUILayout.PropertyField(multicastPortProperty);
                    EditorGUILayout.PropertyField(joinMulticastGroupProperty);
                    EditorGUILayout.PropertyField(multicastInterfaceAddressProperty);
                    DrawAvailableInterfaces("Multicast 用 NIC", bindAddressProperty, multicastInterfaceAddressProperty, false, true);
                    DrawMulticastSupportSummary((FreeDUdpOutputBehaviour)target);
                    break;
            }

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Apply Preset Now"))
            {
                ((FreeDUdpOutputBehaviour)target).ApplyProfile();
                EditorUtility.SetDirty(target);
            }

            var behaviour = (FreeDUdpOutputBehaviour)target;
            if (behaviour.HasConfigurationWarning)
            {
                EditorGUILayout.HelpBox(behaviour.ConfigurationWarning, MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
                EditorGUILayout.IntField("Packet Length", behaviour.PacketLength);
                EditorGUILayout.IntField("Configured Destinations", behaviour.GetConfiguredDestinationCount());
                EditorGUILayout.IntField("Last Requested Destinations", behaviour.LastRequestedDestinationCount);
                EditorGUILayout.IntField("Last Send Success", behaviour.LastSendSuccessCount);
                EditorGUILayout.IntField("Total Send Failures", behaviour.TotalSendFailureCount);
                EditorGUILayout.Toggle("Skipped By Filter", behaviour.LastSendSkippedByFilter);
                EditorGUILayout.Toggle("Multicast Configured", behaviour.IsMulticastConfigured);
                EditorGUILayout.TextField("Checksum", behaviour.LastChecksum.ToString("X2"));
                EditorGUILayout.TextField("User Area", behaviour.LastUserArea.ToString("X4"));
                EditorGUILayout.TextField("Packet Hex", behaviour.LastPacketHex);
            }
        }

        private static void DrawAvailableInterfaces(string title, SerializedProperty bindAddressProperty, SerializedProperty multicastInterfaceAddressProperty, bool allowBindSelection, bool allowMulticastSelection)
        {
            var interfaces = Networking.FreeDUdpNetworkInterfaceUtility.GetIPv4Interfaces();
            if (interfaces.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            for (var i = 0; i < interfaces.Count; i++)
            {
                var info = interfaces[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.SelectableLabel(BuildInterfaceLabel(info, bindAddressProperty.stringValue, multicastInterfaceAddressProperty.stringValue), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    if (allowBindSelection && GUILayout.Button("Bind", GUILayout.Width(48f)))
                    {
                        bindAddressProperty.stringValue = info.Address;
                    }

                    if (allowMulticastSelection && GUILayout.Button("MCast", GUILayout.Width(56f)))
                    {
                        multicastInterfaceAddressProperty.stringValue = info.Address;
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (allowBindSelection && !string.IsNullOrWhiteSpace(bindAddressProperty.stringValue) && GUILayout.Button("Bind をクリア", GUILayout.Width(100f)))
                {
                    bindAddressProperty.stringValue = string.Empty;
                }

                if (allowMulticastSelection && !string.IsNullOrWhiteSpace(multicastInterfaceAddressProperty.stringValue) && GUILayout.Button("MCast をクリア", GUILayout.Width(112f)))
                {
                    multicastInterfaceAddressProperty.stringValue = string.Empty;
                }
            }
        }

        private static void DrawMulticastSupportSummary(FreeDUdpOutputBehaviour behaviour)
        {
            var snapshot = Support.FreeDUdpMulticastSupportSummary.Build(behaviour);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Multicast Support", EditorStyles.miniBoldLabel);
            for (var i = 0; i < snapshot.Lines.Length; i++)
            {
                EditorGUILayout.LabelField(snapshot.Lines[i], EditorStyles.wordWrappedMiniLabel);
            }

            if (snapshot.HasActionNeeded)
            {
                EditorGUILayout.HelpBox("複数 NIC / join / bind の組み合わせを確認してください。", MessageType.Warning);
            }
        }

        private static string BuildInterfaceLabel(Networking.FreeDUdpNetworkInterfaceInfo info, string bindAddress, string multicastInterfaceAddress)
        {
            var isBind = string.Equals(info.Address, bindAddress);
            var isMulticast = string.Equals(info.Address, multicastInterfaceAddress);
            if (isBind && isMulticast)
            {
                return $"{info}  [Bind][MCast]";
            }

            if (isBind)
            {
                return $"{info}  [Bind]";
            }

            if (isMulticast)
            {
                return $"{info}  [MCast]";
            }

            return info.ToString();
        }

        private void DrawPrimaryDestination()
        {
            EditorGUILayout.PropertyField(destinationIpAddressProperty, new GUIContent("送り先 IP"));
            EditorGUILayout.PropertyField(destinationPortProperty, new GUIContent("送り先 Port"));
        }
    }
}
