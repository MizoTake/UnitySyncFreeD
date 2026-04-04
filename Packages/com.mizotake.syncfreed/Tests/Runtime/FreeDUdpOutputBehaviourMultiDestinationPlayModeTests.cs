using System.Collections;
using MizoTake.SyncFreeD.Networking;
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
            cameraObject.AddComponent<SyncFreeDBehaviour>();
            SetPrivateField(output, "packetSendMode", PacketSendMode.MultiDestinationUnicast);
            SetPrivateField(output, "additionalDestinations", new[]
            {
                new FreeDUdpDestination { IpAddress = "127.0.0.1", Port = 40001, Enabled = true },
                new FreeDUdpDestination { IpAddress = "127.0.0.1", Port = 40002, Enabled = false }
            });

            yield return null;
            yield return null;

            Assert.That(output.LastRequestedDestinationCount, Is.EqualTo(2));
            Assert.That(output.LastSendSuccessCount, Is.EqualTo(2));

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
            cameraObject.AddComponent<SyncFreeDBehaviour>();
            SetPrivateField(output, "packetSendMode", PacketSendMode.MultiDestinationUnicast);
            SetPrivateField(output, "additionalDestinations", new[]
            {
                new FreeDUdpDestination { IpAddress = "127.0.0.1", Port = 40001, Enabled = true },
                new FreeDUdpDestination { IpAddress = "127.0.0.1", Port = 40002, Enabled = true }
            });

            yield return null;
            yield return null;

            var diagnostics = output.LastDestinationDiagnostics;
            Assert.That(diagnostics.Length, Is.EqualTo(3));
            Assert.That(diagnostics[0].Label, Is.EqualTo("Primary"));
            Assert.That(diagnostics[1].Order, Is.EqualTo(1));
            Assert.That(diagnostics[2].Order, Is.EqualTo(2));
            Assert.That(output.LastDestinationSpreadMicroseconds, Is.GreaterThanOrEqualTo(0L));

            Object.Destroy(cameraObject);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
