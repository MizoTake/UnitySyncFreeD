using System.Collections;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class SyncCorrectionCountPlayModeTests
    {
        [UnityTest]
        public IEnumerator LateUpdate_IncrementsCorrectionCountWhenCorrectionApplied()
        {
            var root = new GameObject("Correction Count Camera");
            var commandTarget = new GameObject("VISCA Command Target");
            var observedTarget = new GameObject("VISCA Observed Target");
            commandTarget.transform.SetParent(root.transform, false);
            observedTarget.transform.SetParent(root.transform, false);
            commandTarget.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            observedTarget.transform.localRotation = Quaternion.Euler(0f, 20f, 0f);
            root.AddComponent<Camera>();
            root.AddComponent<AudioListener>();
            root.AddComponent<ViscaCameraSourceBehaviour>();
            root.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = root.AddComponent<SyncFreeDBehaviour>();
            SetPrivateField(sync, "syncMode", SyncMode.DualDrive);

            yield return null;

            Assert.That(sync.CorrectionAppliedCount, Is.GreaterThan(0));

            Object.Destroy(root);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
