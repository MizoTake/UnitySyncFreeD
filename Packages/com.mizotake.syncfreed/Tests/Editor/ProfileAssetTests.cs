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
        }
    }
}
