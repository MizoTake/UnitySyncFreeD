using System.Collections;
using System.Net.Sockets;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Outputs;
using MizoTake.SyncFreeD.ScriptableObjects;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class FreeDDrivenCameraBehaviourPlayModeTests
    {
        [UnityTest]
        public IEnumerator LateUpdate_AppliesPoseAndLensToCamera()
        {
            const int port = 41022;
            var cameraObject = new GameObject("Driven Camera");
            var camera = cameraObject.AddComponent<Camera>();
            var source = cameraObject.AddComponent<FreeDInputSourceBehaviour>();
            var driver = cameraObject.AddComponent<FreeDDrivenCameraBehaviour>();
            SetPrivateField(source, "listenPort", port);
            SetPrivateField(source, "packetDecodingPreset", FreeDPacketDecodingPreset.SyncFreeDPhysicalV1);
            cameraObject.SetActive(false);
            cameraObject.SetActive(true);

            using (var udpClient = new UdpClient())
            {
                udpClient.Send(CreatePacket(4, 25f, -10f, 5f, 1500d, 2500d, 3500d, 60d, 5d, 3.2d, 2), FreeDPacketBuilder.PacketLength, "127.0.0.1", port);
            }

            yield return WaitUntilApplied(driver, 60);

            Assert.That(driver.LastAppliedFrame.CameraId, Is.EqualTo(4));
            Assert.That(cameraObject.transform.position.x, Is.EqualTo(1.5f).Within(0.02f));
            Assert.That(cameraObject.transform.position.y, Is.EqualTo(3.5f).Within(0.02f));
            Assert.That(cameraObject.transform.position.z, Is.EqualTo(2.5f).Within(0.02f));
            Assert.That(cameraObject.transform.eulerAngles.y, Is.EqualTo(25f).Within(0.1f));
            Assert.That(camera.usePhysicalProperties, Is.True);
            Assert.That(camera.focalLength, Is.EqualTo(60f).Within(0.01f));
            Assert.That(camera.focusDistance, Is.EqualTo(5f).Within(0.01f));
            Assert.That(driver.LastAppliedFieldOfView, Is.GreaterThan(0f));

            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator LateUpdate_AppliesFocusDistanceToDepthOfFieldTarget()
        {
            const int port = 41023;
            var cameraObject = new GameObject("Driven Camera With Depth Of Field");
            var camera = cameraObject.AddComponent<Camera>();
            var depthOfField = cameraObject.AddComponent<FakeDepthOfFieldComponent>();
            var source = cameraObject.AddComponent<FreeDInputSourceBehaviour>();
            var driver = cameraObject.AddComponent<FreeDDrivenCameraBehaviour>();
            SetPrivateField(source, "listenPort", port);
            SetPrivateField(source, "packetDecodingPreset", FreeDPacketDecodingPreset.SyncFreeDPhysicalV1);
            SetPrivateField(driver, "applyFocusDistanceToDepthOfField", true);
            SetPrivateField(driver, "depthOfFieldTarget", depthOfField);
            cameraObject.SetActive(false);
            cameraObject.SetActive(true);

            using (var udpClient = new UdpClient())
            {
                udpClient.Send(CreatePacket(5, 0d, 0d, 0d, 0d, 0d, 1500d, 50d, 7.5d, 2.8d, 3), FreeDPacketBuilder.PacketLength, "127.0.0.1", port);
            }

            yield return WaitUntilApplied(driver, 60);

            Assert.That(camera.focusDistance, Is.EqualTo(7.5f).Within(0.01f));
            Assert.That(depthOfField.focusDistance.value, Is.EqualTo(7.5f).Within(0.01f));
            Assert.That(depthOfField.focusDistance.overrideState, Is.True);
            Assert.That(driver.LastAppliedDepthOfFieldFocusDistance, Is.EqualTo(7.5f).Within(0.01f));

            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator LateUpdate_AppliesFocusDistanceThroughBuiltinProfileStyleTarget()
        {
            const int port = 41024;
            var cameraObject = new GameObject("Driven Camera With Builtin Profile Style Target");
            cameraObject.AddComponent<Camera>();
            var target = cameraObject.AddComponent<FakeBuiltinPostProcessTarget>();
            var source = cameraObject.AddComponent<FreeDInputSourceBehaviour>();
            var driver = cameraObject.AddComponent<FreeDDrivenCameraBehaviour>();
            SetPrivateField(source, "listenPort", port);
            SetPrivateField(source, "packetDecodingPreset", FreeDPacketDecodingPreset.SyncFreeDPhysicalV1);
            SetPrivateField(driver, "applyFocusDistanceToDepthOfField", true);
            SetPrivateField(driver, "depthOfFieldTarget", target);
            cameraObject.SetActive(false);
            cameraObject.SetActive(true);

            using (var udpClient = new UdpClient())
            {
                udpClient.Send(CreatePacket(6, 0d, 0d, 0d, 0d, 0d, 1500d, 50d, 9.25d, 2.8d, 4), FreeDPacketBuilder.PacketLength, "127.0.0.1", port);
            }

            yield return WaitUntilApplied(driver, 60);

            Assert.That(target.profile.settings[0].focusDistance.value, Is.EqualTo(9.25f).Within(0.01f));
            Assert.That(target.profile.settings[0].focusDistance.overrideState, Is.True);
            Assert.That(driver.LastAppliedDepthOfFieldFocusDistance, Is.EqualTo(9.25f).Within(0.01f));

            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator LateUpdate_SonyBrcX1000ProfilePreservesInstallationPoseAndAppliesProjection()
        {
            const int port = 41025;
            var inputProfile = ScriptableObject.CreateInstance<FreeDUdpInputProfileAsset>();
            inputProfile.Value.ListenPort = port;
            inputProfile.Value.BindAddress = "127.0.0.1";
            inputProfile.Value.PacketDecodingPreset = FreeDPacketDecodingPreset.BuiltInDeviceProfile;
            inputProfile.Value.BuiltInPacketDecodingProfileId = FreeDBuiltInPacketDecodingProfileIds.SonyBrcX1000Firmware210;
            var cameraObject = new GameObject("D1 PTZ Driven Camera");
            cameraObject.SetActive(false);
            cameraObject.transform.position = new Vector3(4f, 5f, 6f);
            cameraObject.transform.rotation = Quaternion.Euler(3f, 20f, 7f);
            var initialPosition = cameraObject.transform.position;
            var initialRotation = cameraObject.transform.rotation;
            var camera = cameraObject.AddComponent<Camera>();
            var source = cameraObject.AddComponent<FreeDInputSourceBehaviour>();
            source.SetInputProfileAsset(inputProfile, true);
            var driver = cameraObject.AddComponent<FreeDDrivenCameraBehaviour>();
            cameraObject.SetActive(true);

            using (var udpClient = new UdpClient())
            {
                udpClient.Send(CreateRawD1Packet(7, 0d, 0d, 35d, 1234d, 2345d, 3456d, 0x0000, 0x2000), FreeDPacketBuilder.PacketLength, "127.0.0.1", port);
            }

            yield return WaitUntilApplied(driver, 60);

            Assert.That(cameraObject.transform.position, Is.EqualTo(initialPosition));
            Assert.That(Quaternion.Angle(cameraObject.transform.rotation, initialRotation), Is.LessThan(0.01f));
            Assert.That(driver.LastAppliedFrame.Capabilities.HasFlag(CameraCapabilities.Position), Is.False);
            Assert.That(driver.LastAppliedFrame.Capabilities.HasFlag(CameraCapabilities.Roll), Is.False);
            Assert.That(camera.sensorSize.x, Is.EqualTo(11.7584319f).Within(0.0001f));
            Assert.That(camera.sensorSize.y, Is.EqualTo(6.6141179f).Within(0.0001f));
            Assert.That(camera.focalLength, Is.EqualTo(9.3f).Within(0.001f));
            Assert.That(driver.LastAppliedFieldOfView, Is.EqualTo(39.15054f).Within(0.01f));

            Object.Destroy(inputProfile);
            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator LateUpdate_D1PtzCapabilitiesApplyPanAndTiltToSeparateRigAxes()
        {
            const int port = 41026;
            var inputProfile = ScriptableObject.CreateInstance<FreeDUdpInputProfileAsset>();
            inputProfile.Value.ListenPort = port;
            inputProfile.Value.BindAddress = "127.0.0.1";
            inputProfile.Value.PacketDecodingPreset = FreeDPacketDecodingPreset.Custom;
            inputProfile.Value.CustomPacketDecodingProfile = CreateGenericD1PtzProfile();
            var installationRoot = new GameObject("D1 PTZ Installation Root");
            installationRoot.SetActive(false);
            installationRoot.transform.position = new Vector3(10f, 2f, -4f);
            var panAxis = new GameObject("Pan Axis").transform;
            panAxis.SetParent(installationRoot.transform, false);
            panAxis.localRotation = Quaternion.Euler(0f, 15f, 0f);
            var tiltAxis = new GameObject("Tilt Axis").transform;
            tiltAxis.SetParent(panAxis, false);
            tiltAxis.localRotation = Quaternion.Euler(5f, 0f, 0f);
            var opticalCenter = new GameObject("Optical Center");
            opticalCenter.transform.SetParent(tiltAxis, false);
            var camera = opticalCenter.AddComponent<Camera>();
            var source = installationRoot.AddComponent<FreeDInputSourceBehaviour>();
            source.SetInputProfileAsset(inputProfile, true);
            var driver = opticalCenter.AddComponent<FreeDDrivenCameraBehaviour>();
            SetPrivateField(driver, "sourceBehaviour", source);
            SetPrivateField(driver, "targetCamera", camera);
            SetPrivateField(driver, "panAxis", panAxis);
            SetPrivateField(driver, "tiltAxis", tiltAxis);
            installationRoot.SetActive(true);

            using (var udpClient = new UdpClient())
            {
                udpClient.Send(CreateRawD1Packet(8, 25d, 10d, 0d, 0d, 0d, 0d, 0x1800, 0x4000), FreeDPacketBuilder.PacketLength, "127.0.0.1", port);
            }

            yield return WaitUntilApplied(driver, 60);

            Assert.That(installationRoot.transform.position, Is.EqualTo(new Vector3(10f, 2f, -4f)));
            Assert.That(panAxis.localEulerAngles.y, Is.EqualTo(40f).Within(0.1f));
            Assert.That(Mathf.DeltaAngle(tiltAxis.localEulerAngles.x, -5f), Is.EqualTo(0f).Within(0.1f));
            Assert.That(camera.focalLength, Is.EqualTo(18.6f).Within(0.001f));

            Object.Destroy(inputProfile);
            Object.Destroy(installationRoot);
        }

        [UnityTest]
        public IEnumerator ApplyLatestFrame_IncompleteRigFallsBackWithoutDroppingTilt()
        {
            var cameraObject = new GameObject("Incomplete PTZ Rig");
            cameraObject.SetActive(false);
            cameraObject.AddComponent<Camera>();
            var source = cameraObject.AddComponent<FreeDDrivenCameraTestProvider>();
            source.Frame = CreateObservedFrame(CameraCapabilities.PanTilt, new PoseState { PanDeg = 25d, TiltDeg = 10d, TimestampTicks = 1L }, default, default, true, false);
            var driver = cameraObject.AddComponent<FreeDDrivenCameraBehaviour>();
            Assert.That(driver.SetSourceBehaviour(source), Is.True);
            var panAxis = new GameObject("Only Pan Axis").transform;
            panAxis.SetParent(cameraObject.transform, false);
            SetPrivateField(driver, "panAxis", panAxis);
            cameraObject.SetActive(true);

            Assert.That(driver.ApplyLatestFrame(), Is.True);
            Assert.That(driver.LastRigError, Is.Not.Empty);
            Assert.That(cameraObject.transform.eulerAngles.y, Is.EqualTo(25f).Within(0.1f));
            Assert.That(Mathf.DeltaAngle(cameraObject.transform.eulerAngles.x, -10f), Is.EqualTo(0f).Within(0.1f));

            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ApplyLatestFrame_InstallationRelativeRotationPreservesRestPoseWithRollCapability()
        {
            var cameraObject = new GameObject("Installation Relative Camera");
            cameraObject.SetActive(false);
            cameraObject.transform.rotation = Quaternion.Euler(3f, 20f, 7f);
            var initialRotation = cameraObject.transform.rotation;
            cameraObject.AddComponent<Camera>();
            var source = cameraObject.AddComponent<FreeDDrivenCameraTestProvider>();
            var pose = new PoseState { PanDeg = 25d, TiltDeg = 10d, RollDeg = 5d, TimestampTicks = 1L };
            source.Frame = CreateObservedFrame(CameraCapabilities.PanTilt | CameraCapabilities.Roll, pose, default, default, true, false);
            var driver = cameraObject.AddComponent<FreeDDrivenCameraBehaviour>();
            Assert.That(driver.SetSourceBehaviour(source), Is.True);
            cameraObject.SetActive(true);

            Assert.That(driver.ApplyLatestFrame(), Is.True);
            Assert.That(Quaternion.Angle(cameraObject.transform.rotation, initialRotation * Quaternion.Euler(-10f, 25f, 5f)), Is.LessThan(0.01f));

            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ApplyLatestFrame_ProfileWithoutLensCapabilitiesRestoresInitialCameraLens()
        {
            var cameraObject = new GameObject("Profile Switching Camera");
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.sensorSize = new Vector2(36f, 24f);
            camera.focalLength = 40f;
            camera.focusDistance = 3f;
            camera.usePhysicalProperties = false;
            var source = cameraObject.AddComponent<FreeDDrivenCameraTestProvider>();
            source.Frame = CreateObservedFrame(CameraCapabilities.Zoom | CameraCapabilities.Focus, new PoseState { TimestampTicks = 1L }, new LensState { FocalLengthMm = 111.6d, FocusDistanceMeters = 5d }, new CameraProjectionState { SensorWidthMm = 11.7584319d, SensorHeightMm = 6.6141179d }, false, true);
            var driver = cameraObject.AddComponent<FreeDDrivenCameraBehaviour>();
            Assert.That(driver.SetSourceBehaviour(source), Is.True);
            cameraObject.SetActive(true);

            Assert.That(driver.ApplyLatestFrame(), Is.True);
            Assert.That(camera.focalLength, Is.EqualTo(111.6f).Within(0.001f));
            source.Frame = CreateObservedFrame(CameraCapabilities.None, new PoseState { TimestampTicks = 2L }, default, default, false, false);
            Assert.That(driver.ApplyLatestFrame(), Is.True);
            Assert.That(camera.sensorSize, Is.EqualTo(new Vector2(36f, 24f)));
            Assert.That(camera.focalLength, Is.EqualTo(40f).Within(0.001f));
            Assert.That(camera.focusDistance, Is.EqualTo(3f).Within(0.001f));
            Assert.That(camera.usePhysicalProperties, Is.False);

            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ApplyLatestFrame_FocusOnlyCapabilityDoesNotApplyZoomFields()
        {
            var cameraObject = new GameObject("Focus Only Camera");
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.focalLength = 35f;
            var source = cameraObject.AddComponent<FreeDDrivenCameraTestProvider>();
            source.Frame = CreateObservedFrame(CameraCapabilities.Focus, new PoseState { TimestampTicks = 1L }, new LensState { FocalLengthMm = 100d, FocusDistanceMeters = 5d }, default, false, true);
            var driver = cameraObject.AddComponent<FreeDDrivenCameraBehaviour>();
            Assert.That(driver.SetSourceBehaviour(source), Is.True);
            cameraObject.SetActive(true);

            Assert.That(driver.ApplyLatestFrame(), Is.True);
            Assert.That(camera.focalLength, Is.EqualTo(35f).Within(0.001f));
            Assert.That(camera.focusDistance, Is.EqualTo(5f).Within(0.001f));

            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ApplyLatestFrame_MotionSmoothingDistributesPacketStepAcrossRenderFrames()
        {
            var cameraObject = new GameObject("Smoothed Free-D Camera");
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            var source = cameraObject.AddComponent<FreeDDrivenCameraTestProvider>();
            source.Frame = CreateObservedFrame(CameraCapabilities.PanTilt | CameraCapabilities.Position | CameraCapabilities.Zoom | CameraCapabilities.Focus, new PoseState { PanDeg = 0d, Xmm = 0d, TimestampTicks = 1L }, new LensState { FocalLengthMm = 35d, FocusDistanceMeters = 2d }, default, true, true);
            var driver = cameraObject.AddComponent<FreeDDrivenCameraBehaviour>();
            Assert.That(driver.SetSourceBehaviour(source), Is.True);
            driver.SetMotionSmoothing(true, 0.04f, 0.04f, 0.06f);
            cameraObject.SetActive(true);
            Assert.That(driver.ApplyLatestFrame(1f / 60f), Is.True);

            source.Frame = CreateObservedFrame(CameraCapabilities.PanTilt | CameraCapabilities.Position | CameraCapabilities.Zoom | CameraCapabilities.Focus, new PoseState { PanDeg = 30d, Xmm = 1000d, TimestampTicks = 2L }, new LensState { FocalLengthMm = 70d, FocusDistanceMeters = 8d }, default, true, true);
            var previousPan = cameraObject.transform.eulerAngles.y;
            Assert.That(driver.ApplyLatestFrame(1f / 60f), Is.True);
            var firstPan = cameraObject.transform.eulerAngles.y;
            var maximumStep = Mathf.Abs(Mathf.DeltaAngle(previousPan, firstPan));

            Assert.That(firstPan, Is.GreaterThan(0f).And.LessThan(30f));
            Assert.That(cameraObject.transform.position.x, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(camera.focalLength, Is.GreaterThan(35f).And.LessThan(70f));
            Assert.That(camera.focusDistance, Is.GreaterThan(2f).And.LessThan(8f));
            for (var frame = 0; frame < 40; frame++)
            {
                previousPan = cameraObject.transform.eulerAngles.y;
                Assert.That(driver.ApplyLatestFrame(1f / 60f), Is.True);
                maximumStep = Mathf.Max(maximumStep, Mathf.Abs(Mathf.DeltaAngle(previousPan, cameraObject.transform.eulerAngles.y)));
            }

            Assert.That(maximumStep, Is.LessThan(10f));
            Assert.That(cameraObject.transform.eulerAngles.y, Is.EqualTo(30f).Within(0.02f));
            Assert.That(cameraObject.transform.position.x, Is.EqualTo(1f).Within(0.002f));
            Assert.That(camera.focalLength, Is.EqualTo(70f).Within(0.02f));
            Assert.That(camera.focusDistance, Is.EqualTo(8f).Within(0.01f));

            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ApplyLatestFrame_MotionSmoothingIsFrameRateIndependent()
        {
            var thirtyFpsObject = CreateSmoothedTestRig("30 FPS Smoothed Camera", out var thirtyFpsCamera, out var thirtyFpsSource, out var thirtyFpsDriver);
            var sixtyFpsObject = CreateSmoothedTestRig("60 FPS Smoothed Camera", out var sixtyFpsCamera, out var sixtyFpsSource, out var sixtyFpsDriver);
            Assert.That(thirtyFpsDriver.ApplyLatestFrame(1f / 30f), Is.True);
            Assert.That(sixtyFpsDriver.ApplyLatestFrame(1f / 60f), Is.True);
            var targetFrame = CreateObservedFrame(CameraCapabilities.PanTilt | CameraCapabilities.Zoom, new PoseState { PanDeg = 30d, TimestampTicks = 2L }, new LensState { FocalLengthMm = 70d }, default, true, true);
            thirtyFpsSource.Frame = targetFrame;
            sixtyFpsSource.Frame = targetFrame;

            for (var frame = 0; frame < 30; frame++)
            {
                Assert.That(thirtyFpsDriver.ApplyLatestFrame(1f / 30f), Is.True);
            }
            for (var frame = 0; frame < 60; frame++)
            {
                Assert.That(sixtyFpsDriver.ApplyLatestFrame(1f / 60f), Is.True);
            }

            Assert.That(Quaternion.Angle(thirtyFpsObject.transform.rotation, sixtyFpsObject.transform.rotation), Is.LessThan(0.001f));
            Assert.That(thirtyFpsCamera.focalLength, Is.EqualTo(sixtyFpsCamera.focalLength).Within(0.001f));

            Object.Destroy(thirtyFpsObject);
            Object.Destroy(sixtyFpsObject);
            yield return null;
        }

        private static byte[] CreatePacket(int cameraId, double panDeg, double tiltDeg, double rollDeg, double xmm, double ymm, double zmm, double focalLengthMm, double focusDistanceMeters, double irisFNumber, ushort frameModulo16)
        {
            var packet = new byte[FreeDPacketBuilder.PacketLength];
            new FreeDPacketBuilder().Build(new CameraSyncState
            {
                CameraId = cameraId,
                Corrected = new PoseState { PanDeg = panDeg, TiltDeg = tiltDeg, RollDeg = rollDeg, Xmm = xmm, Ymm = ymm, Zmm = zmm },
                CorrectedLens = new LensState { FocalLengthMm = focalLengthMm, FocusDistanceMeters = focusDistanceMeters, IrisFNumber = irisFNumber },
                Timing = new TimingState { FrameModulo16 = frameModulo16 }
            }, packet);
            return packet;
        }

        private static CameraObservedFrame CreateObservedFrame(CameraCapabilities capabilities, PoseState pose, LensState lens, CameraProjectionState projection, bool trackingValid, bool lensValid)
        {
            return new CameraObservedFrame { SourceId = "test", CameraId = 1, Capabilities = capabilities, Pose = pose, Lens = lens, Projection = projection, Validity = new ValidityState { IsTrackingValid = trackingValid, IsLensValid = lensValid } };
        }

        private static GameObject CreateSmoothedTestRig(string name, out Camera camera, out FreeDDrivenCameraTestProvider source, out FreeDDrivenCameraBehaviour driver)
        {
            var cameraObject = new GameObject(name);
            cameraObject.SetActive(false);
            camera = cameraObject.AddComponent<Camera>();
            source = cameraObject.AddComponent<FreeDDrivenCameraTestProvider>();
            source.Frame = CreateObservedFrame(CameraCapabilities.PanTilt | CameraCapabilities.Zoom, new PoseState { PanDeg = 0d, TimestampTicks = 1L }, new LensState { FocalLengthMm = 35d }, default, true, true);
            driver = cameraObject.AddComponent<FreeDDrivenCameraBehaviour>();
            Assert.That(driver.SetSourceBehaviour(source), Is.True);
            driver.SetMotionSmoothing(true, 0.04f, 0.04f, 0.06f);
            cameraObject.SetActive(true);
            return cameraObject;
        }

        private static byte[] CreateRawD1Packet(int cameraId, double panDeg, double tiltDeg, double rollDeg, double xmm, double ymm, double zmm, int rawZoom, int rawFocus)
        {
            var packet = CreatePacket(cameraId, panDeg, tiltDeg, rollDeg, xmm, ymm, zmm, 1d, 1d, 2.8d, 0);
            WriteUInt24(packet, 20, rawZoom);
            WriteUInt24(packet, 23, rawFocus);
            packet[28] = FreeDChecksumCalculator.Calculate(new System.ReadOnlySpan<byte>(packet, 0, 28));
            return packet;
        }

        private static FreeDPacketDecodingProfile CreateGenericD1PtzProfile()
        {
            return new FreeDPacketDecodingProfile
            {
                ProfileName = "Generic D1 PTZ Test",
                Capabilities = CameraCapabilities.PanTilt | CameraCapabilities.Zoom,
                FocalLengthMm = new FreeDUnsignedFieldDecoder { Mode = FreeDUnsignedFieldDecodingMode.ScaleAndOffset, Scale = 18.6d / 0x1800 }
            };
        }

        private static void WriteUInt24(byte[] packet, int offset, int value)
        {
            packet[offset] = (byte)((value >> 16) & 0xFF);
            packet[offset + 1] = (byte)((value >> 8) & 0xFF);
            packet[offset + 2] = (byte)(value & 0xFF);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static IEnumerator WaitUntilApplied(FreeDDrivenCameraBehaviour behaviour, int maxFrames)
        {
            for (var frame = 0; frame < maxFrames; frame++)
            {
                if (behaviour.LastAppliedFrame.Pose.TimestampTicks != 0L)
                {
                    yield break;
                }

                yield return null;
            }
        }

        private sealed class FakeDepthOfFieldComponent : MonoBehaviour
        {
            public FakeFloatParameter focusDistance = new FakeFloatParameter();
        }

        private sealed class FakeFloatParameter
        {
            public bool overrideState;
            public float value;
        }

        private sealed class FakeBuiltinPostProcessTarget : MonoBehaviour
        {
            public FakeBuiltinPostProcessProfile profile = new FakeBuiltinPostProcessProfile();
        }

        private sealed class FakeBuiltinPostProcessProfile
        {
            public System.Collections.Generic.List<FakeBuiltinDepthOfField> settings = new System.Collections.Generic.List<FakeBuiltinDepthOfField> { new FakeBuiltinDepthOfField() };
        }

        private sealed class FakeBuiltinDepthOfField
        {
            public FakeFloatParameter focusDistance = new FakeFloatParameter();
        }
    }

    public sealed class FreeDDrivenCameraTestProvider : MonoBehaviour, ICameraFrameProvider, ILensDataSource
    {
        public CameraObservedFrame Frame;
        public string SourceId => Frame.SourceId;
        public int CameraId => Frame.CameraId;
        public CameraCapabilities Capabilities => Frame.Capabilities;

        bool ICameraSource.TryGetObservedState(out CameraObservedFrame frame)
        {
            return TryGetFrame(out frame);
        }

        bool ICameraFrameProvider.TryGetObservedFrame(out CameraObservedFrame frame)
        {
            return TryGetFrame(out frame);
        }

        CameraCommandFrame ICameraFrameProvider.CaptureCommandFrame()
        {
            return new CameraCommandFrame { SourceId = Frame.SourceId, CameraId = Frame.CameraId, Pose = Frame.Pose, Lens = Frame.Lens, Timing = Frame.Timing };
        }

        bool ILensDataSource.TryGetLensState(out LensState lens)
        {
            lens = Frame.Lens;
            return Frame.Pose.TimestampTicks != 0L && Frame.Validity.IsLensValid;
        }

        private bool TryGetFrame(out CameraObservedFrame frame)
        {
            frame = Frame;
            return Frame.Pose.TimestampTicks != 0L;
        }
    }
}
