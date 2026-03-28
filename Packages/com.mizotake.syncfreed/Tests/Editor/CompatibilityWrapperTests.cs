using MizoTake.SyncFreeD.Core.Models;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
#pragma warning disable CS0618
    public sealed class CompatibilityWrapperTests
    {
        [Test]
        public void LegacyOutputWrapper_IsAssignableToBehaviourOutput()
        {
            var wrapperType = typeof(UnityAdapters.Outputs.FreeDUdpOutputBehaviour);
            Assert.That(typeof(UnityAdapters.Behaviours.FreeDUdpOutputBehaviour).IsAssignableFrom(wrapperType), Is.True);
        }

        [Test]
        public void LegacySourceWrapper_ImplementsCameraSourceContract()
        {
            var wrapperType = typeof(UnityAdapters.Sources.UnityCameraSourceBehaviour);
            Assert.That(typeof(Core.Abstractions.ICameraSource).IsAssignableFrom(wrapperType), Is.True);
        }
    }
#pragma warning restore CS0618
}
