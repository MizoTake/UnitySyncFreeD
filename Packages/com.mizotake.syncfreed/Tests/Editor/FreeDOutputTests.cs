using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Outputs;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class FreeDOutputTests
    {
        [Test]
        public void Send_BuildsPacketAndInvokesTransport()
        {
            byte[] sentPacket = null;
            var output = new FreeDOutput(packet => sentPacket = packet);
            output.Send(new CameraSyncState { CameraId = 1, Corrected = new PoseState(), Lens = new LensState() });

            Assert.That(sentPacket, Is.Not.Null);
            Assert.That(sentPacket.Length, Is.EqualTo(FreeDPacketBuilder.PacketLength));
            Assert.That(output.LastPacket.Length, Is.EqualTo(FreeDPacketBuilder.PacketLength));
        }
    }
}
