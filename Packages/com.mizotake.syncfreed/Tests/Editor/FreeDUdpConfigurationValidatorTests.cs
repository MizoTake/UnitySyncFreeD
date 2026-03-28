using MizoTake.SyncFreeD.Networking;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class FreeDUdpConfigurationValidatorTests
    {
        [Test]
        public void Validate_ReturnsWarningWhenMulticastGroupIsNotMulticastRange()
        {
            var warning = FreeDUdpConfigurationValidator.Validate(string.Empty, "127.0.0.1", string.Empty, PacketSendMode.Multicast);

            Assert.That(warning, Does.Contain("224.0.0.0 - 239.255.255.255"));
        }

        [Test]
        public void Validate_ReturnsWarningWhenBindAddressIsInvalid()
        {
            var warning = FreeDUdpConfigurationValidator.Validate("invalid-address", "239.0.0.1", string.Empty, PacketSendMode.SingleDestinationUnicast);

            Assert.That(warning, Does.Contain("Bind Address"));
        }
    }
}
