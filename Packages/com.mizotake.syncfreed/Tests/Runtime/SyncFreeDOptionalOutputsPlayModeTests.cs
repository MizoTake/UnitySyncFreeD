using System.Collections;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class SyncFreeDOptionalOutputsPlayModeTests
    {
        [UnityTest]
        public IEnumerator LateUpdate_WritesDebugAndRecordingOutputs()
        {
            var cameraObject = new GameObject("SyncFreeD Optional Outputs Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var debugOutput = cameraObject.AddComponent<DebugLogOutputBehaviour>();
            var recordingOutput = cameraObject.AddComponent<RecordingOutputBehaviour>();
            cameraObject.AddComponent<SyncFreeDBehaviour>();

            yield return null;

            Assert.That(debugOutput.LastMessage, Is.Not.Empty);
            Assert.That(recordingOutput.RecordedFrameCount, Is.GreaterThan(0));

            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator ManualTick_WithDisabledOptionalOutputs_DoesNotWriteToThem()
        {
            var cameraObject = new GameObject("SyncFreeD Disabled Optional Outputs Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var debugOutput = cameraObject.AddComponent<DebugLogOutputBehaviour>();
            var recordingOutput = cameraObject.AddComponent<RecordingOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            SetPrivateField(sync, "outputTickMode", OutputTickMode.Manual);
            debugOutput.enabled = false;
            recordingOutput.enabled = false;

            yield return null;

            Assert.That(sync.ManualTick(), Is.True);
            Assert.That(sync.LastState.SourceId, Is.Not.Empty);
            Assert.That(debugOutput.LastMessage, Is.Empty);
            Assert.That(recordingOutput.RecordedFrameCount, Is.Zero);

            Object.Destroy(cameraObject);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
