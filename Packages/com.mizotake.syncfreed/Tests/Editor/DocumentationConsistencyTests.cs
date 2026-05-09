using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class DocumentationConsistencyTests
    {
        [Test]
        public void Overview_DeclaresViscaOutOfScope()
        {
            var content = ReadProjectFile("Packages/com.mizotake.syncfreed/Documentation~/Overview.md");
            Assert.That(content, Does.Contain("VISCA 実装は本 package の対象外"));
        }

        [Test]
        public void Overview_ListsMaintainedFreeDSamples()
        {
            var content = ReadProjectFile("Packages/com.mizotake.syncfreed/Documentation~/Overview.md");
            Assert.That(content, Does.Contain("BasicVirtualCamera"));
            Assert.That(content, Does.Contain("ExternalTrackerSample"));
            Assert.That(content, Does.Contain("ReplaySample"));
            Assert.That(content, Does.Contain("OutputInspectorSample"));
            Assert.That(content, Does.Contain("FreeDControllerSample"));
            Assert.That(content, Does.Contain("FreeDReceiveSample"));
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
            Assert.That(content, Does.Contain("FreeDReceiveSample"));
            Assert.That(content, Does.Contain("Sample Visual Rig"));
            Assert.That(content, Does.Not.Contain("PTZDualDriveSample"));
        }

        [Test]
        public void SamplesDocumentation_DescribesSharedVisualRig()
        {
            var content = ReadProjectFile("Packages/com.mizotake.syncfreed/Documentation~/Overview.md");
            Assert.That(content, Does.Contain("## Samples"));
            Assert.That(content, Does.Contain("Sample Visual Rig"));
            Assert.That(content, Does.Contain("Center Tower"));
            Assert.That(content, Does.Contain("Cross Line"));
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
