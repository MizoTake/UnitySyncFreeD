using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using UnityEditor;
using UnityEngine;

namespace MizoTake.SyncFreeD.Editor.Inspectors
{
    [CustomEditor(typeof(FreeDUdpOutputBehaviour))]
    public sealed class FreeDUdpOutputBehaviourEditor : UnityEditor.Editor
    {
        private SerializedProperty packetSendModeProperty;
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
            EditorGUILayout.PropertyField(packetSendModeProperty);
            EditorGUILayout.PropertyField(bindAddressProperty);
            EditorGUILayout.PropertyField(socketBufferSizeProperty);
            EditorGUILayout.PropertyField(cameraIdFilterProperty);
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
                    DrawAvailableInterfaces(multicastInterfaceAddressProperty);
                    break;
            }

            serializedObject.ApplyModifiedProperties();

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

        private static void DrawAvailableInterfaces(SerializedProperty multicastInterfaceAddressProperty)
        {
            var interfaces = Networking.FreeDUdpNetworkInterfaceUtility.GetIPv4Interfaces();
            if (interfaces.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Available IPv4 Interfaces", EditorStyles.miniBoldLabel);
            for (var i = 0; i < interfaces.Count; i++)
            {
                var info = interfaces[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.SelectableLabel(info.ToString(), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    if (GUILayout.Button("Use", GUILayout.Width(48f)))
                    {
                        multicastInterfaceAddressProperty.stringValue = info.Address;
                    }
                }
            }
        }

        private void DrawPrimaryDestination()
        {
            EditorGUILayout.PropertyField(destinationIpAddressProperty);
            EditorGUILayout.PropertyField(destinationPortProperty);
        }
    }
}
