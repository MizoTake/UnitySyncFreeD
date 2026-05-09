using System;
using System.Net;
using System.Net.Sockets;
using MizoTake.SyncFreeD.Networking;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class FreeDUdpTransportTests
    {
        [Test]
        public void Send_IncrementsSentCount()
        {
            using (var transport = new FreeDUdpTransport())
            {
                transport.Send(new byte[] { 1, 2, 3 }, "127.0.0.1", 40000);
                Assert.That(transport.TotalSentCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void Send_WithResolvedEndpoint_IncrementsSentCount()
        {
            using (var transport = new FreeDUdpTransport())
            {
                transport.Send(new byte[] { 1, 2, 3 }, new IPEndPoint(IPAddress.Loopback, 40000));
                Assert.That(transport.TotalSentCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void Constructor_AppliesBindAddressAndSocketBufferSize()
        {
            using (var transport = new FreeDUdpTransport("127.0.0.1", 8192))
            {
                Assert.That(transport.BoundAddress, Is.EqualTo("127.0.0.1"));
                Assert.That(transport.ConfiguredSocketBufferSize, Is.EqualTo(8192));
            }
        }

        [Test]
        public void Send_ReachesLocalUdpListenerAcrossManyPackets()
        {
            const int packetCount = 512;
            using (var listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
            using (var transport = new FreeDUdpTransport())
            {
                listener.Client.ReceiveTimeout = 2000;
                var listenPort = ((IPEndPoint)listener.Client.LocalEndPoint).Port;
                for (var index = 0; index < packetCount; index++)
                {
                    var payload = new[] { (byte)0xD1, (byte)(index & 0xFF), (byte)((index >> 8) & 0xFF) };
                    transport.Send(payload, "127.0.0.1", listenPort);
                }

                var receivedCount = 0;
                var lastPayload = Array.Empty<byte>();
                while (receivedCount < packetCount)
                {
                    var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    lastPayload = listener.Receive(ref remoteEndPoint);
                    receivedCount++;
                }

                Assert.That(transport.TotalSentCount, Is.EqualTo(packetCount));
                Assert.That(receivedCount, Is.EqualTo(packetCount));
                Assert.That(lastPayload, Is.EqualTo(new byte[] { 0xD1, 0xFF, 0x01 }));
            }
        }

        [Test]
        public void Send_MulticastReachesLocalListenerWhenLoopbackIsEnabled()
        {
            const string multicastGroup = "239.10.10.43";
            using (var listener = new UdpClient(41043))
            using (var transport = new FreeDUdpTransport())
            {
                listener.Client.ReceiveTimeout = 2000;
                listener.JoinMulticastGroup(IPAddress.Parse(multicastGroup));

                var payload = new byte[] { 0xD1, 0x0A, 0x01, 0x02 };
                transport.Send(payload, multicastGroup, 41043);

                var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var received = listener.Receive(ref remoteEndPoint);
                Assert.That(received, Is.EqualTo(payload));
            }
        }

        [Test]
        public void ConfigureMulticast_StoresConfiguredTtl()
        {
            using (var transport = new FreeDUdpTransport())
            {
                transport.ConfigureMulticast("239.10.10.44", string.Empty, true, 32);

                Assert.That(transport.IsMulticastConfigured, Is.True);
                Assert.That(transport.ConfiguredMulticastTtl, Is.EqualTo(32));
            }
        }
    }
}
