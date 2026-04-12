using System.Collections;
using MizoTake.SyncFreeD.ScriptableObjects;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class SyncFreeDFirmwareBehaviorProfilePlayModeTests
    {
        [UnityTest]
        public IEnumerator LateUpdate_ReportsFirmwareWarningWhenOutputModeIsUnsupported()
        {
            var cameraObject = new GameObject("SyncFreeD Firmware Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            var firmwareAsset = ScriptableObject.CreateInstance<FirmwareBehaviorProfileAsset>();
            firmwareAsset.Value.SupportsMulticast = false;
            SetPrivateField(output, "packetSendMode", Networking.PacketSendMode.Multicast);
            SetPrivateField(sync, "firmwareBehaviorProfileAsset", firmwareAsset);

            yield return null;

            Assert.That(sync.HasFirmwareBehaviorWarning, Is.True);
            Assert.That(sync.FirmwareBehaviorWarning, Does.Contain("Multicast"));

            Object.Destroy(firmwareAsset);
            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator ManualTick_FallsBackToSingleDestinationWhenConfiguredModeIsUnsupported()
        {
            var cameraObject = new GameObject("SyncFreeD Firmware Fallback Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            var firmwareAsset = ScriptableObject.CreateInstance<FirmwareBehaviorProfileAsset>();
            firmwareAsset.Value.SupportsMulticast = false;
            firmwareAsset.Value.SupportsMultiUnicast = false;
            firmwareAsset.Value.MaxUnicastDestinationCount = 1;
            SetPrivateField(output, "packetSendMode", Networking.PacketSendMode.Multicast);
            SetPrivateField(sync, "outputTickMode", Core.Models.OutputTickMode.Manual);
            SetPrivateField(sync, "firmwareBehaviorProfileAsset", firmwareAsset);

            yield return null;

            Assert.That(sync.ManualTick(), Is.True);
            Assert.That(output.LastEffectiveSendMode, Is.EqualTo(Networking.PacketSendMode.SingleDestinationUnicast));
            Assert.That(output.LastRequestedDestinationCount, Is.EqualTo(1));

            Object.Destroy(firmwareAsset);
            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator ManualTick_LimitsAdditionalDestinationsBasedOnFirmwareProfile()
        {
            var cameraObject = new GameObject("SyncFreeD Firmware Multi Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            var firmwareAsset = ScriptableObject.CreateInstance<FirmwareBehaviorProfileAsset>();
            firmwareAsset.Value.SupportsMulticast = false;
            firmwareAsset.Value.SupportsMultiUnicast = true;
            firmwareAsset.Value.MaxUnicastDestinationCount = 2;
            SetPrivateField(output, "packetSendMode", Networking.PacketSendMode.MultiDestinationUnicast);
            SetPrivateField(output, "additionalDestinations", new[]
            {
                new FreeDUdpDestination { IpAddress = "127.0.0.1", Port = 40001, Enabled = true },
                new FreeDUdpDestination { IpAddress = "127.0.0.1", Port = 40002, Enabled = true }
            });
            SetPrivateField(sync, "outputTickMode", Core.Models.OutputTickMode.Manual);
            SetPrivateField(sync, "firmwareBehaviorProfileAsset", firmwareAsset);

            yield return null;

            Assert.That(sync.ManualTick(), Is.True);
            Assert.That(output.LastEffectiveSendMode, Is.EqualTo(Networking.PacketSendMode.MultiDestinationUnicast));
            Assert.That(output.LastRequestedDestinationCount, Is.EqualTo(2));
            Assert.That(output.LastSendSuccessCount, Is.EqualTo(2));

            Object.Destroy(firmwareAsset);
            Object.Destroy(cameraObject);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
