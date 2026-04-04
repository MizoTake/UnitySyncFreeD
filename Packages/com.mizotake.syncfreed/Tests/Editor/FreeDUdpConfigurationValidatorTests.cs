using MizoTake.SyncFreeD.Networking;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class FreeDUdpConfigurationValidatorTests
    {
        [Test]
        public void Validate_ReturnsWarningWhenMulticastGroupIsNotMulticastRange()
        {
            var warning = FreeDUdpConfigurationValidator.Validate(PacketSendMode.Multicast, string.Empty, "127.0.0.1", 40000, null, "127.0.0.1", 40000, string.Empty, 0, -1);

            Assert.That(warning, Does.Contain("224.0.0.0 - 239.255.255.255"));
        }

        [Test]
        public void Validate_ReturnsWarningWhenBindAddressIsInvalid()
        {
            var warning = FreeDUdpConfigurationValidator.Validate(PacketSendMode.SingleDestinationUnicast, "invalid-address", "127.0.0.1", 40000, null, "239.0.0.1", 40000, string.Empty, 0, -1);

            Assert.That(warning, Does.Contain("Bind Address"));
        }

        [Test]
        public void Validate_ReturnsWarningWhenDestinationPortIsOutOfRange()
        {
            var warning = FreeDUdpConfigurationValidator.Validate(PacketSendMode.SingleDestinationUnicast, string.Empty, "127.0.0.1", 70000, null, "239.0.0.1", 40000, string.Empty, 0, -1);

            Assert.That(warning, Does.Contain("Destination Port"));
        }

        [Test]
        public void Validate_ReturnsWarningWhenAdditionalDestinationIsInvalid()
        {
            var warning = FreeDUdpConfigurationValidator.Validate(PacketSendMode.MultiDestinationUnicast, string.Empty, "127.0.0.1", 40000, new[] { new FreeDUdpDestination { Enabled = true, IpAddress = "invalid-address", Port = 40001 } }, "239.0.0.1", 40000, string.Empty, 0, -1);

            Assert.That(warning, Does.Contain("Additional Destination 1"));
        }

        [Test]
        public void Validate_ReturnsWarningWhenCameraIdFilterIsOutOfRange()
        {
            var warning = FreeDUdpConfigurationValidator.Validate(PacketSendMode.SingleDestinationUnicast, string.Empty, "127.0.0.1", 40000, null, "239.0.0.1", 40000, string.Empty, 0, 999);

            Assert.That(warning, Does.Contain("Camera ID Filter"));
        }

        [Test]
        public void Validate_ReturnsWarningWhenMulticastInterfaceIsNotSelectedOnMultiNicHost()
        {
            var warning = FreeDUdpConfigurationValidator.Validate(PacketSendMode.Multicast, string.Empty, "127.0.0.1", 40000, null, "239.0.0.1", 40000, string.Empty, 0, -1, new[]
            {
                new FreeDUdpNetworkInterfaceInfo("Ethernet0", "192.168.0.10"),
                new FreeDUdpNetworkInterfaceInfo("Ethernet1", "10.0.0.10")
            });

            Assert.That(warning, Does.Contain("Multicast Interface Address"));
        }

        [Test]
        public void Validate_ReturnsWarningWhenBindAddressIsNotSelectedOnMultiNicHost()
        {
            var warning = FreeDUdpConfigurationValidator.Validate(PacketSendMode.Multicast, string.Empty, "127.0.0.1", 40000, null, "239.0.0.1", 40000, "192.168.0.10", 0, -1, new[]
            {
                new FreeDUdpNetworkInterfaceInfo("Ethernet0", "192.168.0.10"),
                new FreeDUdpNetworkInterfaceInfo("Ethernet1", "10.0.0.10")
            });

            Assert.That(warning, Does.Contain("Bind Address"));
        }

        [Test]
        public void Validate_ReturnsWarningWhenBindAndMulticastInterfaceDiffer()
        {
            var warning = FreeDUdpConfigurationValidator.Validate(PacketSendMode.Multicast, "192.168.0.10", "127.0.0.1", 40000, null, "239.0.0.1", 40000, "10.0.0.10", 0, -1, new[]
            {
                new FreeDUdpNetworkInterfaceInfo("Ethernet0", "192.168.0.10"),
                new FreeDUdpNetworkInterfaceInfo("Ethernet1", "10.0.0.10")
            });

            Assert.That(warning, Does.Contain("同じ NIC"));
        }
    }
}
