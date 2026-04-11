using MizoTake.SyncFreeD.ScriptableObjects;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class ProfileAssetTests
    {
        [Test]
        public void SyncTuningProfileAsset_HasDefaultValueInstance()
        {
            var asset = UnityEngine.ScriptableObject.CreateInstance<SyncTuningProfileAsset>();
            Assert.That(asset.Value, Is.Not.Null);
            Assert.That(asset.Value.InquiryIntervalMs, Is.EqualTo(100));
            Assert.That(asset.Value.IdleToSettleDelayMs, Is.EqualTo(150));
            Assert.That(asset.Value.SettleIntervalMs, Is.EqualTo(50));
            Assert.That(asset.Value.SettleTimeoutMs, Is.EqualTo(1000));
            Assert.That(asset.Value.RequiredConsecutiveMatches, Is.EqualTo(3));
        }

        [Test]
        public void FreeDUdpOutputProfileAsset_HasDefaultValueInstance()
        {
            var asset = UnityEngine.ScriptableObject.CreateInstance<FreeDUdpOutputProfileAsset>();
            Assert.That(asset.Value, Is.Not.Null);
            Assert.That(asset.Value.DestinationIpAddress, Is.EqualTo("127.0.0.1"));
        }

        [Test]
        public void FreeDUdpInputProfileAsset_HasDefaultValueInstance()
        {
            var asset = UnityEngine.ScriptableObject.CreateInstance<FreeDUdpInputProfileAsset>();
            Assert.That(asset.Value, Is.Not.Null);
            Assert.That(asset.Value.ListenPort, Is.EqualTo(40000));
            Assert.That(asset.Value.MulticastGroupIpAddress, Is.EqualTo("239.0.0.1"));
        }

        [Test]
        public void FreeDControllerProfileAsset_HasDefaultValueInstance()
        {
            var asset = UnityEngine.ScriptableObject.CreateInstance<FreeDControllerProfileAsset>();
            Assert.That(asset.Value, Is.Not.Null);
            Assert.That(asset.Value.MoveSpeedMetersPerSecond, Is.GreaterThan(0f));
        }

        [Test]
        public void SyncFreeDBehaviourProfileAsset_HasDefaultValueInstance()
        {
            var asset = UnityEngine.ScriptableObject.CreateInstance<SyncFreeDBehaviourProfileAsset>();
            Assert.That(asset.Value, Is.Not.Null);
            Assert.That(asset.Value.SyncMode, Is.EqualTo(MizoTake.SyncFreeD.Core.Models.SyncMode.VirtualMaster));
            Assert.That(asset.Value.OutputTickMode, Is.EqualTo(MizoTake.SyncFreeD.Core.Models.OutputTickMode.LateUpdate));
            Assert.That(asset.Value.Tuning, Is.Not.Null);
        }
    }
}
