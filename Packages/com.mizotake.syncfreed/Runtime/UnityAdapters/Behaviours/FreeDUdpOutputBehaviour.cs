using System;
using System.Collections.Generic;
using System.Diagnostics;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Outputs;
using MizoTake.SyncFreeD.Networking;
using MizoTake.SyncFreeD.ScriptableObjects;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    public class FreeDUdpOutputBehaviour : MonoBehaviour
    {
        [SerializeField] private FreeDUdpOutputProfileAsset outputProfileAsset;
        [SerializeField] private bool applyProfileOnEnable = true;
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
        private readonly List<FreeDUdpDestinationDiagnostic> lastDestinationDiagnostics = new List<FreeDUdpDestinationDiagnostic>();
        private FreeDUdpTransport transport;

        public string LastPacketHex { get; private set; } = string.Empty;
        public int LastSendSuccessCount { get; private set; }
        public int TotalSendFailureCount { get; private set; }
        public PacketSendMode SendMode => packetSendMode;
        public FreeDUdpDestination[] AdditionalDestinations => CloneDestinations(additionalDestinations);
        public byte LastChecksum => packetBuffer[packetBuffer.Length - 1];
        public ushort LastUserArea => (ushort)((packetBuffer[26] << 8) | packetBuffer[27]);
        public int PacketLength => packetBuffer.Length;
        public bool LastSendSkippedByFilter { get; private set; }
        public int LastRequestedDestinationCount { get; private set; }
        public FreeDUdpDestinationDiagnostic[] LastDestinationDiagnostics => lastDestinationDiagnostics.ToArray();
        public long LastSendSpreadMicroseconds { get; private set; }
        public long LastDestinationSpreadMicroseconds => LastSendSpreadMicroseconds;
        public bool IsMulticastConfigured => transport != null && transport.IsMulticastConfigured;
        public string BindAddress => bindAddress;
        public string DestinationIpAddress => destinationIpAddress;
        public int DestinationPort => destinationPort;
        public int SocketBufferSize => socketBufferSize;
        public int CameraIdFilter => cameraIdFilter;
        public string MulticastGroupIpAddress => multicastGroupIpAddress;
        public int MulticastPort => multicastPort;
        public bool JoinMulticastGroup => joinMulticastGroup;
        public string MulticastInterfaceAddress => multicastInterfaceAddress;
        public FreeDUdpOutputProfileAsset OutputProfileAsset => outputProfileAsset;
        public bool ApplyProfileOnEnable => applyProfileOnEnable;
        public string ConfigurationWarning => FreeDUdpConfigurationValidator.Validate(packetSendMode, bindAddress, destinationIpAddress, destinationPort, additionalDestinations, multicastGroupIpAddress, multicastPort, multicastInterfaceAddress, socketBufferSize, cameraIdFilter);
        public bool HasConfigurationWarning => !string.IsNullOrEmpty(ConfigurationWarning);

        private void Awake()
        {
            if (applyProfileOnEnable)
            {
                ApplyProfile();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (applyProfileOnEnable)
            {
                ApplyProfile();
            }
        }
#endif

        public byte[] BuildPacket(in CameraSyncState state)
        {
            packetBuilder.Build(state, packetBuffer);
            LastPacketHex = BitConverter.ToString(packetBuffer);
            return packetBuffer;
        }

        public void SetOutputProfileAsset(FreeDUdpOutputProfileAsset profileAsset, bool applyImmediately)
        {
            outputProfileAsset = profileAsset;
            if (applyImmediately)
            {
                ApplyProfile();
            }
        }

        public void ApplyProfile()
        {
            if (outputProfileAsset == null || outputProfileAsset.Value == null)
            {
                return;
            }

            var profile = outputProfileAsset.Value;
            packetSendMode = profile.PacketSendMode;
            destinationIpAddress = profile.DestinationIpAddress ?? "127.0.0.1";
            destinationPort = profile.DestinationPort;
            additionalDestinations = CloneDestinations(profile.AdditionalDestinations);
            multicastGroupIpAddress = profile.MulticastGroupIpAddress ?? "239.0.0.1";
            multicastPort = profile.MulticastPort;
            bindAddress = profile.BindAddress ?? string.Empty;
            socketBufferSize = profile.SocketBufferSize;
            cameraIdFilter = profile.CameraIdFilter;
            joinMulticastGroup = profile.JoinMulticastGroup;
            multicastInterfaceAddress = profile.MulticastInterfaceAddress ?? string.Empty;
        }

        public void Send(in CameraSyncState state)
        {
            if (cameraIdFilter >= 0 && state.CameraId != cameraIdFilter)
            {
                LastSendSkippedByFilter = true;
                LastSendSuccessCount = 0;
                LastRequestedDestinationCount = 0;
                lastDestinationDiagnostics.Clear();
                LastSendSpreadMicroseconds = 0L;
                return;
            }

            LastSendSkippedByFilter = false;
            EnsureSocket();
            packetBuilder.Build(state, packetBuffer);
            LastPacketHex = BitConverter.ToString(packetBuffer);
            LastSendSuccessCount = 0;
            LastRequestedDestinationCount = GetRequestedDestinationCount();
            lastDestinationDiagnostics.Clear();
            LastSendSpreadMicroseconds = 0L;
            var sendStartTimestamp = Stopwatch.GetTimestamp();
            switch (packetSendMode)
            {
                case PacketSendMode.SingleDestinationUnicast:
                    SendPrimaryDestination(sendStartTimestamp);
                    break;
                case PacketSendMode.MultiDestinationUnicast:
                    SendPrimaryDestination(sendStartTimestamp);
                    SendAdditionalDestinations(sendStartTimestamp);
                    break;
                case PacketSendMode.Multicast:
                    transport.ConfigureMulticast(multicastGroupIpAddress, multicastInterfaceAddress, joinMulticastGroup);
                    if (TrySend(multicastGroupIpAddress, multicastPort, sendStartTimestamp, 0))
                    {
                        LastSendSuccessCount++;
                    }

                    break;
            }

            UpdateSendSpread();
        }

        public int GetConfiguredDestinationCount()
        {
            return GetRequestedDestinationCount();
        }

        private void EnsureSocket()
        {
            transport ??= new FreeDUdpTransport(bindAddress, socketBufferSize);
        }

        private bool TrySend(string ipAddress, int port, long sendStartTimestamp, int order)
        {
            try
            {
                transport.Send(packetBuffer, ipAddress, port);
                RecordDestinationDiagnostic(ipAddress, port, order, sendStartTimestamp, true);
                return true;
            }
            catch (Exception exception)
            {
                TotalSendFailureCount++;
                RecordDestinationDiagnostic(ipAddress, port, order, sendStartTimestamp, false);
                UnityEngine.Debug.LogWarning($"SyncFreeD UDP send failed: {exception.Message}", this);
                return false;
            }
        }

        private void SendPrimaryDestination(long sendStartTimestamp)
        {
            if (TrySend(destinationIpAddress, destinationPort, sendStartTimestamp, 0))
            {
                LastSendSuccessCount++;
            }
        }

        private void SendAdditionalDestinations(long sendStartTimestamp)
        {
            var order = 1;
            for (var i = 0; i < additionalDestinations.Length; i++)
            {
                var destination = additionalDestinations[i];
                if (!destination.Enabled)
                {
                    continue;
                }

                if (TrySend(destination.IpAddress, destination.Port, sendStartTimestamp, order))
                {
                    LastSendSuccessCount++;
                }

                order++;
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

        private void RecordDestinationDiagnostic(string ipAddress, int port, int order, long sendStartTimestamp, bool success)
        {
            var elapsedMicroseconds = (long)(((Stopwatch.GetTimestamp() - sendStartTimestamp) * 1000000d) / Stopwatch.Frequency);
            lastDestinationDiagnostics.Add(new FreeDUdpDestinationDiagnostic($"{ipAddress}:{port}", order, elapsedMicroseconds, success));
        }

        private void UpdateSendSpread()
        {
            if (lastDestinationDiagnostics.Count <= 1)
            {
                LastSendSpreadMicroseconds = 0L;
                return;
            }

            LastSendSpreadMicroseconds = Math.Max(0L, lastDestinationDiagnostics[lastDestinationDiagnostics.Count - 1].ElapsedMicroseconds - lastDestinationDiagnostics[0].ElapsedMicroseconds);
        }

        private static FreeDUdpDestination[] CloneDestinations(FreeDUdpDestination[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<FreeDUdpDestination>();
            }

            var clone = new FreeDUdpDestination[source.Length];
            Array.Copy(source, clone, source.Length);
            return clone;
        }
    }
}
