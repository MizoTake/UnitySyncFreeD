using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class DocumentationConsistencyTests
    {
        [Test]
        public void Spec_DeclaresViscaOutOfScope()
        {
            var content = ReadProjectFile("SyncFreeD_spec.md");
            Assert.That(content, Does.Contain("VISCA 実装は本 package の対象外"));
        }

        [Test]
        public void Spec_ListsMaintainedFreeDSamples()
        {
            var content = ReadProjectFile("SyncFreeD_spec.md");
            Assert.That(content, Does.Contain("### 21.1 BasicVirtualCamera"));
            Assert.That(content, Does.Contain("### 21.2 ExternalTrackerSample"));
            Assert.That(content, Does.Contain("### 21.3 ReplaySample"));
            Assert.That(content, Does.Contain("### 21.4 OutputInspectorSample"));
            Assert.That(content, Does.Contain("### 21.5 FreeDControllerSample"));
            Assert.That(content, Does.Not.Contain("PTZDualDriveSample"));
        }

        [Test]
        public void PackageReadme_ListsMaintainedFreeDSamples()
        {
            var content = ReadProjectFile("Packages/com.mizotake.syncfreed/README.md");
            Assert.That(content, Does.Contain("BasicVirtualCamera"));
            Assert.That(content, Does.Contain("ExternalTrackerSample"));
            Assert.That(content, Does.Contain("ReplaySample"));
            Assert.That(content, Does.Contain("OutputInspectorSample"));
            Assert.That(content, Does.Contain("FreeDControllerSample"));
            Assert.That(content, Does.Contain("Sample Visual Rig"));
            Assert.That(content, Does.Not.Contain("PTZDualDriveSample"));
        }

        [Test]
        public void SamplesDocumentation_DescribesSharedVisualRig()
        {
            var content = ReadProjectFile("Packages/com.mizotake.syncfreed/Documentation~/Samples.md");
            Assert.That(content, Does.Contain("## Sample Visual Rig"));
            Assert.That(content, Does.Contain("Yellow: `Center Tower` / `Depth Pole`"));
            Assert.That(content, Does.Contain("White: `Center Line` / `Cross Line`"));
        }

        private static string ReadProjectFile(string relativePath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var fullPath = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.That(File.Exists(fullPath), Is.True, $"Missing file: {fullPath}");
            return File.ReadAllText(fullPath);
        }
    }
}
