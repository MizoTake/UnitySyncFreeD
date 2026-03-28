using System.Collections;
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
    }
}
