using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;

namespace MizoTake.SyncFreeD.Networking
{
    public sealed class FreeDUdpReceiveHub : IDisposable
    {
        private const int MaxPacketsPerPoll = 256;
        private static readonly Dictionary<FreeDUdpReceiveConfiguration, FreeDUdpReceiveHub> Hubs = new Dictionary<FreeDUdpReceiveConfiguration, FreeDUdpReceiveHub>();
        private readonly FreeDUdpReceiveConfiguration configuration;
        private readonly List<Action<byte[], IPEndPoint>> listeners = new List<Action<byte[], IPEndPoint>>();
        private Action<byte[], IPEndPoint>[] listenerSnapshot = Array.Empty<Action<byte[], IPEndPoint>>();
        private UdpClient udpClient;
        private bool disposeRequested;
        private int dispatchDepth;
        private int referenceCount;

        private FreeDUdpReceiveHub(FreeDUdpReceiveConfiguration configuration, UdpClient udpClient, string bindError)
        {
            this.configuration = configuration;
            this.udpClient = udpClient;
            BindError = bindError ?? string.Empty;
        }

        public string BindError { get; }
        public bool IsBound => udpClient != null && !disposeRequested;

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
                hub.RefreshListenerSnapshot();
                hub.referenceCount++;
                return hub;
            }
        }

        public void Poll()
        {
            var processedPacketCount = 0;
            while (processedPacketCount < MaxPacketsPerPoll)
            {
                UdpClient client;
                lock (Hubs)
                {
                    if (udpClient == null || disposeRequested)
                    {
                        return;
                    }

                    client = udpClient;
                }

                if (!TryReceive(client, out var packet, out var remoteEndPoint))
                {
                    return;
                }

                Action<byte[], IPEndPoint>[] currentListeners;
                lock (Hubs)
                {
                    currentListeners = listenerSnapshot;
                    dispatchDepth++;
                }

                Exception listenerFailure = null;
                try
                {
                    for (var i = 0; i < currentListeners.Length; i++)
                    {
                        try
                        {
                            currentListeners[i]?.Invoke(packet, remoteEndPoint);
                        }
                        catch (Exception exception)
                        {
                            listenerFailure ??= exception;
                        }
                    }
                }
                finally
                {
                    lock (Hubs)
                    {
                        dispatchDepth--;
                        if (dispatchDepth == 0 && disposeRequested)
                        {
                            DisposeClient();
                        }
                    }
                }

                processedPacketCount++;
                if (listenerFailure != null)
                {
                    ExceptionDispatchInfo.Capture(listenerFailure).Throw();
                }
            }
        }

        public void Release(Action<byte[], IPEndPoint> listener)
        {
            lock (Hubs)
            {
                if (!listeners.Remove(listener))
                {
                    return;
                }

                RefreshListenerSnapshot();
                referenceCount--;
                if (referenceCount > 0)
                {
                    return;
                }

                Hubs.Remove(configuration);
                RequestDispose();
            }
        }

        public void Dispose()
        {
            lock (Hubs)
            {
                Hubs.Remove(configuration);
                listeners.Clear();
                RefreshListenerSnapshot();
                referenceCount = 0;
                RequestDispose();
            }
        }

        private static bool TryCreateHub(FreeDUdpReceiveConfiguration configuration, out FreeDUdpReceiveHub hub, out string bindError)
        {
            bindError = string.Empty;
            UdpClient udpClient = null;
            try
            {
                udpClient = new UdpClient(AddressFamily.InterNetwork);
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
                udpClient = null;
                return true;
            }
            catch (Exception exception)
            {
                udpClient?.Dispose();
                bindError = exception.Message;
                hub = null;
                return false;
            }
        }

        private void RequestDispose()
        {
            disposeRequested = true;
            if (dispatchDepth == 0)
            {
                DisposeClient();
            }
        }

        private void DisposeClient()
        {
            var client = udpClient;
            udpClient = null;
            client?.Dispose();
        }

        private void RefreshListenerSnapshot()
        {
            listenerSnapshot = listeners.ToArray();
        }

        private static bool TryReceive(UdpClient client, out byte[] packet, out IPEndPoint remoteEndPoint)
        {
            packet = null;
            remoteEndPoint = null;
            try
            {
                if (client.Available <= 0)
                {
                    return false;
                }

                remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                packet = client.Receive(ref remoteEndPoint);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        private readonly struct FreeDUdpReceiveConfiguration : IEquatable<FreeDUdpReceiveConfiguration>
        {
            public FreeDUdpReceiveConfiguration(string bindAddress, int listenPort, bool joinMulticastGroup, string multicastGroupIpAddress, string multicastInterfaceAddress)
            {
                BindAddress = NormalizeBindAddress(bindAddress);
                ListenPort = listenPort;
                JoinMulticastGroup = joinMulticastGroup && !string.IsNullOrWhiteSpace(multicastGroupIpAddress);
                MulticastGroupIpAddress = JoinMulticastGroup ? NormalizeOptionalIpAddress(multicastGroupIpAddress) : string.Empty;
                MulticastInterfaceAddress = JoinMulticastGroup ? NormalizeInterfaceAddress(multicastInterfaceAddress) : string.Empty;
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

            private static string NormalizeOptionalIpAddress(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return string.Empty;
                }

                var trimmedValue = value.Trim();
                return IPAddress.TryParse(trimmedValue, out var address) ? address.ToString() : trimmedValue;
            }

            private static string NormalizeBindAddress(string value)
            {
                var normalizedAddress = NormalizeOptionalIpAddress(value);
                return string.Equals(normalizedAddress, IPAddress.Any.ToString(), StringComparison.Ordinal) ? string.Empty : normalizedAddress;
            }

            private static string NormalizeInterfaceAddress(string value)
            {
                var normalizedAddress = NormalizeOptionalIpAddress(value);
                return string.Equals(normalizedAddress, IPAddress.Any.ToString(), StringComparison.Ordinal) ? string.Empty : normalizedAddress;
            }
        }
    }
}
