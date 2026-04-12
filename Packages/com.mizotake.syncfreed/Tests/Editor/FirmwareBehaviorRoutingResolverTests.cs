using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Networking;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class FirmwareBehaviorRoutingResolverTests
    {
        [Test]
        public void ResolveSendMode_FallsBackToSingleDestination_WhenMultiDestinationIsUnsupported()
        {
            var resolved = FirmwareBehaviorRoutingResolver.ResolveSendMode(PacketSendMode.MultiDestinationUnicast, new FirmwareBehaviorProfile
            {
                SupportsMultiUnicast = false,
                MaxUnicastDestinationCount = 1
            });

            Assert.That(resolved, Is.EqualTo(PacketSendMode.SingleDestinationUnicast));
        }

        [Test]
        public void ResolveSendMode_FallsBackFromMulticastToMultiDestinationUnicast_WhenSupported()
        {
            var resolved = FirmwareBehaviorRoutingResolver.ResolveSendMode(PacketSendMode.Multicast, new FirmwareBehaviorProfile
            {
                SupportsMulticast = false,
                SupportsMultiUnicast = true,
                MaxUnicastDestinationCount = 4
            });

            Assert.That(resolved, Is.EqualTo(PacketSendMode.MultiDestinationUnicast));
        }

        [Test]
        public void ResolveAdditionalDestinationLimit_UsesFirmwareMaxUnicastDestinationCount()
        {
            var limit = FirmwareBehaviorRoutingResolver.ResolveAdditionalDestinationLimit(PacketSendMode.MultiDestinationUnicast, new FirmwareBehaviorProfile
            {
                SupportsMultiUnicast = true,
                MaxUnicastDestinationCount = 2
            });

            Assert.That(limit, Is.EqualTo(1));
        }
    }
}
