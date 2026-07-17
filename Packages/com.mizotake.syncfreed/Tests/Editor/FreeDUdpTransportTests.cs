using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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

        [Test]
        public void ConfigureMulticast_DisablesAnExistingMembershipWhenJoinIsFalse()
        {
            using (var transport = new FreeDUdpTransport())
            {
                transport.ConfigureMulticast("239.10.10.45", string.Empty, true, 32);
                transport.ConfigureMulticast("239.10.10.45", string.Empty, false, 32);

                Assert.That(transport.IsMulticastConfigured, Is.False);
            }
        }

        [Test]
        public void ReceiveHub_UsesEffectiveConfigurationAndAllowsIdempotentReleaseDuringDispatch()
        {
            var listenPort = GetAvailableUdpPort();
            var firstReceivedCount = 0;
            var secondReceivedCount = 0;
            FreeDUdpReceiveHub firstHub = null;
            Action<byte[], IPEndPoint> firstListener = null;
            firstListener = (packet, remoteEndPoint) =>
            {
                firstReceivedCount++;
                firstHub.Release(firstListener);
                firstHub.Release(firstListener);
            };
            Action<byte[], IPEndPoint> secondListener = (packet, remoteEndPoint) => secondReceivedCount++;
            firstHub = FreeDUdpReceiveHub.Acquire(" 0.0.0.0 ", listenPort, false, "239.10.10.46", "127.0.0.1", firstListener, out var firstBindError);
            var secondHub = FreeDUdpReceiveHub.Acquire(string.Empty, listenPort, false, "239.10.10.47", string.Empty, secondListener, out var secondBindError);

            try
            {
                Assert.That(firstHub, Is.Not.Null, firstBindError);
                Assert.That(secondHub, Is.SameAs(firstHub), secondBindError);
                using (var sender = new UdpClient())
                {
                    sender.Send(new byte[] { 0xD1 }, 1, new IPEndPoint(IPAddress.Loopback, listenPort));
                }

                var timeout = Stopwatch.StartNew();
                while (secondReceivedCount == 0 && timeout.ElapsedMilliseconds < 2000)
                {
                    firstHub.Poll();
                    Thread.Sleep(1);
                }

                Assert.That(firstReceivedCount, Is.EqualTo(1));
                Assert.That(secondReceivedCount, Is.EqualTo(1));
                Assert.That(firstHub.IsBound, Is.True);
            }
            finally
            {
                firstHub?.Release(firstListener);
                secondHub?.Release(secondListener);
            }
        }

        [Test]
        public void ReceiveHub_InvokesLaterListenersBeforeRethrowingAListenerFailure()
        {
            var listenPort = GetAvailableUdpPort();
            var laterListenerReceivedCount = 0;
            Action<byte[], IPEndPoint> failingListener = (packet, remoteEndPoint) => throw new InvalidOperationException("Expected listener failure.");
            Action<byte[], IPEndPoint> laterListener = (packet, remoteEndPoint) => laterListenerReceivedCount++;
            var hub = FreeDUdpReceiveHub.Acquire(string.Empty, listenPort, false, string.Empty, string.Empty, failingListener, out var firstBindError);
            var sharedHub = FreeDUdpReceiveHub.Acquire(string.Empty, listenPort, false, string.Empty, string.Empty, laterListener, out var secondBindError);

            try
            {
                Assert.That(hub, Is.Not.Null, firstBindError);
                Assert.That(sharedHub, Is.SameAs(hub), secondBindError);
                using (var sender = new UdpClient())
                {
                    sender.Send(new byte[] { 0xD1 }, 1, new IPEndPoint(IPAddress.Loopback, listenPort));
                }

                Exception listenerFailure = null;
                var timeout = Stopwatch.StartNew();
                while (laterListenerReceivedCount == 0 && timeout.ElapsedMilliseconds < 2000)
                {
                    try
                    {
                        hub.Poll();
                    }
                    catch (Exception exception)
                    {
                        listenerFailure = exception;
                    }

                    Thread.Sleep(1);
                }

                Assert.That(listenerFailure, Is.TypeOf<InvalidOperationException>());
                Assert.That(laterListenerReceivedCount, Is.EqualTo(1));
            }
            finally
            {
                hub?.Release(failingListener);
                sharedHub?.Release(laterListener);
            }
        }

        private static int GetAvailableUdpPort()
        {
            using (var listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
            {
                return ((IPEndPoint)listener.Client.LocalEndPoint).Port;
            }
        }
    }
}
