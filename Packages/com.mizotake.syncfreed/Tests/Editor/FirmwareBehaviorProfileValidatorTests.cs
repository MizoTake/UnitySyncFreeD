using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Networking;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class FirmwareBehaviorProfileValidatorTests
    {
        [Test]
        public void Validate_ReturnsEmpty_WhenProfileIsNull()
        {
            var warning = FirmwareBehaviorProfileValidator.Validate(null, PacketSendMode.Multicast);

            Assert.That(warning, Is.Empty);
        }

        [Test]
        public void Validate_ReturnsWarning_WhenMulticastIsNotSupported()
        {
            var warning = FirmwareBehaviorProfileValidator.Validate(new FirmwareBehaviorProfile { SupportsMulticast = false }, PacketSendMode.Multicast);

            Assert.That(warning, Does.Contain("Multicast"));
        }

        [Test]
        public void Validate_ReturnsWarning_WhenMultiDestinationUnicastIsNotSupported()
        {
            var warning = FirmwareBehaviorProfileValidator.Validate(new FirmwareBehaviorProfile { SupportsMultiUnicast = false }, PacketSendMode.MultiDestinationUnicast);

            Assert.That(warning, Does.Contain("Multi Destination Unicast"));
        }

        [Test]
        public void Validate_ReturnsWarning_WhenMaxUnicastDestinationCountIsOne()
        {
            var warning = FirmwareBehaviorProfileValidator.Validate(new FirmwareBehaviorProfile { SupportsMultiUnicast = true, MaxUnicastDestinationCount = 1 }, PacketSendMode.MultiDestinationUnicast);

            Assert.That(warning, Does.Contain("1 送信先"));
        }
    }
}
