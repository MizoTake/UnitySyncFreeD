using System.Collections;
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
            cameraObject.AddComponent<SyncFreeDBehaviour>();
            SetPrivateField(output, "cameraIdFilter", 1);

            yield return null;
            yield return null;

            Assert.That(output.LastSendSkippedByFilter, Is.True);
            Assert.That(output.LastSendSuccessCount, Is.EqualTo(0));

            Object.Destroy(cameraObject);
            LogAssert.ignoreFailingMessages = false;
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
