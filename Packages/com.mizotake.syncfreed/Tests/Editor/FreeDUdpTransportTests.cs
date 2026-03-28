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
        public void Constructor_AppliesBindAddressAndSocketBufferSize()
        {
            using (var transport = new FreeDUdpTransport("127.0.0.1", 8192))
            {
                Assert.That(transport.BoundAddress, Is.EqualTo("127.0.0.1"));
                Assert.That(transport.ConfiguredSocketBufferSize, Is.EqualTo(8192));
            }
        }
    }
}
