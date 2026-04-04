using NUnit.Framework;
using System.IO;
using UnityEngine;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class SampleReadmeTests
    {
        [Test]
        public void BasicVirtualCameraReadme_DescribesCoreFlow()
        {
            AssertReadmeContains("Assets/Samples/SyncFreeD/BasicVirtualCamera/README.md", "Canonical State", "Free-D D1", "UDP");
        }

        [Test]
        public void ReplayReadme_DescribesReplayFlow()
        {
            AssertReadmeContains("Assets/Samples/SyncFreeD/ReplaySample/README.md", "Replay", "JSON", "CSV", "frame step", "Free-D");
        }

        [Test]
        public void ExternalTrackerReadme_DescribesTrackerFlow()
        {
            AssertReadmeContains("Assets/Samples/SyncFreeD/ExternalTrackerSample/README.md", "Tracker", "Canonical State", "Free-D");
        }

        [Test]
        public void OutputInspectorReadme_DescribesInspectionFlow()
        {
            AssertReadmeContains("Assets/Samples/SyncFreeD/OutputInspectorSample/README.md", "loopback", "checksum", "network warning");
        }

        [Test]
        public void FreeDControllerReadme_DescribesControllerFlow()
        {
            AssertReadmeContains("Assets/Samples/SyncFreeD/FreeDControllerSample/README.md", "controller", "Arrow Keys", "loopback");
        }

        [Test]
        public void FreeDReceiveReadme_DescribesReceiveFlow()
        {
            AssertReadmeContains("Assets/Samples/SyncFreeD/FreeDReceiveSample/README.md", "multicast", "CG camera", "focal length", "focus distance");
        }

        private static void AssertReadmeContains(string relativePath, params string[] expectedTokens)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var fullPath = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.That(File.Exists(fullPath), Is.True, $"Missing readme: {fullPath}");
            var content = File.ReadAllText(fullPath);
            for (var i = 0; i < expectedTokens.Length; i++)
            {
                Assert.That(content, Does.Contain(expectedTokens[i]), $"Missing token '{expectedTokens[i]}' in {fullPath}");
            }
        }
    }
}
