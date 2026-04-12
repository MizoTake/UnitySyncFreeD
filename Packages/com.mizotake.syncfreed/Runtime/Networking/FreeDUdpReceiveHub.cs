using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace MizoTake.SyncFreeD.Networking
{
    public sealed class FreeDUdpReceiveHub : IDisposable
    {
        private static readonly Dictionary<FreeDUdpReceiveConfiguration, FreeDUdpReceiveHub> Hubs = new Dictionary<FreeDUdpReceiveConfiguration, FreeDUdpReceiveHub>();
        private readonly FreeDUdpReceiveConfiguration configuration;
        private readonly List<Action<byte[], IPEndPoint>> listeners = new List<Action<byte[], IPEndPoint>>();
        private readonly UdpClient udpClient;
        private int referenceCount;

        private FreeDUdpReceiveHub(FreeDUdpReceiveConfiguration configuration, UdpClient udpClient, string bindError)
        {
            this.configuration = configuration;
            this.udpClient = udpClient;
            BindError = bindError ?? string.Empty;
        }

        public string BindError { get; }
        public bool IsBound => udpClient != null;

        public static FreeDUdpReceiveHub Acquire(string bindAddress, int listenPort, bool joinMulticastGroup, string multicastGroupIpAddress, string multicastInterfaceAddress, Action<byte[], IPEndPoint> listener, out string bindError)
        {
            var configuration = new FreeDUdpReceiveConfiguration(bindAddress, listenPort, joinMulticastGroup, multicastGroupIpAddress, multicastInterfaceAddress);
            lock (Hubs)
            {
                if (!Hubs.TryGetValue(configuration, out var hub))
                {
                    if (!TryCreateHub(configuration, out hub, out bindError))
                    {
                        return null;
                    }

                    Hubs.Add(configuration, hub);
                }
                else
                {
                    bindError = hub.BindError;
                }

                hub.listeners.Add(listener);
                hub.referenceCount++;
                return hub;
            }
        }

        public void Poll()
        {
            if (udpClient == null || udpClient.Available <= 0)
            {
                return;
            }

            while (udpClient.Available > 0)
            {
                try
                {
                    var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    var packet = udpClient.Receive(ref remoteEndPoint);
                    var listenerSnapshot = listeners.ToArray();
                    for (var i = 0; i < listenerSnapshot.Length; i++)
                    {
                        listenerSnapshot[i]?.Invoke(packet, remoteEndPoint);
                    }
                }
                catch (SocketException)
                {
                    break;
                }
            }
        }

        public void Release(Action<byte[], IPEndPoint> listener)
        {
            lock (Hubs)
            {
                listeners.Remove(listener);
                referenceCount = Math.Max(0, referenceCount - 1);
                if (referenceCount > 0)
                {
                    return;
                }

                Hubs.Remove(configuration);
                Dispose();
            }
        }

        public void Dispose()
        {
            udpClient?.Dispose();
        }

        private static bool TryCreateHub(FreeDUdpReceiveConfiguration configuration, out FreeDUdpReceiveHub hub, out string bindError)
        {
            bindError = string.Empty;
            try
            {
                var udpClient = new UdpClient(AddressFamily.InterNetwork);
                udpClient.Client.ExclusiveAddressUse = false;
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(new IPEndPoint(configuration.HasBindAddress ? IPAddress.Parse(configuration.BindAddress) : IPAddress.Any, configuration.ListenPort));
                udpClient.Client.Blocking = false;
                if (configuration.JoinMulticastGroup && !string.IsNullOrWhiteSpace(configuration.MulticastGroupIpAddress))
                {
                    try
                    {
                        var multicastAddress = IPAddress.Parse(configuration.MulticastGroupIpAddress);
                        if (string.IsNullOrWhiteSpace(configuration.MulticastInterfaceAddress))
                        {
                            udpClient.JoinMulticastGroup(multicastAddress);
                        }
                        else
                        {
                            udpClient.JoinMulticastGroup(multicastAddress, IPAddress.Parse(configuration.MulticastInterfaceAddress));
                        }
                    }
                    catch (Exception exception)
                    {
                        bindError = $"Multicast join failed: {exception.Message}";
                    }
                }

                hub = new FreeDUdpReceiveHub(configuration, udpClient, bindError);
                return true;
            }
            catch (Exception exception)
            {
                bindError = exception.Message;
                hub = null;
                return false;
            }
        }

        private readonly struct FreeDUdpReceiveConfiguration : IEquatable<FreeDUdpReceiveConfiguration>
        {
            public FreeDUdpReceiveConfiguration(string bindAddress, int listenPort, bool joinMulticastGroup, string multicastGroupIpAddress, string multicastInterfaceAddress)
            {
                BindAddress = bindAddress ?? string.Empty;
                ListenPort = listenPort;
                JoinMulticastGroup = joinMulticastGroup;
                MulticastGroupIpAddress = multicastGroupIpAddress ?? string.Empty;
                MulticastInterfaceAddress = multicastInterfaceAddress ?? string.Empty;
            }

            public string BindAddress { get; }
            public int ListenPort { get; }
            public bool JoinMulticastGroup { get; }
            public string MulticastGroupIpAddress { get; }
            public string MulticastInterfaceAddress { get; }
            public bool HasBindAddress => !string.IsNullOrWhiteSpace(BindAddress);

            public bool Equals(FreeDUdpReceiveConfiguration other)
            {
                return ListenPort == other.ListenPort
                    && JoinMulticastGroup == other.JoinMulticastGroup
                    && string.Equals(BindAddress, other.BindAddress, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(MulticastGroupIpAddress, other.MulticastGroupIpAddress, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(MulticastInterfaceAddress, other.MulticastInterfaceAddress, StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object obj)
            {
                return obj is FreeDUdpReceiveConfiguration other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = ListenPort;
                    hashCode = (hashCode * 397) ^ JoinMulticastGroup.GetHashCode();
                    hashCode = (hashCode * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(BindAddress);
                    hashCode = (hashCode * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(MulticastGroupIpAddress);
                    hashCode = (hashCode * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(MulticastInterfaceAddress);
                    return hashCode;
                }
            }
        }
    }
}
