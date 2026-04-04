using System;
using System.Collections;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class SampleSceneStartupPlayModeTests
    {
        private static readonly string[] SampleScenePaths =
        {
            "Assets/Samples/SyncFreeD/BasicVirtualCamera/Scenes/BasicVirtualCamera.unity",
            "Assets/Samples/SyncFreeD/ReplaySample/Scenes/ReplaySample.unity",
            "Assets/Samples/SyncFreeD/ExternalTrackerSample/Scenes/ExternalTrackerSample.unity",
            "Assets/Samples/SyncFreeD/OutputInspectorSample/Scenes/OutputInspectorSample.unity",
            "Assets/Samples/SyncFreeD/FreeDControllerSample/Scenes/FreeDControllerSample.unity",
            "Assets/Samples/SyncFreeD/FreeDReceiveSample/Scenes/FreeDReceiveSample.unity"
        };

        [UnityTest]
        public IEnumerator AssetsSamples_OpenAndInitializeCoreBehaviours()
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
}
