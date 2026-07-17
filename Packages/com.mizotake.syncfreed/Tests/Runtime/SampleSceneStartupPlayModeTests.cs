using System;
using System.Collections;
using System.IO;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    [PrebuildSetup(typeof(PackageSampleImportSetup))]
    [PostBuildCleanup(typeof(PackageSampleImportSetup))]
    public sealed class SampleSceneStartupPlayModeTests
    {
        private const string ImportedSamplesRoot = "Assets/__SyncFreeDPackageSampleVerification";
        private static readonly string[] SampleScenePaths =
        {
            ImportedSamplesRoot + "/BasicVirtualCamera/Scenes/BasicVirtualCamera.unity",
            ImportedSamplesRoot + "/ReplaySample/Scenes/ReplaySample.unity",
            ImportedSamplesRoot + "/ExternalTrackerSample/Scenes/ExternalTrackerSample.unity",
            ImportedSamplesRoot + "/OutputInspectorSample/Scenes/OutputInspectorSample.unity",
            ImportedSamplesRoot + "/FreeDControllerSample/Scenes/FreeDControllerSample.unity",
            ImportedSamplesRoot + "/FreeDReceiveSample/Scenes/FreeDReceiveSample.unity"
        };

        [UnityTest]
        public IEnumerator ImportedPackageSamples_OpenAndInitializeCoreBehaviours()
        {
            foreach (var scenePath in SampleScenePaths)
            {
                yield return LoadScene(scenePath);
                yield return null;
                yield return null;
                Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(scenePath));
                Assert.That(SceneManager.GetActiveScene().rootCount, Is.GreaterThan(0), $"Scene root count was zero: {scenePath}");
                var syncBehaviours = UnityEngine.Object.FindObjectsByType<SyncFreeDBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (scenePath.Contains("FreeDControllerSample"))
                {
                    Assert.That(UnityEngine.Object.FindFirstObjectByType<FreeDControllerBehaviour>(), Is.Not.Null, $"FreeDControllerBehaviour not found: {scenePath}");
                }
                else if (scenePath.Contains("FreeDReceiveSample"))
                {
                    Assert.That(UnityEngine.Object.FindFirstObjectByType<FreeDDrivenCameraBehaviour>(), Is.Not.Null, $"FreeDDrivenCameraBehaviour not found: {scenePath}");
                    Assert.That(UnityEngine.Object.FindFirstObjectByType<FreeDInputSourceBehaviour>(), Is.Not.Null, $"FreeDInputSourceBehaviour not found: {scenePath}");
                }
                else
                {
                    Assert.That(syncBehaviours.Length, Is.GreaterThan(0), $"SyncFreeDBehaviour not found: {scenePath}");
                }

                foreach (var behaviour in syncBehaviours)
                {
                    Assert.That(behaviour.ManualTick(), Is.True, $"ManualTick failed: {scenePath}");
                }
            }
        }

        private static IEnumerator LoadScene(string scenePath)
        {
            var editorSceneManagerType = Type.GetType("UnityEditor.SceneManagement.EditorSceneManager, UnityEditor");
            Assert.That(editorSceneManagerType, Is.Not.Null, "EditorSceneManager not found.");
            var method = editorSceneManagerType.GetMethod("LoadSceneAsyncInPlayMode", new[] { typeof(string), typeof(LoadSceneParameters) });
            if (method == null)
            {
                method = editorSceneManagerType.GetMethod("LoadSceneInPlayMode", new[] { typeof(string), typeof(LoadSceneParameters) });
            }

            Assert.That(method, Is.Not.Null, "LoadSceneAsyncInPlayMode / LoadSceneInPlayMode not found.");
            var result = method.Invoke(null, new object[] { scenePath, new LoadSceneParameters(LoadSceneMode.Single) });
            var operation = result as AsyncOperation;
            if (operation == null)
            {
                yield return null;
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }
    }

    public sealed class PackageSampleImportSetup : IPrebuildSetup, IPostBuildCleanup
    {
        private const string ImportedSamplesRoot = "Assets/__SyncFreeDPackageSampleVerification";

        public void Setup()
        {
#if UNITY_EDITOR
            CleanupImportedSamples();
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var packageSamplesRoot = Path.Combine(projectRoot, "Packages", "com.mizotake.syncfreed", "Samples~");
            var importedSamplesRoot = Path.Combine(projectRoot, ImportedSamplesRoot.Replace('/', Path.DirectorySeparatorChar));
            CopyDirectory(packageSamplesRoot, importedSamplesRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            CleanupImportedSamples();
#endif
        }

#if UNITY_EDITOR
        private static void CleanupImportedSamples()
        {
            if (AssetDatabase.IsValidFolder(ImportedSamplesRoot))
            {
                AssetDatabase.DeleteAsset(ImportedSamplesRoot);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

        private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            Directory.CreateDirectory(destinationDirectory);
            foreach (var sourceFile in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                if (sourceFile.EndsWith(".unity.meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var relativePath = sourceFile.Substring(sourceDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var destinationFile = Path.Combine(destinationDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                File.Copy(sourceFile, destinationFile, true);
            }
        }
#endif
    }
}
