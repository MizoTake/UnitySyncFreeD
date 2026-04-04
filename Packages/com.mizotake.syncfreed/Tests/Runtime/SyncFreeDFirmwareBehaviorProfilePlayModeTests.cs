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

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
