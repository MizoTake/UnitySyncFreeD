using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
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
        [SerializeField] private int multicastTtl = 1;
        [SerializeField] private string bindAddress = string.Empty;
        [SerializeField] private int socketBufferSize;
        [SerializeField] private int cameraIdFilter = -1;
        [SerializeField] private bool joinMulticastGroup = true;
        [SerializeField] private string multicastInterfaceAddress = string.Empty;

        private readonly FreeDPacketBuilder packetBuilder = new FreeDPacketBuilder();
        private readonly byte[] packetBuffer = new byte[FreeDPacketBuilder.PacketLength];
        private readonly List<FreeDUdpDestinationDiagnostic> lastDestinationDiagnostics = new List<FreeDUdpDestinationDiagnostic>();
        private readonly List<EndpointCacheEntry> endpointCache = new List<EndpointCacheEntry>();
        private FreeDUdpTransport transport;
        private string lastPacketHex = string.Empty;
        private bool hasPacket;
        private bool lastPacketHexDirty;

        public string LastPacketHex
        {
            get
            {
                if (!hasPacket)
                {
                    return string.Empty;
                }

                if (lastPacketHexDirty)
                {
                    lastPacketHex = BitConverter.ToString(packetBuffer);
                    lastPacketHexDirty = false;
                }

                return lastPacketHex;
            }
        }
        public int LastSendSuccessCount { get; private set; }
        public int TotalSendFailureCount { get; private set; }
        public PacketSendMode SendMode => packetSendMode;
        public FreeDUdpDestination[] AdditionalDestinations => CloneDestinations(additionalDestinations);
        public byte LastChecksum => packetBuffer[packetBuffer.Length - 1];
        public ushort LastUserArea => (ushort)((packetBuffer[26] << 8) | packetBuffer[27]);
        public int PacketLength => packetBuffer.Length;
        public bool LastSendSkippedByFilter { get; private set; }
        public int LastRequestedDestinationCount { get; private set; }
        public int LastDestinationDiagnosticCount => lastDestinationDiagnostics.Count;
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
        public int MulticastTtl => multicastTtl;
        public bool JoinMulticastGroup => joinMulticastGroup;
        public string MulticastInterfaceAddress => multicastInterfaceAddress;
        public FreeDUdpOutputProfileAsset OutputProfileAsset => outputProfileAsset;
        public bool ApplyProfileOnEnable => applyProfileOnEnable;
        public PacketSendMode LastEffectiveSendMode { get; private set; } = PacketSendMode.SingleDestinationUnicast;
        public string ConfigurationWarning => FreeDUdpConfigurationValidator.Validate(packetSendMode, bindAddress, destinationIpAddress, destinationPort, additionalDestinations, multicastGroupIpAddress, multicastPort, multicastInterfaceAddress, multicastTtl, socketBufferSize, cameraIdFilter);
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
            MarkPacketBufferChanged();
            return ClonePacketBuffer();
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
            multicastTtl = profile.MulticastTtl;
            bindAddress = profile.BindAddress ?? string.Empty;
            socketBufferSize = profile.SocketBufferSize;
            cameraIdFilter = profile.CameraIdFilter;
            joinMulticastGroup = profile.JoinMulticastGroup;
            multicastInterfaceAddress = profile.MulticastInterfaceAddress ?? string.Empty;
            endpointCache.Clear();
        }

        public void Send(in CameraSyncState state)
        {
            Send(state, packetSendMode, int.MaxValue);
        }

        public void Send(in CameraSyncState state, PacketSendMode effectiveSendMode, int additionalDestinationLimit)
        {
            if (cameraIdFilter >= 0 && state.CameraId != cameraIdFilter)
            {
                LastSendSkippedByFilter = true;
                LastSendSuccessCount = 0;
                LastRequestedDestinationCount = 0;
                LastEffectiveSendMode = effectiveSendMode;
                lastDestinationDiagnostics.Clear();
                LastSendSpreadMicroseconds = 0L;
                return;
            }

            packetBuilder.Build(state, packetBuffer);
            MarkPacketBufferChanged();
            LastSendSkippedByFilter = false;
            LastSendSuccessCount = 0;
            LastEffectiveSendMode = effectiveSendMode;
            LastRequestedDestinationCount = GetRequestedDestinationCount(effectiveSendMode, additionalDestinationLimit);
            lastDestinationDiagnostics.Clear();
            LastSendSpreadMicroseconds = 0L;
            var sendStartTimestamp = Stopwatch.GetTimestamp();
            if (!TryEnsureSocket(sendStartTimestamp))
            {
                UpdateSendSpread();
                return;
            }

            switch (effectiveSendMode)
            {
                case PacketSendMode.SingleDestinationUnicast:
                    SendPrimaryDestination(sendStartTimestamp);
                    break;
                case PacketSendMode.MultiDestinationUnicast:
                    SendPrimaryDestination(sendStartTimestamp);
                    SendAdditionalDestinations(sendStartTimestamp, additionalDestinationLimit);
                    break;
                case PacketSendMode.Multicast:
                    if (TryConfigureMulticast(sendStartTimestamp) && TrySend(multicastGroupIpAddress, multicastPort, sendStartTimestamp, 0))
                    {
                        LastSendSuccessCount++;
                    }

                    break;
            }

            UpdateSendSpread();
        }

        public int GetConfiguredDestinationCount()
        {
            return GetRequestedDestinationCount(packetSendMode, int.MaxValue);
        }

        private void EnsureSocket()
        {
            transport ??= new FreeDUdpTransport(bindAddress, socketBufferSize);
        }

        private bool TryEnsureSocket(long sendStartTimestamp)
        {
            try
            {
                EnsureSocket();
                return true;
            }
            catch (Exception exception)
            {
                TotalSendFailureCount++;
                RecordDestinationDiagnostic(string.Empty, 0, 0, sendStartTimestamp, false);
                UnityEngine.Debug.LogWarning($"SyncFreeD UDP transport setup failed: {exception.Message}", this);
                return false;
            }
        }

        private bool TryConfigureMulticast(long sendStartTimestamp)
        {
            try
            {
                transport.ConfigureMulticast(multicastGroupIpAddress, multicastInterfaceAddress, joinMulticastGroup, multicastTtl);
                return true;
            }
            catch (Exception exception)
            {
                TotalSendFailureCount++;
                RecordDestinationDiagnostic(multicastGroupIpAddress, multicastPort, 0, sendStartTimestamp, false);
                UnityEngine.Debug.LogWarning($"SyncFreeD UDP multicast setup failed: {exception.Message}", this);
                return false;
            }
        }

        private bool TrySend(string ipAddress, int port, long sendStartTimestamp, int order)
        {
            try
            {
                transport.Send(packetBuffer, GetCachedEndpoint(ipAddress, port));
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

        private void SendAdditionalDestinations(long sendStartTimestamp, int additionalDestinationLimit)
        {
            var order = 1;
            var remaining = additionalDestinationLimit < 0 ? 0 : additionalDestinationLimit;
            for (var i = 0; i < additionalDestinations.Length; i++)
            {
                var destination = additionalDestinations[i];
                if (!destination.Enabled)
                {
                    continue;
                }

                if (remaining <= 0)
                {
                    break;
                }

                if (TrySend(destination.IpAddress, destination.Port, sendStartTimestamp, order))
                {
                    LastSendSuccessCount++;
                }

                order++;
                remaining--;
            }
        }

        private int GetRequestedDestinationCount(PacketSendMode sendMode, int additionalDestinationLimit)
        {
            switch (sendMode)
            {
                case PacketSendMode.MultiDestinationUnicast:
                    return 1 + CountEnabledAdditionalDestinations(additionalDestinationLimit);
                case PacketSendMode.Multicast:
                    return 1;
                case PacketSendMode.SingleDestinationUnicast:
                default:
                    return 1;
            }
        }

        private int CountEnabledAdditionalDestinations(int additionalDestinationLimit)
        {
            if (additionalDestinationLimit <= 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < additionalDestinations.Length; i++)
            {
                if (additionalDestinations[i].Enabled)
                {
                    count++;
                    if (count >= additionalDestinationLimit)
                    {
                        break;
                    }
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
            lastDestinationDiagnostics.Add(new FreeDUdpDestinationDiagnostic(ipAddress, port, order, elapsedMicroseconds, success));
        }

        private IPEndPoint GetCachedEndpoint(string ipAddress, int port)
        {
            for (var i = 0; i < endpointCache.Count; i++)
            {
                if (endpointCache[i].Matches(ipAddress, port))
                {
                    return endpointCache[i].EndPoint;
                }
            }

            var endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            endpointCache.Add(new EndpointCacheEntry(ipAddress, port, endPoint));
            return endPoint;
        }

        private void MarkPacketBufferChanged()
        {
            hasPacket = true;
            lastPacketHexDirty = true;
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

        private byte[] ClonePacketBuffer()
        {
            var clone = new byte[packetBuffer.Length];
            Array.Copy(packetBuffer, clone, packetBuffer.Length);
            return clone;
        }

        private readonly struct EndpointCacheEntry
        {
            public EndpointCacheEntry(string ipAddress, int port, IPEndPoint endPoint)
            {
                IpAddress = ipAddress ?? string.Empty;
                Port = port;
                EndPoint = endPoint;
            }

            public string IpAddress { get; }
            public int Port { get; }
            public IPEndPoint EndPoint { get; }

            public bool Matches(string ipAddress, int port)
            {
                return Port == port && string.Equals(IpAddress, ipAddress ?? string.Empty, StringComparison.Ordinal);
            }
        }
    }
}
