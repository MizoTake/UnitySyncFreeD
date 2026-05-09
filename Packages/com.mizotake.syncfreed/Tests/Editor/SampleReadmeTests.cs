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
            AssertReadmeContains("Packages/com.mizotake.syncfreed/Samples~/BasicVirtualCamera/README.md", "Canonical State", "Free-D D1", "UDP", "Main Camera");
        }

        [Test]
        public void ReplayReadme_DescribesReplayFlow()
        {
            AssertReadmeContains("Packages/com.mizotake.syncfreed/Samples~/ReplaySample/README.md", "Replay", "JSON", "CSV", "frame step", "Free-D");
        }

        [Test]
        public void ExternalTrackerReadme_DescribesTrackerFlow()
        {
            AssertReadmeContains("Packages/com.mizotake.syncfreed/Samples~/ExternalTrackerSample/README.md", "Tracker", "Canonical State", "Free-D");
        }

        [Test]
        public void OutputInspectorReadme_DescribesInspectionFlow()
        {
            AssertReadmeContains("Packages/com.mizotake.syncfreed/Samples~/OutputInspectorSample/README.md", "loopback", "checksum", "network warning", "recording");
        }

        [Test]
        public void FreeDControllerReadme_DescribesControllerFlow()
        {
            AssertReadmeContains("Packages/com.mizotake.syncfreed/Samples~/FreeDControllerSample/README.md", "controller", "Arrow Keys", "loopback");
        }

        [Test]
        public void FreeDReceiveReadme_DescribesReceiveFlow()
        {
            AssertReadmeContains("Packages/com.mizotake.syncfreed/Samples~/FreeDReceiveSample/README.md", "multicast", "CG camera", "focal length", "focus distance", "FreeDInputSourceBehaviour", "Camera ID", "同じ UDP port");
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
