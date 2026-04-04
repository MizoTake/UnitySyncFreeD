using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Editor.Support;
using MizoTake.SyncFreeD.Networking;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class SyncFreeDDebugSnapshotBuilderTests
    {
        [Test]
        public void Build_IncludesPredictedObservedAndCorrectedColumns()
        {
            var state = new CameraSyncState
            {
                Command = new PoseState { PanDeg = 1d, TiltDeg = 2d, RollDeg = 3d, Xmm = 10d, Ymm = 20d, Zmm = 30d },
                Predicted = new PoseState { PanDeg = 4d, TiltDeg = 5d, RollDeg = 6d, Xmm = 40d, Ymm = 50d, Zmm = 60d },
                Observed = new PoseState { PanDeg = 7d, TiltDeg = 8d, RollDeg = 9d, Xmm = 70d, Ymm = 80d, Zmm = 90d },
                Corrected = new PoseState { PanDeg = 10d, TiltDeg = 11d, RollDeg = 12d, Xmm = 100d, Ymm = 110d, Zmm = 120d },
                CommandLens = new LensState { FocalLengthMm = 31d, FocusDistanceMeters = 1.1d, IrisFNumber = 2.8d },
                PredictedLens = new LensState { FocalLengthMm = 32d, FocusDistanceMeters = 1.2d, IrisFNumber = 2.9d },
                ObservedLens = new LensState { FocalLengthMm = 33d, FocusDistanceMeters = 1.3d, IrisFNumber = 3.0d },
                CorrectedLens = new LensState { FocalLengthMm = 34d, FocusDistanceMeters = 1.4d, IrisFNumber = 3.1d }
            };

            var snapshot = SyncFreeDDebugSnapshotBuilder.Build(state, default, null);

            Assert.That(snapshot.PoseRows.Length, Is.EqualTo(2));
            Assert.That(snapshot.PoseRows[0].Predicted, Does.Contain("4.00"));
            Assert.That(snapshot.PoseRows[0].Observed, Does.Contain("7.00"));
            Assert.That(snapshot.PoseRows[0].Corrected, Does.Contain("10.00"));
            Assert.That(snapshot.LensRows[0].Command, Is.EqualTo("31.00"));
            Assert.That(snapshot.LensRows[0].Predicted, Is.EqualTo("32.00"));
            Assert.That(snapshot.LensRows[0].Observed, Is.EqualTo("33.00"));
            Assert.That(snapshot.LensRows[0].Corrected, Is.EqualTo("34.00"));
        }

        [Test]
        public void Build_FormatsDestinationDiagnosticsLines()
        {
            var lines = SyncFreeDDebugSnapshotBuilder.BuildDestinationLines(new[]
            {
                new FreeDUdpDestinationDiagnostic("127.0.0.1:40000", 0, 11L, true),
                new FreeDUdpDestinationDiagnostic("127.0.0.1:40001", 1, 22L, false)
            });

            Assert.That(lines[0], Does.Contain("127.0.0.1:40000"));
            Assert.That(lines[1], Does.Contain("FAIL"));
        }

        [Test]
        public void BuildMulticastSupport_ReturnsActionNeededWhenJoinIsDisabledAndMultipleNicsExist()
        {
            var snapshot = FreeDUdpMulticastSupportSummary.Build(PacketSendMode.Multicast, string.Empty, string.Empty, false, new[]
            {
                new FreeDUdpNetworkInterfaceInfo("Ethernet0", "192.168.0.10"),
                new FreeDUdpNetworkInterfaceInfo("Ethernet1", "10.0.0.10")
            });

            Assert.That(snapshot.HasActionNeeded, Is.True);
            Assert.That(snapshot.Lines, Has.Some.Contains("Join Multicast Group: disabled"));
            Assert.That(snapshot.Lines, Has.Some.Contains("multiple NICs"));
        }

        [Test]
        public void BuildMulticastSupport_ReturnsSelectedNicWhenBindAndInterfaceAreAligned()
        {
            var snapshot = FreeDUdpMulticastSupportSummary.Build(PacketSendMode.Multicast, "192.168.0.10", "192.168.0.10", true, new[]
            {
                new FreeDUdpNetworkInterfaceInfo("Ethernet0", "192.168.0.10")
            });

            Assert.That(snapshot.HasActionNeeded, Is.False);
            Assert.That(snapshot.Lines, Has.Some.Contains("Multicast Interface: Ethernet0 (192.168.0.10)"));
            Assert.That(snapshot.Lines, Has.Some.Contains("Bind Address: 192.168.0.10"));
        }

        [Test]
        public void BuildMulticastSupport_ReturnsActionNeededWhenBindAddressIsAutoOnMultiNicHost()
        {
            var snapshot = FreeDUdpMulticastSupportSummary.Build(PacketSendMode.Multicast, string.Empty, "192.168.0.10", true, new[]
            {
                new FreeDUdpNetworkInterfaceInfo("Ethernet0", "192.168.0.10"),
                new FreeDUdpNetworkInterfaceInfo("Ethernet1", "10.0.0.10")
            });

            Assert.That(snapshot.HasActionNeeded, Is.True);
            Assert.That(snapshot.Lines, Has.Some.Contains("Bind Address: auto selection with multiple NICs"));
        }

        [Test]
        public void BuildMulticastSupport_ReturnsActionNeededWhenBindAddressIsMissingFromLocalNicList()
        {
            var snapshot = FreeDUdpMulticastSupportSummary.Build(PacketSendMode.Multicast, "172.16.0.10", "192.168.0.10", true, new[]
            {
                new FreeDUdpNetworkInterfaceInfo("Ethernet0", "192.168.0.10")
            });

            Assert.That(snapshot.HasActionNeeded, Is.True);
            Assert.That(snapshot.Lines, Has.Some.Contains("not found locally"));
        }

        [Test]
        public void BuildMulticastSupport_ReturnsActionNeededWhenBindAndInterfaceDiffer()
        {
            var snapshot = FreeDUdpMulticastSupportSummary.Build(PacketSendMode.Multicast, "192.168.0.10", "10.0.0.10", true, new[]
            {
                new FreeDUdpNetworkInterfaceInfo("Ethernet0", "192.168.0.10"),
                new FreeDUdpNetworkInterfaceInfo("Ethernet1", "10.0.0.10")
            });

            Assert.That(snapshot.HasActionNeeded, Is.True);
            Assert.That(snapshot.Lines, Has.Some.Contains("一致していません"));
        }
    }
}
