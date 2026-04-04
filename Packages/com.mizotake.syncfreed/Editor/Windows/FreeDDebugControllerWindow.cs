using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Outputs;
using MizoTake.SyncFreeD.Networking;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using UnityEditor;
using UnityEngine;

namespace MizoTake.SyncFreeD.Editor.Windows
{
    public sealed class FreeDDebugControllerWindow : EditorWindow
    {
        private readonly byte[] packetBuffer = new byte[FreeDPacketBuilder.PacketLength];
        private readonly FreeDPacketBuilder packetBuilder = new FreeDPacketBuilder();
        private FreeDUdpTransport transport;
        private FreeDInputSourceBehaviour targetInputSource;
        private FreeDDrivenCameraBehaviour targetDrivenCamera;
        private bool preferLocalLoopbackForSceneDebug = true;
        private bool continuousSend;
        private double nextSendTime;
        private PacketSendMode sendMode = PacketSendMode.Multicast;
        private string destinationIpAddress = "127.0.0.1";
        private int destinationPort = 41030;
        private string multicastGroupIpAddress = "239.10.10.10";
        private int multicastPort = 41030;
        private int cameraId = 10;
        private float panDeg;
        private float tiltDeg;
        private float rollDeg;
        private float xMeters;
        private float yMeters = 1.5f;
        private float zMeters = -8f;
        private float focalLengthMm = 50f;
        private float focusDistanceMeters = 10f;
        private float irisFNumber = 2.8f;
        private int frameModulo16;
        private float sendRateHz = 30f;
        private float translateStepMeters = 0.1f;
        private float angleStepDegrees = 5f;
        private float zoomStepMm = 5f;
        private float focusStepMeters = 0.5f;
        private string lastPacketHex = string.Empty;

        [MenuItem("Tools/SyncFreeD/Debug Controller")]
        public static void OpenWindow()
        {
            GetWindow<FreeDDebugControllerWindow>("FreeD Debug Controller");
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            SyncFromSceneTarget();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            transport?.Dispose();
            transport = null;
            continuousSend = false;
        }

        private void OnSelectionChange()
        {
            SyncFromSelection();
            Repaint();
        }

        public void SetTargetForDebug(FreeDInputSourceBehaviour inputSource, FreeDDrivenCameraBehaviour drivenCamera)
        {
            targetInputSource = inputSource;
            targetDrivenCamera = drivenCamera;
            SyncEndpointFromInputSource();
            CopyPoseAndLensFromDrivenCamera();
        }

        public bool SendCurrentPacketForDebug()
        {
            SendPacket();
            return !string.IsNullOrEmpty(lastPacketHex);
        }

        private void OnGUI()
        {
            if (targetInputSource == null || !targetInputSource)
            {
                SyncFromSceneTarget();
            }

            DrawTargetPanel();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("送信先", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(targetInputSource != null))
            {
                sendMode = (PacketSendMode)EditorGUILayout.EnumPopup("Send Mode", sendMode);
            }
            if (sendMode == PacketSendMode.Multicast)
            {
                multicastGroupIpAddress = EditorGUILayout.TextField("Multicast Group", multicastGroupIpAddress);
                multicastPort = EditorGUILayout.IntField("Multicast Port", multicastPort);
            }
            else
            {
                destinationIpAddress = EditorGUILayout.TextField("Destination IP", destinationIpAddress);
                destinationPort = EditorGUILayout.IntField("Destination Port", destinationPort);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Free-D State", EditorStyles.boldLabel);
            cameraId = Mathf.Clamp(EditorGUILayout.IntField("Camera ID", cameraId), 0, 255);
            DrawPoseButtons();
            DrawPositionButtons();
            DrawLensButtons();
            EditorGUILayout.Space();
            panDeg = EditorGUILayout.FloatField("Pan (deg)", panDeg);
            tiltDeg = EditorGUILayout.FloatField("Tilt (deg)", tiltDeg);
            rollDeg = EditorGUILayout.FloatField("Roll (deg)", rollDeg);
            xMeters = EditorGUILayout.FloatField("X (m)", xMeters);
            yMeters = EditorGUILayout.FloatField("Y (m)", yMeters);
            zMeters = EditorGUILayout.FloatField("Z (m)", zMeters);
            focalLengthMm = Mathf.Max(1f, EditorGUILayout.FloatField("Focal Length (mm)", focalLengthMm));
            focusDistanceMeters = Mathf.Max(0.01f, EditorGUILayout.FloatField("Focus Distance (m)", focusDistanceMeters));
            irisFNumber = Mathf.Max(0.1f, EditorGUILayout.FloatField("Iris F", irisFNumber));
            frameModulo16 = Mathf.Clamp(EditorGUILayout.IntField("Frame Modulo 16", frameModulo16), 0, 15);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("連続送信", EditorStyles.boldLabel);
            sendRateHz = Mathf.Clamp(EditorGUILayout.FloatField("Send Rate (Hz)", sendRateHz), 1f, 120f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Send Once"))
                {
                    SendPacket();
                }

                if (GUILayout.Button(continuousSend ? "Stop Continuous" : "Start Continuous"))
                {
                    continuousSend = !continuousSend;
                    nextSendTime = EditorApplication.timeSinceStartup;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("受信 camera から姿勢を取得"))
                {
                    CopyPoseAndLensFromDrivenCamera();
                }

                if (GUILayout.Button("Reset Pose"))
                {
                    panDeg = 0f;
                    tiltDeg = 0f;
                    rollDeg = 0f;
                    xMeters = 0f;
                    yMeters = 1.5f;
                    zMeters = -8f;
                }

                if (GUILayout.Button("Reset Lens"))
                {
                    focalLengthMm = 50f;
                    focusDistanceMeters = 10f;
                    irisFNumber = 2.8f;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("scene 上の FreeDInputSourceBehaviour を自動検出し、その待受設定に向けて送信します。受信専用 sample では Play Mode 中にこの window から Free-D を送って確認します。", MessageType.Info);
            DrawRuntimeDiagnostics();
            EditorGUILayout.SelectableLabel(lastPacketHex, EditorStyles.textField, GUILayout.Height(36f));
        }

        private void OnEditorUpdate()
        {
            if (!continuousSend || EditorApplication.timeSinceStartup < nextSendTime)
            {
                return;
            }

            SendPacket();
            nextSendTime = EditorApplication.timeSinceStartup + (1d / Mathf.Max(1f, sendRateHz));
        }

        private void DrawTargetPanel()
        {
            EditorGUILayout.LabelField("対象 camera", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                var newInputSource = (FreeDInputSourceBehaviour)EditorGUILayout.ObjectField("Input Source", targetInputSource, typeof(FreeDInputSourceBehaviour), true);
                if (newInputSource != targetInputSource)
                {
                    targetInputSource = newInputSource;
                    SyncEndpointFromInputSource();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                targetDrivenCamera = (FreeDDrivenCameraBehaviour)EditorGUILayout.ObjectField("Driven Camera", targetDrivenCamera, typeof(FreeDDrivenCameraBehaviour), true);
                if (GUILayout.Button("Scene から自動検出", GUILayout.Width(140f)))
                {
                    SyncFromSceneTarget();
                }
            }

            preferLocalLoopbackForSceneDebug = EditorGUILayout.Toggle("Prefer Local Loopback", preferLocalLoopbackForSceneDebug);
            if (targetInputSource != null)
            {
                EditorGUILayout.HelpBox(preferLocalLoopbackForSceneDebug ? "scene デバッグでは 127.0.0.1:listenPort に送ります。" : "受信 camera の multicast / unicast 設定に合わせて送ります。", MessageType.None);
            }
        }

        private void DrawPoseButtons()
        {
            EditorGUILayout.LabelField("Pan / Tilt / Roll", EditorStyles.boldLabel);
            angleStepDegrees = Mathf.Max(0.1f, EditorGUILayout.FloatField("Angle Step (deg)", angleStepDegrees));
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawRepeatButton("Pan -", () => panDeg -= angleStepDegrees);
                DrawRepeatButton("Pan +", () => panDeg += angleStepDegrees);
                DrawRepeatButton("Tilt -", () => tiltDeg -= angleStepDegrees);
                DrawRepeatButton("Tilt +", () => tiltDeg += angleStepDegrees);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawRepeatButton("Roll -", () => rollDeg -= angleStepDegrees);
                DrawRepeatButton("Roll +", () => rollDeg += angleStepDegrees);
            }
        }

        private void DrawPositionButtons()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
            translateStepMeters = Mathf.Max(0.01f, EditorGUILayout.FloatField("Move Step (m)", translateStepMeters));
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawRepeatButton("Left", () => xMeters -= translateStepMeters);
                DrawRepeatButton("Right", () => xMeters += translateStepMeters);
                DrawRepeatButton("Down", () => yMeters -= translateStepMeters);
                DrawRepeatButton("Up", () => yMeters += translateStepMeters);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawRepeatButton("Back", () => zMeters -= translateStepMeters);
                DrawRepeatButton("Forward", () => zMeters += translateStepMeters);
            }
        }

        private void DrawLensButtons()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Zoom / Focus", EditorStyles.boldLabel);
            zoomStepMm = Mathf.Max(0.1f, EditorGUILayout.FloatField("Zoom Step (mm)", zoomStepMm));
            focusStepMeters = Mathf.Max(0.01f, EditorGUILayout.FloatField("Focus Step (m)", focusStepMeters));
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawRepeatButton("Zoom -", () => focalLengthMm = Mathf.Max(1f, focalLengthMm - zoomStepMm));
                DrawRepeatButton("Zoom +", () => focalLengthMm += zoomStepMm);
                DrawRepeatButton("Focus -", () => focusDistanceMeters = Mathf.Max(0.01f, focusDistanceMeters - focusStepMeters));
                DrawRepeatButton("Focus +", () => focusDistanceMeters += focusStepMeters);
            }
        }

        private void SendPacket()
        {
            if (targetInputSource == null || !targetInputSource)
            {
                SyncFromSceneTarget();
            }

            transport ??= new FreeDUdpTransport();
            var state = new CameraSyncState
            {
                CameraId = cameraId,
                Corrected = new PoseState
                {
                    PanDeg = panDeg,
                    TiltDeg = tiltDeg,
                    RollDeg = rollDeg,
                    Xmm = xMeters * 1000d,
                    Ymm = zMeters * 1000d,
                    Zmm = yMeters * 1000d
                },
                CorrectedLens = new LensState
                {
                    FocalLengthMm = focalLengthMm,
                    FocusDistanceMeters = focusDistanceMeters,
                    IrisFNumber = irisFNumber
                },
                Timing = new TimingState
                {
                    FrameModulo16 = (ushort)frameModulo16
                }
            };
            packetBuilder.Build(state, packetBuffer);
            lastPacketHex = System.BitConverter.ToString(packetBuffer);
            if (sendMode == PacketSendMode.Multicast)
            {
                transport.Send(packetBuffer, multicastGroupIpAddress, multicastPort);
                return;
            }

            transport.Send(packetBuffer, destinationIpAddress, destinationPort);
        }

        private void SyncFromSelection()
        {
            if (Selection.activeGameObject == null)
            {
                return;
            }

            targetDrivenCamera = Selection.activeGameObject.GetComponent<FreeDDrivenCameraBehaviour>() ?? targetDrivenCamera;
            targetInputSource = Selection.activeGameObject.GetComponent<FreeDInputSourceBehaviour>() ?? targetInputSource;
            if (targetDrivenCamera != null && targetInputSource == null)
            {
                targetInputSource = targetDrivenCamera.GetComponent<FreeDInputSourceBehaviour>();
            }

            SyncEndpointFromInputSource();
        }

        private void SyncFromSceneTarget()
        {
            if (targetDrivenCamera == null)
            {
                targetDrivenCamera = FindFirstObjectByType<FreeDDrivenCameraBehaviour>();
            }

            if (targetInputSource == null)
            {
                targetInputSource = targetDrivenCamera != null ? targetDrivenCamera.GetComponent<FreeDInputSourceBehaviour>() : FindFirstObjectByType<FreeDInputSourceBehaviour>();
            }

            SyncEndpointFromInputSource();
            CopyPoseAndLensFromDrivenCamera();
        }

        private void SyncEndpointFromInputSource()
        {
            if (targetInputSource == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(targetInputSource);
            var joinMulticastGroupProperty = serializedObject.FindProperty("joinMulticastGroup");
            var listenPortProperty = serializedObject.FindProperty("listenPort");
            var multicastGroupProperty = serializedObject.FindProperty("multicastGroupIpAddress");
            var bindAddressProperty = serializedObject.FindProperty("bindAddress");
            var listenPort = listenPortProperty != null ? listenPortProperty.intValue : destinationPort;
            if (preferLocalLoopbackForSceneDebug)
            {
                sendMode = PacketSendMode.SingleDestinationUnicast;
                destinationIpAddress = "127.0.0.1";
                destinationPort = listenPort;
                multicastPort = listenPort;
                return;
            }

            sendMode = joinMulticastGroupProperty != null && joinMulticastGroupProperty.boolValue ? PacketSendMode.Multicast : PacketSendMode.SingleDestinationUnicast;
            destinationPort = listenPort;
            multicastPort = listenPort;
            multicastGroupIpAddress = multicastGroupProperty != null && !string.IsNullOrWhiteSpace(multicastGroupProperty.stringValue) ? multicastGroupProperty.stringValue : multicastGroupIpAddress;
            destinationIpAddress = bindAddressProperty != null && !string.IsNullOrWhiteSpace(bindAddressProperty.stringValue) ? bindAddressProperty.stringValue : "127.0.0.1";
        }

        private void CopyPoseAndLensFromDrivenCamera()
        {
            if (targetDrivenCamera == null)
            {
                return;
            }

            var targetTransform = targetDrivenCamera.transform;
            var camera = targetDrivenCamera.GetComponent<Camera>();
            var euler = targetTransform.rotation.eulerAngles;
            panDeg = NormalizeSignedAngle(euler.y);
            tiltDeg = NormalizeSignedAngle(-euler.x);
            rollDeg = NormalizeSignedAngle(euler.z);
            xMeters = targetTransform.position.x;
            yMeters = targetTransform.position.y;
            zMeters = targetTransform.position.z;
            if (camera != null)
            {
                focalLengthMm = camera.focalLength;
                focusDistanceMeters = camera.focusDistance;
            }
        }

        private static float NormalizeSignedAngle(float angle)
        {
            return Mathf.Repeat(angle + 180f, 360f) - 180f;
        }

        private void DrawRepeatButton(string label, System.Action action)
        {
            if (!GUILayout.RepeatButton(label))
            {
                return;
            }

            action();
            SendPacket();
            Repaint();
        }

        private void DrawRuntimeDiagnostics()
        {
            if (targetInputSource == null && targetDrivenCamera == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Runtime Diagnostics", EditorStyles.boldLabel);
            if (targetInputSource != null)
            {
                EditorGUILayout.LabelField($"Input Bound: {targetInputSource.IsBound}");
                EditorGUILayout.LabelField($"Input Bind Error: {targetInputSource.LastBindError}");
                EditorGUILayout.LabelField($"Input Received: {targetInputSource.ReceivedCount}");
                EditorGUILayout.LabelField($"Input Remote: {targetInputSource.LastRemoteEndpoint}");
                EditorGUILayout.LabelField($"Input Packet: {targetInputSource.LastPacketHex}");
            }

            if (targetDrivenCamera != null)
            {
                EditorGUILayout.LabelField($"Applied Ticks: {targetDrivenCamera.LastAppliedFrame.Pose.TimestampTicks}");
                EditorGUILayout.LabelField($"Applied FOV: {targetDrivenCamera.LastAppliedFieldOfView:F2}");
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                SyncFromSceneTarget();
                Repaint();
            }
        }
    }
}
