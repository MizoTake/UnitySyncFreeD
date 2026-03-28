using NUnit.Framework;
using System.IO;
using UnityEngine;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class SampleScenesTests
    {
        [Test]
        public void BasicVirtualCameraSample_AssetExists()
        {
            AssertSceneExists("Packages/com.mizotake.syncfreed/Samples~/BasicVirtualCamera/Scenes/BasicVirtualCamera.unity");
        }

        [Test]
        public void PTZDualDriveSample_AssetExists()
        {
            AssertSceneExists("Packages/com.mizotake.syncfreed/Samples~/PTZDualDriveSample/Scenes/PTZDualDriveSample.unity");
        }

        [Test]
        public void ReplaySample_AssetExists()
        {
            AssertSceneExists("Packages/com.mizotake.syncfreed/Samples~/ReplaySample/Scenes/ReplaySample.unity");
        }

        [Test]
        public void ExternalTrackerSample_AssetExists()
        {
            AssertSceneExists("Packages/com.mizotake.syncfreed/Samples~/ExternalTrackerSample/Scenes/ExternalTrackerSample.unity");
        }

        [Test]
        public void OutputInspectorSample_AssetExists()
        {
            AssertSceneExists("Packages/com.mizotake.syncfreed/Samples~/OutputInspectorSample/Scenes/OutputInspectorSample.unity");
        }

        private static void AssertSceneExists(string scenePath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var fullPath = Path.Combine(projectRoot, scenePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.That(File.Exists(fullPath), Is.True, $"Missing scene asset: {fullPath}");
        }
    }
}
