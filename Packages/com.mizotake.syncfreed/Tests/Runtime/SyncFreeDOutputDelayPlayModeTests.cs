using System.Collections;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class SyncFreeDOutputDelayPlayModeTests
    {
        [UnityTest]
        public IEnumerator ManualTick_UsesDelayedCorrectedPoseWhenOutputDelayIsConfigured()
        {
            var cameraObject = new GameObject("SyncFreeD Output Delay Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.rotation = Quaternion.Euler(0f, 10f, 0f);
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            SetPrivateField(sync, "outputTickMode", OutputTickMode.Manual);
            SetPrivateField(sync, "tuning", new SyncTuningProfile { OutputDelayMs = 100 });

            yield return null;

            Assert.That(sync.ManualTick(), Is.True);
            var initialPan = sync.LastState.Corrected.PanDeg;

            cameraObject.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            Assert.That(sync.ManualTick(), Is.True);

            Assert.That(sync.LastState.Corrected.PanDeg, Is.EqualTo(initialPan).Within(0.0001d));

            Object.Destroy(cameraObject);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
