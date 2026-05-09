using MizoTake.SyncFreeD.Core.Models;
using NUnit.Framework;
using System.IO;
using UnityEngine;

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

        [Test]
        public void FreeDPacketBuilder_ImplementsCanonicalAndLegacyPacketBuilderContracts()
        {
            var builderType = typeof(Core.Outputs.FreeDPacketBuilder);
            Assert.That(typeof(Core.Abstractions.IFreeDPacketBuilder).IsAssignableFrom(builderType), Is.True);
            Assert.That(typeof(Core.Outputs.IFreeDPacketBuilder).IsAssignableFrom(builderType), Is.True);
        }

        [Test]
        public void CoreAssemblyDefinition_IsUnityIndependentAndReferencedByRuntimeAssembly()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var coreAsmdef = File.ReadAllText(Path.Combine(projectRoot, "Packages", "com.mizotake.syncfreed", "Runtime", "Core", "com.mizotake.syncfreed.core.asmdef"));
            var runtimeAsmdef = File.ReadAllText(Path.Combine(projectRoot, "Packages", "com.mizotake.syncfreed", "Runtime", "com.mizotake.syncfreed.asmdef"));

            Assert.That(coreAsmdef, Does.Contain("\"noEngineReferences\": true"));
            Assert.That(runtimeAsmdef, Does.Contain("\"com.mizotake.syncfreed.core\""));
        }
    }
#pragma warning restore CS0618
}
