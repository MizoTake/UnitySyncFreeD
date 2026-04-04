using System.Collections;
using MizoTake.SyncFreeD.Core.Diagnostics;
using MizoTake.SyncFreeD.Networking;
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
            SetPrivateField(diagnostics, "logLevel", SyncLogLevel.Packet);

            yield return null;

            Assert.That(sync.LastState.SourceId, Is.Not.Empty);
            Assert.That(diagnostics.LastSummary, Does.Contain("Pan"));
            Assert.That(diagnostics.LastSummary, Does.Contain("Tracking"));
            Assert.That(diagnostics.LastSummary, Does.Contain("Cam"));
            Assert.That(diagnostics.LastSummary, Does.Contain("Zoom"));
            Assert.That(diagnostics.LastSummary, Does.Contain("SpreadUs"));

            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator LateUpdate_InVerboseMode_ShowsDestinationSpread()
        {
            var cameraObject = new GameObject("SyncFreeD Verbose Diagnostics Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            cameraObject.AddComponent<SyncFreeDBehaviour>();
            var diagnostics = cameraObject.AddComponent<SyncDiagnosticsBehaviour>();
            SetPrivateField(output, "packetSendMode", PacketSendMode.MultiDestinationUnicast);
            SetPrivateField(output, "additionalDestinations", new[]
            {
                new FreeDUdpDestination { IpAddress = "127.0.0.1", Port = 40001, Enabled = true }
            });
            SetPrivateField(diagnostics, "logLevel", SyncLogLevel.Verbose);

            yield return null;
            yield return null;

            Assert.That(diagnostics.LastSummary, Does.Contain("DestSpreadUs"));
            Assert.That(diagnostics.LastSummary, Does.Contain("Dest 2"));

            Object.Destroy(cameraObject);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
