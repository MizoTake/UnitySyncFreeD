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
        private FreeDLoopbackReceiverBehaviour targetLoopbackReceiver;
        private FreeDDrivenCameraBehaviour targetDrivenCamera;
        private FreeDControllerBehaviour targetController;
        private Transform directTargetTransform;
        private Camera directTargetCamera;
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
        public static FreeDDebugControllerWindow OpenWindow()
        {
            return GetWindow<FreeDDebugControllerWindow>("FreeD Debug Controller");
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
            targetLoopbackReceiver = null;
            targetDrivenCamera = drivenCamera;
            targetController = null;
            ResolveDirectTargets(drivenCamera != null ? drivenCamera.gameObject : inputSource != null ? inputSource.gameObject : null);
            SyncEndpointFromReceiver();
            CopyPoseAndLensFromDrivenCamera();
        }

        public void SetTargetForDebug(GameObject context)
        {
            targetInputSource = Support.SyncFreeDSupportSummary.FindInputSourceBehaviour(context);
            targetLoopbackReceiver = targetInputSource == null ? Support.SyncFreeDSupportSummary.FindLoopbackReceiverBehaviour(context) : null;
            targetDrivenCamera = Support.SyncFreeDSupportSummary.FindDrivenCameraBehaviour(context);
            targetController = Support.SyncFreeDSupportSummary.FindControllerBehaviour(context);
            ResolveDirectTargets(context);
            SyncEndpointFromReceiver();
            CopyPoseAndLensFromDrivenCamera();
        }

        public bool SendCurrentPacketForDebug()
        {
            SendPacket();
            return !string.IsNullOrEmpty(lastPacketHex);
        }

        private void OnGUI()
        {
            if ((targetInputSource == null || !targetInputSource) && (targetLoopbackReceiver == null || !targetLoopbackReceiver) && directTargetTransform == null)
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
                if (GUILayout.Button("対象 camera から姿勢を取得"))
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
            EditorGUILayout.HelpBox("scene 上の FreeDInputSourceBehaviour / FreeDLoopbackReceiverBehaviour があれば待受設定に向けて送信しつつ、対象 camera に debug apply します。", MessageType.Info);
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
            EditorGUILayout.HelpBox("Hierarchy の選択を優先し、未選択なら scene から自動検出します。手動の ObjectField は使いません。", MessageType.None);
            EditorGUILayout.LabelField("現在の選択", Selection.activeGameObject != null ? Selection.activeGameObject.name : "なし");
            EditorGUILayout.LabelField("操作対象", BuildDirectTargetSummary());
            EditorGUILayout.LabelField("受信 Behaviour", BuildReceiverSummary());
            EditorGUILayout.LabelField("送信先", BuildEndpointSummary());
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(Selection.activeGameObject == null))
                {
                    if (GUILayout.Button("選択から更新"))
                    {
                        SyncFromSelection();
                    }
                }

                if (GUILayout.Button("Scene から再検出"))
                {
                    SyncFromSceneTarget();
                }
            }
            preferLocalLoopbackForSceneDebug = EditorGUILayout.Toggle("Prefer Local Loopback", preferLocalLoopbackForSceneDebug);
            if (targetInputSource != null || targetLoopbackReceiver != null)
            {
                EditorGUILayout.HelpBox(targetLoopbackReceiver != null || preferLocalLoopbackForSceneDebug ? "scene デバッグでは 127.0.0.1:listenPort に送ります。" : "受信 camera の multicast / unicast 設定に合わせて送ります。", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("受信 Behaviour が見つからないため、送信ボタンは対象 camera へ直接 debug apply します。", MessageType.None);
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
            if ((targetInputSource == null || !targetInputSource) && (targetLoopbackReceiver == null || !targetLoopbackReceiver) && directTargetTransform == null)
            {
                SyncFromSceneTarget();
            }

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
            var shouldApplyDirectly = ShouldApplyDirectlyForDebug();
            var hasUdpTarget = targetInputSource != null || targetLoopbackReceiver != null;
            if (hasUdpTarget)
            {
                transport ??= new FreeDUdpTransport();
                if (sendMode == PacketSendMode.Multicast)
                {
                    transport.Send(packetBuffer, multicastGroupIpAddress, multicastPort);
                }
                else
                {
                    transport.Send(packetBuffer, destinationIpAddress, destinationPort);
                }
            }

            if (shouldApplyDirectly)
            {
                ApplyPoseAndLensDirectly();
            }
        }

        private void SyncFromSelection()
        {
            if (Selection.activeGameObject == null)
            {
                return;
            }

            SetTargetForDebug(Selection.activeGameObject);
        }

        private void SyncFromSceneTarget()
        {
            SetTargetForDebug(Selection.activeGameObject);
            CopyPoseAndLensFromDrivenCamera();
        }

        private void SyncEndpointFromReceiver()
        {
            var listenPort = -1;
            if (targetInputSource != null)
            {
                listenPort = targetInputSource.ListenPort;
            }
            else if (targetLoopbackReceiver != null)
            {
                listenPort = targetLoopbackReceiver.ListenPort;
            }

            if (listenPort <= 0)
            {
                return;
            }

            if (targetLoopbackReceiver != null || preferLocalLoopbackForSceneDebug)
            {
                sendMode = PacketSendMode.SingleDestinationUnicast;
                destinationIpAddress = "127.0.0.1";
                destinationPort = listenPort;
                multicastPort = listenPort;
                return;
            }

            sendMode = targetInputSource.JoinMulticastGroup ? PacketSendMode.Multicast : PacketSendMode.SingleDestinationUnicast;
            destinationPort = listenPort;
            multicastPort = listenPort;
            multicastGroupIpAddress = !string.IsNullOrWhiteSpace(targetInputSource.MulticastGroupIpAddress) ? targetInputSource.MulticastGroupIpAddress : multicastGroupIpAddress;
            destinationIpAddress = !string.IsNullOrWhiteSpace(targetInputSource.BindAddress) ? targetInputSource.BindAddress : "127.0.0.1";
        }

        private void CopyPoseAndLensFromDrivenCamera()
        {
            if (directTargetTransform == null)
            {
                return;
            }

            var euler = directTargetTransform.rotation.eulerAngles;
            panDeg = NormalizeSignedAngle(euler.y);
            tiltDeg = NormalizeSignedAngle(-euler.x);
            rollDeg = NormalizeSignedAngle(euler.z);
            xMeters = directTargetTransform.position.x;
            yMeters = directTargetTransform.position.y;
            zMeters = directTargetTransform.position.z;
            if (directTargetCamera != null)
            {
                focalLengthMm = directTargetCamera.focalLength;
                focusDistanceMeters = directTargetCamera.focusDistance;
            }
        }

        private void ResolveDirectTargets(GameObject context)
        {
            if (targetDrivenCamera != null)
            {
                directTargetTransform = targetDrivenCamera.transform;
                directTargetCamera = targetDrivenCamera.GetComponent<Camera>();
                return;
            }

            if (targetController != null)
            {
                directTargetTransform = targetController.ControlledTransform;
                directTargetCamera = targetController.ControlledCamera;
                return;
            }

            if (context != null)
            {
                directTargetCamera = FindContextCamera(context);
                directTargetTransform = directTargetCamera != null ? directTargetCamera.transform : null;
                return;
            }

            var syncBehaviour = Support.SyncFreeDSupportSummary.FindSyncBehaviour(null);
            if (syncBehaviour != null)
            {
                directTargetTransform = syncBehaviour.transform;
                directTargetCamera = syncBehaviour.GetComponent<Camera>();
                return;
            }

            directTargetCamera = FindContextCamera(null);
            directTargetTransform = directTargetCamera != null ? directTargetCamera.transform : null;
        }

        private string BuildDirectTargetSummary()
        {
            if (directTargetTransform == null)
            {
                return "未検出";
            }

            if (targetDrivenCamera != null)
            {
                return $"{directTargetTransform.name} (FreeDDrivenCameraBehaviour)";
            }

            if (targetController != null)
            {
                return $"{directTargetTransform.name} (FreeDControllerBehaviour)";
            }

            return directTargetCamera != null ? $"{directTargetTransform.name} (Camera)" : directTargetTransform.name;
        }

        private string BuildReceiverSummary()
        {
            if (targetInputSource != null)
            {
                return $"{targetInputSource.name} (FreeDInputSourceBehaviour)";
            }

            if (targetLoopbackReceiver != null)
            {
                return $"{targetLoopbackReceiver.name} (FreeDLoopbackReceiverBehaviour)";
            }

            return "なし";
        }

        private string BuildEndpointSummary()
        {
            if (targetLoopbackReceiver != null)
            {
                return $"127.0.0.1:{destinationPort} (Loopback)";
            }

            if (targetInputSource != null)
            {
                if (sendMode == PacketSendMode.Multicast)
                {
                    return $"{multicastGroupIpAddress}:{multicastPort} (Multicast)";
                }

                return $"{destinationIpAddress}:{destinationPort} (Unicast)";
            }

            return "UDP送信なし / camera へ直接反映";
        }

        private bool ShouldApplyDirectlyForDebug()
        {
            return directTargetTransform != null;
        }

        private void ApplyPoseAndLensDirectly()
        {
            if (directTargetTransform == null)
            {
                return;
            }

            directTargetTransform.position = new Vector3(xMeters, yMeters, zMeters);
            directTargetTransform.rotation = Quaternion.Euler(-tiltDeg, panDeg, rollDeg);
            if (directTargetCamera == null)
            {
                return;
            }

            directTargetCamera.usePhysicalProperties = true;
            directTargetCamera.focalLength = focalLengthMm;
            directTargetCamera.focusDistance = focusDistanceMeters;
        }

        private static Camera FindContextCamera(GameObject context)
        {
            if (context == null)
            {
                return UnityEngine.Object.FindFirstObjectByType<Camera>();
            }

            return context.GetComponent<Camera>() ?? context.GetComponentInParent<Camera>() ?? context.GetComponentInChildren<Camera>(true);
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
            if (targetInputSource == null && targetLoopbackReceiver == null && targetDrivenCamera == null && targetController == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Runtime Diagnostics", EditorStyles.boldLabel);
            if (targetInputSource != null)
            {
                EditorGUILayout.LabelField($"Input Bound: {targetInputSource.IsBound}");
                EditorGUILayout.LabelField($"Input Bind Error: {targetInputSource.LastBindError}");
                EditorGUILayout.LabelField($"Input Received: {targetInputSource.ReceivedCount}");
                EditorGUILayout.LabelField($"Input Last Packet Length: {targetInputSource.LastPacketLength}");
                EditorGUILayout.LabelField($"Input Remote: {targetInputSource.LastRemoteEndpoint}");
                EditorGUILayout.LabelField($"Input Packet: {targetInputSource.LastPacketHex}");
                EditorGUILayout.LabelField($"Input Dropped: {targetInputSource.DroppedPacketCount}");
                EditorGUILayout.LabelField($"Input Last Drop Reason: {targetInputSource.LastDropReason}");
                EditorGUILayout.LabelField($"Input Last Dropped Length: {targetInputSource.LastDroppedPacketLength}");
                EditorGUILayout.LabelField($"Input Last Dropped Remote: {targetInputSource.LastDroppedRemoteEndpoint}");
                EditorGUILayout.LabelField($"Input Last Dropped Packet: {targetInputSource.LastDroppedPacketHex}");
            }

            if (targetLoopbackReceiver != null)
            {
                EditorGUILayout.LabelField($"Loopback Bound: {targetLoopbackReceiver.IsBound}");
                EditorGUILayout.LabelField($"Loopback Received: {targetLoopbackReceiver.ReceivedCount}");
                EditorGUILayout.LabelField($"Loopback Remote: {targetLoopbackReceiver.LastRemoteEndpoint}");
                EditorGUILayout.LabelField($"Loopback Packet: {targetLoopbackReceiver.LastPacketHex}");
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
