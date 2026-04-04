using System.Collections;
using MizoTake.SyncFreeD.Networking;
using MizoTake.SyncFreeD.ScriptableObjects;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class FreeDUdpOutputProfileAssetPlayModeTests
    {
        [UnityTest]
        public IEnumerator ApplyProfile_UsesPresetValues()
        {
            var cameraObject = new GameObject("Output Profile Camera");
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var profile = ScriptableObject.CreateInstance<FreeDUdpOutputProfileAsset>();
            profile.Value.PacketSendMode = PacketSendMode.MultiDestinationUnicast;
            profile.Value.DestinationIpAddress = "127.0.0.1";
            profile.Value.DestinationPort = 41000;
            profile.Value.AdditionalDestinations = new[]
            {
                new FreeDUdpDestination { IpAddress = "127.0.0.1", Port = 41001, Enabled = true }
            };
            profile.Value.CameraIdFilter = 12;

            output.SetOutputProfileAsset(profile, true);
            yield return null;

            Assert.That(output.SendMode, Is.EqualTo(PacketSendMode.MultiDestinationUnicast));
            Assert.That(output.DestinationPort, Is.EqualTo(41000));
            Assert.That(output.CameraIdFilter, Is.EqualTo(12));
            Assert.That(output.GetConfiguredDestinationCount(), Is.EqualTo(2));

            Object.Destroy(profile);
            Object.Destroy(cameraObject);
        }
    }
}
