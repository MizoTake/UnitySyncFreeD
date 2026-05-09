using System.Collections;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class FreeDUdpOutputBehaviourDiagnosticsPlayModeTests
    {
        [UnityTest]
        public IEnumerator BuildPacket_MarksPacketHexDirtyUntilDiagnosticsReadIt()
        {
            var outputObject = new GameObject("FreeD Output Diagnostics");
            var output = outputObject.AddComponent<FreeDUdpOutputBehaviour>();

            output.BuildPacket(CreateState(1));
            Assert.That(GetPrivateBool(output, "lastPacketHexDirty"), Is.True);
            Assert.That(output.LastPacketHex, Does.StartWith("D1-01"));
            Assert.That(GetPrivateBool(output, "lastPacketHexDirty"), Is.False);
            output.BuildPacket(CreateState(2));
            Assert.That(GetPrivateBool(output, "lastPacketHexDirty"), Is.True);
            Assert.That(output.LastPacketHex, Does.StartWith("D1-02"));

            Object.Destroy(outputObject);
            yield return null;
        }

        private static CameraSyncState CreateState(int cameraId)
        {
            return new CameraSyncState
            {
                CameraId = cameraId,
                Corrected = new PoseState { PanDeg = 1d, TiltDeg = 2d, RollDeg = 3d, Xmm = 100d, Ymm = 200d, Zmm = 300d },
                CorrectedLens = new LensState { FocalLengthMm = 35d, FocusDistanceMeters = 2d, IrisFNumber = 2.8d },
                Timing = new TimingState { FrameModulo16 = 1 }
            };
        }

        private static bool GetPrivateBool(Object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return (bool)field.GetValue(target);
        }
    }
}
