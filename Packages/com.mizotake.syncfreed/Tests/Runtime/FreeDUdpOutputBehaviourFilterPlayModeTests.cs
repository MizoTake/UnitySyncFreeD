using System.Collections;
using MizoTake.SyncFreeD.ScriptableObjects;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class FreeDUdpOutputBehaviourFilterPlayModeTests
    {
        [UnityTest]
        public IEnumerator LateUpdate_SkipsSendWhenCameraIdFilterDoesNotMatch()
        {
            LogAssert.ignoreFailingMessages = true;
            var cameraObject = new GameObject("Filtered Output Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var profile = ScriptableObject.CreateInstance<FreeDUdpOutputProfileAsset>();
            profile.Value.CameraIdFilter = 1;
            output.SetOutputProfileAsset(profile, true);
            cameraObject.AddComponent<SyncFreeDBehaviour>();

            yield return null;
            yield return null;

            Assert.That(output.LastSendSkippedByFilter, Is.True);
            Assert.That(output.LastSendSuccessCount, Is.EqualTo(0));

            Object.Destroy(profile);
            Object.Destroy(cameraObject);
            LogAssert.ignoreFailingMessages = false;
        }
    }
}
