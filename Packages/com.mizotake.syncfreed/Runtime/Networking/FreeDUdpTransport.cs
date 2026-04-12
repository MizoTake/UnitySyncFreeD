using System;
using System.Net;
using System.Net.Sockets;

namespace MizoTake.SyncFreeD.Networking
{
    public sealed class FreeDUdpTransport : IDisposable
    {
        private readonly UdpClient udpClient;
        private string configuredMulticastGroup = string.Empty;
        private string configuredMulticastInterface = string.Empty;

        public int TotalSentCount { get; private set; }
        public string BoundAddress { get; }
        public int ConfiguredSocketBufferSize { get; }
        public int ConfiguredMulticastTtl { get; private set; } = 1;
        public bool IsMulticastConfigured { get; private set; }

        public FreeDUdpTransport(string bindAddress = "", int socketBufferSize = 0)
        {
            if (string.IsNullOrWhiteSpace(bindAddress))
            {
                udpClient = new UdpClient();
                BoundAddress = string.Empty;
            }
            else
            {
                udpClient = new UdpClient(new IPEndPoint(IPAddress.Parse(bindAddress), 0));
                BoundAddress = bindAddress;
            }

            if (socketBufferSize > 0)
            {
                udpClient.Client.SendBufferSize = socketBufferSize;
                udpClient.Client.ReceiveBufferSize = socketBufferSize;
                ConfiguredSocketBufferSize = socketBufferSize;
            }

            udpClient.MulticastLoopback = true;
            udpClient.Ttl = (short)ConfiguredMulticastTtl;
        }

        public void Send(byte[] payload, string ipAddress, int port)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            udpClient.Send(payload, payload.Length, ipAddress, port);
            TotalSentCount++;
        }

        public void ConfigureMulticast(string multicastGroupIpAddress, string interfaceAddress, bool joinMulticastGroup)
        {
            ConfigureMulticast(multicastGroupIpAddress, interfaceAddress, joinMulticastGroup, ConfiguredMulticastTtl);
        }

        public void ConfigureMulticast(string multicastGroupIpAddress, string interfaceAddress, bool joinMulticastGroup, int multicastTtl)
        {
            ConfiguredMulticastTtl = Math.Clamp(multicastTtl, 1, 255);
            udpClient.Ttl = (short)ConfiguredMulticastTtl;
            if (!joinMulticastGroup || string.IsNullOrWhiteSpace(multicastGroupIpAddress))
            {
                IsMulticastConfigured = false;
                configuredMulticastGroup = string.Empty;
                configuredMulticastInterface = string.Empty;
                return;
            }

            if (IsMulticastConfigured && configuredMulticastGroup == multicastGroupIpAddress && configuredMulticastInterface == (interfaceAddress ?? string.Empty))
            {
                return;
            }

            var groupAddress = IPAddress.Parse(multicastGroupIpAddress);
            if (string.IsNullOrWhiteSpace(interfaceAddress))
            {
                udpClient.JoinMulticastGroup(groupAddress);
            }
            else
            {
                udpClient.JoinMulticastGroup(groupAddress, IPAddress.Parse(interfaceAddress));
            }

            IsMulticastConfigured = true;
            configuredMulticastGroup = multicastGroupIpAddress;
            configuredMulticastInterface = interfaceAddress ?? string.Empty;
        }

        public void Dispose()
        {
            udpClient.Dispose();
        }
    }
}
