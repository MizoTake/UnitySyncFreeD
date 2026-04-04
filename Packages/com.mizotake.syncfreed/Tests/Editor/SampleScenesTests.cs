using NUnit.Framework;
using System.IO;
using System.Linq;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using UnityEngine;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class SampleScenesTests
    {
        [Test]
        public void BasicVirtualCameraSample_AssetExists()
        {
            AssertSceneExists("Assets/Samples/SyncFreeD/BasicVirtualCamera/Scenes/BasicVirtualCamera.unity");
        }

        [Test]
        public void ReplaySample_AssetExists()
        {
            AssertSceneExists("Assets/Samples/SyncFreeD/ReplaySample/Scenes/ReplaySample.unity");
        }

        [Test]
        public void ExternalTrackerSample_AssetExists()
        {
            AssertSceneExists("Assets/Samples/SyncFreeD/ExternalTrackerSample/Scenes/ExternalTrackerSample.unity");
        }

        [Test]
        public void OutputInspectorSample_AssetExists()
        {
            AssertSceneExists("Assets/Samples/SyncFreeD/OutputInspectorSample/Scenes/OutputInspectorSample.unity");
        }

        [Test]
        public void FreeDControllerSample_AssetExists()
        {
            AssertSceneExists("Assets/Samples/SyncFreeD/FreeDControllerSample/Scenes/FreeDControllerSample.unity");
        }

        [Test]
        public void FreeDReceiveSample_AssetExists()
        {
            AssertSceneExists("Assets/Samples/SyncFreeD/FreeDReceiveSample/Scenes/FreeDReceiveSample.unity");
        }

        [Test]
        public void PackageSampleScenes_ContainVisualReferenceRig()
        {
            AssertFileContains("Packages/com.mizotake.syncfreed/Samples~/BasicVirtualCamera/Scenes/BasicVirtualCamera.unity", "m_Name: Sample Visual Rig");
            AssertFileContains("Packages/com.mizotake.syncfreed/Samples~/ExternalTrackerSample/Scenes/ExternalTrackerSample.unity", "m_Name: Sample Visual Rig");
            AssertFileContains("Packages/com.mizotake.syncfreed/Samples~/FreeDControllerSample/Scenes/FreeDControllerSample.unity", "m_Name: Sample Visual Rig");
            AssertFileContains("Packages/com.mizotake.syncfreed/Samples~/FreeDReceiveSample/Scenes/FreeDReceiveSample.unity", "m_Name: Sample Visual Rig");
            AssertFileContains("Packages/com.mizotake.syncfreed/Samples~/OutputInspectorSample/Scenes/OutputInspectorSample.unity", "m_Name: Sample Visual Rig");
            AssertFileContains("Packages/com.mizotake.syncfreed/Samples~/ReplaySample/Scenes/ReplaySample.unity", "m_Name: Sample Visual Rig");
        }

        [Test]
        public void SampleReadmes_DescribeVisualReferenceRigChecks()
        {
            AssertFileContains("Packages/com.mizotake.syncfreed/Samples~/BasicVirtualCamera/README.md", "Sample Visual Rig");
            AssertFileContains("Packages/com.mizotake.syncfreed/Samples~/ExternalTrackerSample/README.md", "Sample Visual Rig");
            AssertFileContains("Packages/com.mizotake.syncfreed/Samples~/FreeDControllerSample/README.md", "Sample Visual Rig");
            AssertFileContains("Packages/com.mizotake.syncfreed/Samples~/FreeDReceiveSample/README.md", "Sample Visual Rig");
            AssertFileContains("Packages/com.mizotake.syncfreed/Samples~/OutputInspectorSample/README.md", "Sample Visual Rig");
            AssertFileContains("Packages/com.mizotake.syncfreed/Samples~/ReplaySample/README.md", "Sample Visual Rig");
        }

        [Test]
        public void SampleVisualReferenceRigBehaviour_CreatesMarkersOnEnable()
        {
            var gameObject = new GameObject("Rig Root");
            try
            {
                var behaviour = gameObject.AddComponent<SampleVisualReferenceRigBehaviour>();
                behaviour.EnsureVisuals();
                Assert.That(behaviour.transform.childCount, Is.EqualTo(9));
                Assert.That(behaviour.transform.Find("Ground"), Is.Not.Null);
                Assert.That(behaviour.transform.Find("Center Tower"), Is.Not.Null);
                Assert.That(behaviour.transform.Find("Depth Pole"), Is.Not.Null);
                Assert.That(behaviour.transform.Find("Center Line"), Is.Not.Null);
                Assert.That(behaviour.transform.Find("Cross Line"), Is.Not.Null);
                Assert.That(behaviour.transform.Find("Near Target").localPosition, Is.EqualTo(new Vector3(-1.5f, 1f, 6f)));
                Assert.That(behaviour.transform.Find("Far Target").GetComponent<Renderer>(), Is.Not.Null);
                Assert.That(behaviour.transform.Find("Center Tower/Center Tower Label"), Is.Not.Null);
                Assert.That(behaviour.transform.Find("Near Target/Near Target Label").GetComponent<TextMesh>().text, Is.EqualTo("Near Target"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SharedPresetAssets_Exist()
        {
            AssertFileExists("Assets/Samples/SyncFreeD/SharedPresets/LocalLoopbackOutput.asset");
            AssertFileExists("Assets/Samples/SyncFreeD/SharedPresets/ComfortController.asset");
        }

        [Test]
        public void PackageSamples_AreLimitedToMaintainedFreeDSamples()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var packageSamplesPath = Path.Combine(projectRoot, "Packages", "com.mizotake.syncfreed", "Samples~");
            var directories = Directory.GetDirectories(packageSamplesPath).Select(Path.GetFileName).OrderBy(name => name).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "BasicVirtualCamera",
                "ExternalTrackerSample",
                "FreeDControllerSample",
                "FreeDReceiveSample",
                "OutputInspectorSample",
                "ReplaySample"
            }, directories);
        }

        private static void AssertSceneExists(string scenePath)
        {
            AssertFileExists(scenePath);
        }

        private static void AssertFileExists(string relativePath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var fullPath = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.That(File.Exists(fullPath), Is.True, $"Missing asset: {fullPath}");
        }

        private static void AssertFileContains(string relativePath, string expectedText)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var fullPath = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.That(File.Exists(fullPath), Is.True, $"Missing asset: {fullPath}");
            Assert.That(File.ReadAllText(fullPath), Does.Contain(expectedText), $"Missing text '{expectedText}' in {fullPath}");
        }
    }
}
