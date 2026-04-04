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
        public void FreeDControllerProfileAsset_HasDefaultValueInstance()
        {
            var asset = UnityEngine.ScriptableObject.CreateInstance<FreeDControllerProfileAsset>();
            Assert.That(asset.Value, Is.Not.Null);
            Assert.That(asset.Value.MoveSpeedMetersPerSecond, Is.GreaterThan(0f));
        }
    }
}
