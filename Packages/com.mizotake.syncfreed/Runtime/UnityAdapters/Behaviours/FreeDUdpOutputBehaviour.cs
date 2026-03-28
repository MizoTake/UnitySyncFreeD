using System;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Outputs;
using MizoTake.SyncFreeD.Networking;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    public class FreeDUdpOutputBehaviour : MonoBehaviour
    {
        [SerializeField] private PacketSendMode packetSendMode = PacketSendMode.SingleDestinationUnicast;
        [SerializeField] private string destinationIpAddress = "127.0.0.1";
        [SerializeField] private int destinationPort = 40000;
        [SerializeField] private FreeDUdpDestination[] additionalDestinations = Array.Empty<FreeDUdpDestination>();
        [SerializeField] private string multicastGroupIpAddress = "239.0.0.1";
        [SerializeField] private int multicastPort = 40000;
        [SerializeField] private string bindAddress = string.Empty;
        [SerializeField] private int socketBufferSize = 0;
        [SerializeField] private int cameraIdFilter = -1;
        [SerializeField] private bool joinMulticastGroup = true;
        [SerializeField] private string multicastInterfaceAddress = string.Empty;

        private readonly FreeDPacketBuilder packetBuilder = new FreeDPacketBuilder();
        private readonly byte[] packetBuffer = new byte[FreeDPacketBuilder.PacketLength];
        private FreeDUdpTransport transport;

        public string LastPacketHex { get; private set; } = string.Empty;
        public int LastSendSuccessCount { get; private set; }
        public int TotalSendFailureCount { get; private set; }
        public PacketSendMode SendMode => packetSendMode;
        public byte LastChecksum => packetBuffer[packetBuffer.Length - 1];
        public ushort LastUserArea => (ushort)((packetBuffer[26] << 8) | packetBuffer[27]);
        public int PacketLength => packetBuffer.Length;
        public bool LastSendSkippedByFilter { get; private set; }
        public int LastRequestedDestinationCount { get; private set; }
        public bool IsMulticastConfigured => transport != null && transport.IsMulticastConfigured;
        public string ConfigurationWarning => FreeDUdpConfigurationValidator.Validate(bindAddress, multicastGroupIpAddress, multicastInterfaceAddress, packetSendMode);
        public bool HasConfigurationWarning => !string.IsNullOrEmpty(ConfigurationWarning);

        public byte[] BuildPacket(in CameraSyncState state)
        {
            packetBuilder.Build(state, packetBuffer);
            LastPacketHex = BitConverter.ToString(packetBuffer);
            return packetBuffer;
        }

        public void Send(in CameraSyncState state)
        {
            if (cameraIdFilter >= 0 && state.CameraId != cameraIdFilter)
            {
                LastSendSkippedByFilter = true;
                LastSendSuccessCount = 0;
                LastRequestedDestinationCount = 0;
                return;
            }

            LastSendSkippedByFilter = false;
            EnsureSocket();
            packetBuilder.Build(state, packetBuffer);
            LastPacketHex = BitConverter.ToString(packetBuffer);
            LastSendSuccessCount = 0;
            LastRequestedDestinationCount = GetRequestedDestinationCount();
            switch (packetSendMode)
            {
                case PacketSendMode.SingleDestinationUnicast:
                    SendPrimaryDestination();
                    break;
                case PacketSendMode.MultiDestinationUnicast:
                    SendPrimaryDestination();
                    SendAdditionalDestinations();
                    break;
                case PacketSendMode.Multicast:
                    transport.ConfigureMulticast(multicastGroupIpAddress, multicastInterfaceAddress, joinMulticastGroup);
                    if (TrySend(multicastGroupIpAddress, multicastPort))
                    {
                        LastSendSuccessCount++;
                    }

                    break;
            }
        }

        public int GetConfiguredDestinationCount()
        {
            return GetRequestedDestinationCount();
        }

        private void EnsureSocket()
        {
            transport ??= new FreeDUdpTransport(bindAddress, socketBufferSize);
        }

        private bool TrySend(string ipAddress, int port)
        {
            try
            {
                transport.Send(packetBuffer, ipAddress, port);
                return true;
            }
            catch (Exception exception)
            {
                TotalSendFailureCount++;
                Debug.LogWarning($"SyncFreeD UDP send failed: {exception.Message}", this);
                return false;
            }
        }

        private void SendPrimaryDestination()
        {
            if (TrySend(destinationIpAddress, destinationPort))
            {
                LastSendSuccessCount++;
            }
        }

        private void SendAdditionalDestinations()
        {
            for (var i = 0; i < additionalDestinations.Length; i++)
            {
                var destination = additionalDestinations[i];
                if (!destination.Enabled)
                {
                    continue;
                }

                if (TrySend(destination.IpAddress, destination.Port))
                {
                    LastSendSuccessCount++;
                }
            }
        }

        private int GetRequestedDestinationCount()
        {
            switch (packetSendMode)
            {
                case PacketSendMode.MultiDestinationUnicast:
                    return 1 + CountEnabledAdditionalDestinations();
                case PacketSendMode.Multicast:
                    return 1;
                case PacketSendMode.SingleDestinationUnicast:
                default:
                    return 1;
            }
        }

        private int CountEnabledAdditionalDestinations()
        {
            var count = 0;
            for (var i = 0; i < additionalDestinations.Length; i++)
            {
                if (additionalDestinations[i].Enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private void OnDestroy()
        {
            transport?.Dispose();
            transport = null;
        }
    }
}
