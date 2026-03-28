using System.Collections;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class SyncDiagnosticsBehaviourPlayModeTests
    {
        [UnityTest]
        public IEnumerator LateUpdate_UpdatesSummaryFromSyncBehaviour()
        {
            var cameraObject = new GameObject("SyncFreeD Diagnostics Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            var diagnostics = cameraObject.AddComponent<SyncDiagnosticsBehaviour>();

            yield return null;

            Assert.That(sync.LastState.SourceId, Is.Not.Empty);
            Assert.That(diagnostics.LastSummary, Does.Contain("Pan"));
            Assert.That(diagnostics.LastSummary, Does.Contain("Tracking"));
            Assert.That(diagnostics.LastSummary, Does.Contain("Cam"));
            Assert.That(diagnostics.LastSummary, Does.Contain("Zoom"));

            Object.Destroy(cameraObject);
        }
    }
}
