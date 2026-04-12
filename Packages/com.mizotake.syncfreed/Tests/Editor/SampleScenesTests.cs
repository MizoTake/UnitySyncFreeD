using NUnit.Framework;
using System.IO;
using System.Linq;
using MizoTake.SyncFreeD.Editor.Support;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using UnityEditor.SceneManagement;
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
        public void PTZDualDriveSample_AssetExists()
        {
            AssertSceneExists("Assets/Samples/SyncFreeD/PTZDualDriveSample/Scenes/PTZDualDriveSample.unity");
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
            AssertFileExists("Assets/Samples/SyncFreeD/SharedPresets/LocalLoopbackInput.asset");
            AssertFileExists("Assets/Samples/SyncFreeD/SharedPresets/FreeDMulticastInput.asset");
            AssertFileExists("Assets/Samples/SyncFreeD/SharedPresets/DefaultSyncBehaviour.asset");
        }

        [TestCase("Assets/Samples/SyncFreeD/BasicVirtualCamera/Scenes/BasicVirtualCamera.unity", "Main Camera")]
        [TestCase("Assets/Samples/SyncFreeD/ExternalTrackerSample/Scenes/ExternalTrackerSample.unity", "Tracked Camera")]
        [TestCase("Assets/Samples/SyncFreeD/FreeDControllerSample/Scenes/FreeDControllerSample.unity", "FreeD Controller Camera")]
        [TestCase("Assets/Samples/SyncFreeD/OutputInspectorSample/Scenes/OutputInspectorSample.unity", "Output Inspector Camera")]
        [TestCase("Assets/Samples/SyncFreeD/ReplaySample/Scenes/ReplaySample.unity", "Replay Camera")]
        public void SenderSamples_SeparateCameraOutputAndPresetReferences(string scenePath, string cameraName)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var cameraObject = roots.FirstOrDefault(root => root.name == cameraName);
            Assert.That(cameraObject, Is.Not.Null, $"Camera root not found: {scenePath}");
            Assert.That(cameraObject.GetComponent<FreeDUdpOutputBehaviour>(), Is.Null, $"FreeDUdpOutputBehaviour should be separated from camera: {scenePath}");
            Assert.That(cameraObject.GetComponent<SyncFreeDPacketPreviewBehaviour>(), Is.Null, $"Packet preview should be separated from camera: {scenePath}");
            Assert.That(cameraObject.GetComponent<SyncDiagnosticsBehaviour>(), Is.Null, $"Diagnostics should be separated from camera: {scenePath}");
            Assert.That(cameraObject.GetComponent<RecordingOutputBehaviour>(), Is.Null, $"Recording output should be separated from camera: {scenePath}");
            Assert.That(cameraObject.GetComponent<DebugLogOutputBehaviour>(), Is.Null, $"Debug log output should be separated from camera: {scenePath}");

            var syncBehaviour = cameraObject.GetComponent<SyncFreeDBehaviour>();
            Assert.That(syncBehaviour, Is.Not.Null, $"SyncFreeDBehaviour missing on camera: {scenePath}");
            Assert.That(syncBehaviour.ProfileAsset, Is.Not.Null, $"SyncFreeDBehaviour preset missing: {scenePath}");

            var outputRoot = roots.FirstOrDefault(root => root.name == "Sample Output");
            Assert.That(outputRoot, Is.Not.Null, $"Sample Output root missing: {scenePath}");
            var outputBehaviour = outputRoot.GetComponent<FreeDUdpOutputBehaviour>();
            Assert.That(outputBehaviour, Is.Not.Null, $"FreeDUdpOutputBehaviour missing on Sample Output: {scenePath}");
            Assert.That(outputBehaviour.OutputProfileAsset, Is.Not.Null, $"Output preset missing on Sample Output: {scenePath}");

            var debugHudRoot = roots.FirstOrDefault(root => root.name == "Sample Debug HUD");
            Assert.That(debugHudRoot, Is.Not.Null, $"Sample Debug HUD root missing: {scenePath}");
            Assert.That(debugHudRoot.GetComponent<SyncFreeDPacketPreviewBehaviour>(), Is.Not.Null, $"Packet preview missing on Sample Debug HUD: {scenePath}");
        }

        [Test]
        public void FreeDControllerSample_SeparatesLoopbackReceiverFromCamera()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Samples/SyncFreeD/FreeDControllerSample/Scenes/FreeDControllerSample.unity", OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var cameraObject = roots.First(root => root.name == "FreeD Controller Camera");
            Assert.That(cameraObject.GetComponent<FreeDLoopbackReceiverBehaviour>(), Is.Null, "Loopback receiver should not stay on camera.");
            var receiverRoot = roots.FirstOrDefault(root => root.name == "Sample Loopback Receiver");
            Assert.That(receiverRoot, Is.Not.Null, "Sample Loopback Receiver root missing.");
            var receiver = receiverRoot.GetComponent<FreeDLoopbackReceiverBehaviour>();
            Assert.That(receiver, Is.Not.Null, "Loopback receiver missing.");
            Assert.That(receiver.InputProfileAsset, Is.Not.Null, "Loopback receiver input preset missing.");
        }

        [Test]
        public void FreeDReceiveSample_UsesInputPresetAsset()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Samples/SyncFreeD/FreeDReceiveSample/Scenes/FreeDReceiveSample.unity", OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var cameraObject = roots.First(root => root.name == "FreeD Driven Camera");
            Assert.That(cameraObject.GetComponent<FreeDInputSourceBehaviour>(), Is.Null, "FreeD input should be separated from the driven camera.");
            var inputRoot = roots.FirstOrDefault(root => root.name == "Sample Input");
            Assert.That(inputRoot, Is.Not.Null, "Sample Input root missing.");
            var inputSource = inputRoot.GetComponent<FreeDInputSourceBehaviour>();
            Assert.That(inputSource, Is.Not.Null, "FreeDInputSourceBehaviour missing.");
            Assert.That(inputSource.InputProfileAsset, Is.Not.Null, "FreeD input preset missing.");
        }

        [Test]
        public void PTZDualDriveSample_ResolvesSourceAndSupportSnapshot()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Samples/SyncFreeD/PTZDualDriveSample/Scenes/PTZDualDriveSample.unity", OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var cameraObject = roots.First(root => root.name == "PTZ DualDrive Camera");
            var syncBehaviour = cameraObject.GetComponent<SyncFreeDBehaviour>();

            Assert.That(syncBehaviour, Is.Not.Null, "SyncFreeDBehaviour missing on PTZ DualDrive Camera.");
            Assert.That(syncBehaviour.SourceProvider, Is.Not.Null, "PTZ DualDrive sample source is unresolved.");
            Assert.That(syncBehaviour.OutputBehaviour, Is.Not.Null, "PTZ DualDrive sample output is unresolved.");

            var snapshot = SyncFreeDSupportSummary.Build(syncBehaviour);
            Assert.That(snapshot.HasSource, Is.True, "Support summary should detect the dual-drive source.");
            Assert.That(snapshot.HasOutput, Is.True, "Support summary should detect the separated output root.");
            Assert.That(snapshot.HasOutputPreset, Is.True, "Support summary should detect the output preset.");
            Assert.That(snapshot.Tone, Is.EqualTo(SyncFreeDStatusTone.Ready), "PTZ DualDrive sample should be immediately verifiable.");
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
