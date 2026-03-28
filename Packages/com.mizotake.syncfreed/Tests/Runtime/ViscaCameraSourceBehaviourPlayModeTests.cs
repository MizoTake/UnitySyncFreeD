using System.Collections;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class ViscaCameraSourceBehaviourPlayModeTests
    {
        [UnityTest]
        public IEnumerator DualDrive_UsesSeparateCommandAndObservedTransforms()
        {
            var root = new GameObject("Visca Source Root");
            var commandObject = new GameObject("Command Target");
            var observedObject = new GameObject("Observed Target");
            commandObject.transform.position = new Vector3(0f, 1f, -10f);
            commandObject.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            observedObject.transform.position = new Vector3(1f, 1f, -10f);
            observedObject.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            var camera = root.AddComponent<Camera>();
            root.AddComponent<AudioListener>();
            var source = root.AddComponent<ViscaCameraSourceBehaviour>();
            var output = root.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = root.AddComponent<SyncFreeDBehaviour>();
            SetPrivateField(source, "commandTransform", commandObject.transform);
            SetPrivateField(source, "observedTransform", observedObject.transform);
            SetPrivateField(sync, "syncMode", SyncMode.DualDrive);
            SetPrivateField(sync, "sourceBehaviour", source);
            SetPrivateField(sync, "outputBehaviour", output);

            yield return null;

            Assert.That(camera, Is.Not.Null);
            Assert.That(sync.LastState.Command.PanDeg, Is.EqualTo(20d).Within(0.0001d));
            Assert.That(sync.LastState.Observed.PanDeg, Is.EqualTo(30d).Within(0.0001d));
            Assert.That(sync.LastState.Corrected.PanDeg, Is.GreaterThan(20d));
            Assert.That(output.LastPacketHex, Is.Not.Empty);

            Object.Destroy(root);
            Object.Destroy(commandObject);
            Object.Destroy(observedObject);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        [UnityTest]
        public IEnumerator DualDrive_UsesTelemetryProviderWhenAssigned()
        {
            var root = new GameObject("Visca Source Telemetry Root");
            var commandObject = new GameObject("Command Target");
            commandObject.transform.rotation = Quaternion.Euler(0f, 5f, 0f);
            var provider = root.AddComponent<FakeViscaTelemetryProvider>();
            root.AddComponent<Camera>();
            root.AddComponent<AudioListener>();
            var source = root.AddComponent<ViscaCameraSourceBehaviour>();
            var output = root.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = root.AddComponent<SyncFreeDBehaviour>();
            provider.Frame = new ViscaTelemetryFrame
            {
                ObservedPose = new PoseState { PanDeg = 35d, TimestampTicks = System.DateTime.UtcNow.Ticks },
                Lens = new LensState { FocalLengthMm = 88d, FocusDistanceMeters = 5d, IrisFNumber = 4d },
                Timing = new TimingState { FrameModulo16 = 9, TrackingDelayMs = 12 },
                Validity = new ValidityState { IsTrackingValid = true, IsLensValid = true }
            };
            SetPrivateField(source, "commandTransform", commandObject.transform);
            SetPrivateField(source, "telemetryProviderBehaviour", provider);
            SetPrivateField(sync, "syncMode", SyncMode.DualDrive);
            SetPrivateField(sync, "sourceBehaviour", source);
            SetPrivateField(sync, "outputBehaviour", output);

            yield return null;

            Assert.That(sync.LastState.Command.PanDeg, Is.EqualTo(5d).Within(0.0001d));
            Assert.That(sync.LastState.Observed.PanDeg, Is.EqualTo(35d).Within(0.0001d));
            Assert.That(sync.LastState.ObservedLens.FocalLengthMm, Is.EqualTo(88d).Within(0.0001d));
            Assert.That(sync.LastState.Timing.FrameModulo16, Is.EqualTo(9));

            Object.Destroy(root);
            Object.Destroy(commandObject);
        }

        private sealed class FakeViscaTelemetryProvider : MonoBehaviour, IViscaTelemetryProvider
        {
            public ViscaTelemetryFrame Frame;

            public bool TryGetTelemetry(out ViscaTelemetryFrame frame)
            {
                frame = Frame;
                return true;
            }
        }
    }
}
