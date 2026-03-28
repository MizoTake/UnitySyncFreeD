using System.Collections;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class RecordingOutputBehaviourPlayModeTests
    {
        [UnityTest]
        public IEnumerator Send_AppendsRecordedFrame()
        {
            var gameObject = new GameObject("Recording Output");
            var behaviour = gameObject.AddComponent<RecordingOutputBehaviour>();

            yield return null;

            behaviour.Send(new CameraSyncState { SourceId = "recording", CameraId = 7, Corrected = new PoseState { PanDeg = 1d }, Lens = new LensState { FocalLengthMm = 35d } });

            Assert.That(behaviour.RecordedFrameCount, Is.EqualTo(1));
            Assert.That(behaviour.LastCsvLine, Does.Contain("recording,7"));

            Object.Destroy(gameObject);
        }
    }
}
