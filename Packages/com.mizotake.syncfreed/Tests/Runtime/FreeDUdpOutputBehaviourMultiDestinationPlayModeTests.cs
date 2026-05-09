using System.Collections;
using MizoTake.SyncFreeD.Networking;
using MizoTake.SyncFreeD.ScriptableObjects;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class FreeDUdpOutputBehaviourMultiDestinationPlayModeTests
    {
        [UnityTest]
        public IEnumerator LateUpdate_SendsToPrimaryAndEnabledAdditionalDestinations()
        {
            var cameraObject = new GameObject("Multi Destination Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var profile = CreateMultiDestinationProfile(new[]
            {
                new FreeDUdpDestination { IpAddress = "127.0.0.1", Port = 40001, Enabled = true },
                new FreeDUdpDestination { IpAddress = "127.0.0.1", Port = 40002, Enabled = false }
            });
            output.SetOutputProfileAsset(profile, true);
            cameraObject.AddComponent<SyncFreeDBehaviour>();

            yield return null;
            yield return null;

            Assert.That(output.LastRequestedDestinationCount, Is.EqualTo(2));
            Assert.That(output.LastSendSuccessCount, Is.EqualTo(2));

            Object.Destroy(profile);
            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator LateUpdate_RecordsDestinationDiagnosticsInSendOrder()
        {
            var cameraObject = new GameObject("Multi Destination Diagnostics Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var profile = CreateMultiDestinationProfile(new[]
            {
                new FreeDUdpDestination { IpAddress = "127.0.0.1", Port = 40001, Enabled = true },
                new FreeDUdpDestination { IpAddress = "127.0.0.1", Port = 40002, Enabled = true }
            });
            output.SetOutputProfileAsset(profile, true);
            cameraObject.AddComponent<SyncFreeDBehaviour>();

            yield return null;
            yield return null;

            var diagnostics = output.LastDestinationDiagnostics;
            Assert.That(diagnostics.Length, Is.EqualTo(3));
            Assert.That(output.LastDestinationDiagnosticCount, Is.EqualTo(3));
            Assert.That(diagnostics[0].Label, Is.EqualTo("Primary"));
            Assert.That(diagnostics[1].Order, Is.EqualTo(1));
            Assert.That(diagnostics[2].Order, Is.EqualTo(2));
            Assert.That(output.LastDestinationSpreadMicroseconds, Is.GreaterThanOrEqualTo(0L));

            Object.Destroy(profile);
            Object.Destroy(cameraObject);
        }

        private static FreeDUdpOutputProfileAsset CreateMultiDestinationProfile(FreeDUdpDestination[] destinations)
        {
            var profile = ScriptableObject.CreateInstance<FreeDUdpOutputProfileAsset>();
            profile.Value.PacketSendMode = PacketSendMode.MultiDestinationUnicast;
            profile.Value.AdditionalDestinations = destinations;
            return profile;
        }
    }
}
