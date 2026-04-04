using MizoTake.SyncFreeD.Editor.Support;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using UnityEditor;
using UnityEngine;

namespace MizoTake.SyncFreeD.Editor.Windows
{
    public sealed class SyncFreeDDebugWindow : EditorWindow
    {
        private SyncFreeDBehaviour targetBehaviour;
        private Vector2 scrollPosition;
        private GUIStyle sectionStyle;
        private GUIStyle miniValueStyle;

        [MenuItem("Tools/SyncFreeD/Debug Window")]
        public static void OpenWindow()
        {
            GetWindow<SyncFreeDDebugWindow>("SyncFreeD Debug");
        }

        private void OnSelectionChange()
        {
            if (Selection.activeGameObject != null)
            {
                targetBehaviour = Selection.activeGameObject.GetComponent<SyncFreeDBehaviour>();
            }

            Repaint();
        }

        private void OnGUI()
        {
            EnsureStyles();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Operator Window"))
                {
                    SyncFreeDOperatorWindow.OpenWindow();
                }

                if (GUILayout.Button("Open Setup Wizard"))
                {
                    Setup.SyncFreeDSetupWizard.OpenWindow();
                }
            }

            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
            targetBehaviour = (SyncFreeDBehaviour)EditorGUILayout.ObjectField(targetBehaviour, typeof(SyncFreeDBehaviour), true);
            if (targetBehaviour == null)
            {
                EditorGUILayout.HelpBox("SyncFreeDBehaviour を選択するか ObjectField に指定してください。", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            var state = targetBehaviour.LastState;
            var diagnostics = targetBehaviour.LastDiagnostics;
            var support = SyncFreeDSupportSummary.Build(targetBehaviour);
            var output = targetBehaviour.GetComponent<FreeDUdpOutputBehaviour>();
            var debugSnapshot = SyncFreeDDebugSnapshotBuilder.Build(state, diagnostics, output);
            var multicastSupport = FreeDUdpMulticastSupportSummary.Build(output);
            DrawSupportBanner(support);
            EditorGUILayout.Space();
            DrawSection("State Compare", SyncFreeDStatusTone.Info, () =>
            {
                EditorGUILayout.TextField("SourceId", state.SourceId ?? string.Empty);
                EditorGUILayout.IntField("CameraId", state.CameraId);
                DrawComparisonTable(debugSnapshot.PoseRows);
                EditorGUILayout.Space();
                DrawComparisonTable(debugSnapshot.LensRows);
            });
            EditorGUILayout.Space();
            var diagnosticsTone = diagnostics.IsTrackingValid && !diagnostics.IsDegraded ? SyncFreeDStatusTone.Ready : SyncFreeDStatusTone.ActionNeeded;
            DrawSection("Diagnostics", diagnosticsTone, () =>
            {
                DrawMetricRow("Pan Error", $"{diagnostics.PanErrorDeg:F2}");
                DrawMetricRow("Tilt Error", $"{diagnostics.TiltErrorDeg:F2}");
                DrawMetricRow("Roll Error", $"{diagnostics.RollErrorDeg:F2}");
                DrawMetricRow("Position Error (mm)", $"{diagnostics.PositionErrorMm:F1}");
                DrawMetricRow("Zoom Error (mm)", $"{diagnostics.ZoomErrorMm:F1}");
                DrawMetricRow("Tracking Delay (ms)", diagnostics.TrackingDelayMs.ToString());
                DrawMetricRow("Video Delay (ms)", diagnostics.VideoAlignmentDelayMs.ToString());
                EditorGUILayout.Toggle("Tracking Valid", diagnostics.IsTrackingValid);
                EditorGUILayout.Toggle("Lens Valid", diagnostics.IsLensValid);
                EditorGUILayout.Toggle("Degraded", diagnostics.IsDegraded);
                EditorGUILayout.Toggle("Fallback Mode", diagnostics.IsFallbackMode);
                EditorGUILayout.Toggle("Correction Applied", diagnostics.CorrectionApplied);
                EditorGUILayout.IntField("Correction Count", targetBehaviour.CorrectionAppliedCount);
            });
            EditorGUILayout.Space();
            DrawSection("Profiles", SyncFreeDStatusTone.Info, () =>
            {
                EditorGUILayout.ObjectField("Tuning", targetBehaviour.TuningProfileAsset, typeof(ScriptableObjects.SyncTuningProfileAsset), false);
                EditorGUILayout.ObjectField("Device", targetBehaviour.DeviceProfileAsset, typeof(ScriptableObjects.DeviceProfileAsset), false);
                EditorGUILayout.ObjectField("Firmware", targetBehaviour.FirmwareBehaviorProfileAsset, typeof(ScriptableObjects.FirmwareBehaviorProfileAsset), false);
                EditorGUILayout.ObjectField("Lens", targetBehaviour.LensProfileAsset, typeof(ScriptableObjects.LensProfileAsset), false);
                EditorGUILayout.ObjectField("Mount", targetBehaviour.MountProfileAsset, typeof(ScriptableObjects.MountProfileAsset), false);
                if (targetBehaviour.HasFirmwareBehaviorWarning)
                {
                    EditorGUILayout.HelpBox(targetBehaviour.FirmwareBehaviorWarning, MessageType.Warning);
                }
            });
            var debugOutput = targetBehaviour.GetComponent<DebugLogOutputBehaviour>();
            var recordingOutput = targetBehaviour.GetComponent<RecordingOutputBehaviour>();
            EditorGUILayout.Space();
            var packetTone = output != null && !output.HasConfigurationWarning ? SyncFreeDStatusTone.Ready : SyncFreeDStatusTone.Warning;
            DrawSection("Packet", packetTone, () =>
            {
                EditorGUILayout.SelectableLabel(output != null ? output.LastPacketHex : "Output not found", EditorStyles.textArea, GUILayout.Height(48f));
                if (output != null)
                {
                    DrawMetricRow("Packet Length", output.PacketLength.ToString());
                    DrawMetricRow("Configured Destinations", output.GetConfiguredDestinationCount().ToString());
                    DrawMetricRow("Last Requested Destinations", output.LastRequestedDestinationCount.ToString());
                    DrawMetricRow("Destination Spread (us)", output.LastDestinationSpreadMicroseconds.ToString());
                    DrawMetricRow("Bind Address", output.BindAddress);
                    DrawMetricRow("Destination IP", output.DestinationIpAddress);
                    DrawMetricRow("Destination Port", output.DestinationPort.ToString());
                    DrawMetricRow("Socket Buffer Size", output.SocketBufferSize.ToString());
                    DrawMetricRow("Camera ID Filter", output.CameraIdFilter.ToString());
                    DrawMetricRow("Multicast Group", output.MulticastGroupIpAddress);
                    DrawMetricRow("Multicast Port", output.MulticastPort.ToString());
                    DrawMetricRow("Multicast Interface", output.MulticastInterfaceAddress);
                    DrawMetricRow("Send Success", output.LastSendSuccessCount.ToString());
                    DrawMetricRow("Send Failures", output.TotalSendFailureCount.ToString());
                    EditorGUILayout.Toggle("Skipped By Filter", output.LastSendSkippedByFilter);
                    EditorGUILayout.Toggle("Multicast Configured", output.IsMulticastConfigured);
                    DrawMetricRow("Checksum", output.LastChecksum.ToString("X2"));
                    DrawMetricRow("User Area", output.LastUserArea.ToString("X4"));
                    DrawMetricList("Destination Diagnostics", debugSnapshot.DestinationLines);
                    if (output.SendMode == Networking.PacketSendMode.Multicast)
                    {
                        DrawMetricList("Multicast Support", multicastSupport.Lines);
                    }
                    if (output.HasConfigurationWarning)
                    {
                        EditorGUILayout.HelpBox(output.ConfigurationWarning, MessageType.Warning);
                    }
                    else if (output.SendMode == Networking.PacketSendMode.Multicast && multicastSupport.HasActionNeeded)
                    {
                        EditorGUILayout.HelpBox("Multicast の NIC / join / bind 設定に確認事項があります。", MessageType.Warning);
                    }
                }
            });

            EditorGUILayout.Space();
            DrawSection("Optional Outputs", SyncFreeDStatusTone.Info, () =>
            {
                DrawMetricRow("Debug Log", debugOutput != null ? debugOutput.LastMessage : "Not attached");
                DrawMetricRow("Recording", recordingOutput != null ? recordingOutput.LastCsvLine : "Not attached");
            });
            var loopbackReceiver = targetBehaviour.GetComponent<FreeDLoopbackReceiverBehaviour>();
            if (loopbackReceiver != null)
            {
                EditorGUILayout.Space();
                var loopbackTone = loopbackReceiver.ReceivedCount > 0 ? SyncFreeDStatusTone.Ready : SyncFreeDStatusTone.Info;
                DrawSection("Loopback", loopbackTone, () =>
                {
                    EditorGUILayout.Toggle("Bound", loopbackReceiver.IsBound);
                    DrawMetricRow("Received Count", loopbackReceiver.ReceivedCount.ToString());
                    DrawMetricRow("Remote Endpoint", loopbackReceiver.LastRemoteEndpoint);
                    DrawMetricRow("Last Received UTC", loopbackReceiver.LastReceivedAtUtcTicks > 0L ? new System.DateTime(loopbackReceiver.LastReceivedAtUtcTicks, System.DateTimeKind.Utc).ToString("u") : string.Empty);
                    EditorGUILayout.SelectableLabel(loopbackReceiver.LastPacketHex, EditorStyles.textArea, GUILayout.Height(48f));
                });
            }

            EditorGUILayout.EndScrollView();
            Repaint();
        }

        private void DrawSupportBanner(SyncFreeDSupportSnapshot support)
        {
            using (new ColorScope(SyncFreeDSupportSummary.GetBackgroundColor(support.Tone)))
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(support.StatusTitle, sectionStyle);
                    EditorGUILayout.LabelField(support.GuidanceMessage, EditorStyles.wordWrappedLabel);
                }
            }
        }

        private void DrawSection(string title, SyncFreeDStatusTone tone, System.Action drawBody)
        {
            using (new ColorScope(SyncFreeDSupportSummary.GetBackgroundColor(tone)))
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var previousColor = GUI.color;
                    GUI.color = SyncFreeDSupportSummary.GetAccentColor(tone);
                    EditorGUILayout.LabelField(title, sectionStyle);
                    GUI.color = previousColor;
                    drawBody();
                }
            }
        }

        private void DrawMetricRow(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(160f));
                EditorGUILayout.SelectableLabel(value ?? string.Empty, miniValueStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }

        private void DrawMetricList(string label, string[] values)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            if (values == null || values.Length == 0)
            {
                EditorGUILayout.LabelField("None");
                return;
            }

            for (var i = 0; i < values.Length; i++)
            {
                EditorGUILayout.SelectableLabel(values[i] ?? string.Empty, miniValueStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }

        private void DrawComparisonTable(SyncFreeDDebugValueRow[] rows)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(96f));
                EditorGUILayout.LabelField("Command", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("Predicted", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("Observed", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("Corrected", EditorStyles.miniBoldLabel);
            }

            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(row.Label, GUILayout.Width(96f));
                    EditorGUILayout.SelectableLabel(row.Command, miniValueStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    EditorGUILayout.SelectableLabel(row.Predicted, miniValueStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    EditorGUILayout.SelectableLabel(row.Observed, miniValueStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    EditorGUILayout.SelectableLabel(row.Corrected, miniValueStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                }
            }
        }

        private void EnsureStyles()
        {
            if (sectionStyle == null)
            {
                sectionStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            }

            if (miniValueStyle == null)
            {
                miniValueStyle = new GUIStyle(EditorStyles.textField) { wordWrap = false };
            }
        }

        private readonly struct ColorScope : System.IDisposable
        {
            private readonly Color previousColor;

            public ColorScope(Color color)
            {
                previousColor = GUI.backgroundColor;
                GUI.backgroundColor = color;
            }

            public void Dispose()
            {
                GUI.backgroundColor = previousColor;
            }
        }
    }
}
