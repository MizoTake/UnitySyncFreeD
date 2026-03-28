using System.Collections;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class DebugLogOutputBehaviourPlayModeTests
    {
        [UnityTest]
        public IEnumerator Send_StoresFormattedMessage()
        {
            var gameObject = new GameObject("Debug Log Output");
            var behaviour = gameObject.AddComponent<DebugLogOutputBehaviour>();

            yield return null;

            behaviour.Send(new CameraSyncState { SourceId = "runtime-debug", CameraId = 6, Corrected = new PoseState { PanDeg = 1d, TiltDeg = 2d, RollDeg = 3d } });

            Assert.That(behaviour.LastMessage, Does.Contain("runtime-debug"));
            Assert.That(behaviour.LastMessage, Does.Contain("CameraId=6"));

            Object.Destroy(gameObject);
        }
    }
}
