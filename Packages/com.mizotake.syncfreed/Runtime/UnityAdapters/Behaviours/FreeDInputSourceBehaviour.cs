using System;
using System.Net;
using System.Net.Sockets;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Outputs;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class FreeDInputSourceBehaviour : MonoBehaviour, ICameraFrameProvider, ILensDataSource
    {
        [SerializeField] private string sourceId = "FreeDInput";
        [SerializeField] private int listenPort = 40000;
        [SerializeField] private string bindAddress = string.Empty;
        [SerializeField] private bool validateChecksum = true;
        [SerializeField] private int cameraIdFilter = -1;
        [SerializeField] private bool joinMulticastGroup;
        [SerializeField] private string multicastGroupIpAddress = "239.0.0.1";
        [SerializeField] private string multicastInterfaceAddress = string.Empty;

        private readonly FreeDPacketParser packetParser = new FreeDPacketParser();
        private UdpClient udpClient;
        private CameraObservedFrame lastFrame;

        public string SourceId => sourceId;
        public int CameraId => lastFrame.CameraId;
        public CameraCapabilities Capabilities => CameraCapabilities.PanTilt | CameraCapabilities.Roll | CameraCapabilities.Position | CameraCapabilities.Zoom | CameraCapabilities.Focus | CameraCapabilities.Iris | CameraCapabilities.ExternalTracking;
        public string LastPacketHex { get; private set; } = string.Empty;
        public string LastRemoteEndpoint { get; private set; } = string.Empty;
        public string LastBindError { get; private set; } = string.Empty;
        public int ReceivedCount { get; private set; }
        public int DroppedPacketCount { get; private set; }
        public bool IsBound => udpClient != null;

        private void OnEnable()
        {
            TryBind();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Rebind();
        }
#endif

        private void Update()
        {
            if (udpClient == null || udpClient.Available <= 0)
            {
                return;
            }

            while (udpClient != null && udpClient.Available > 0)
            {
                try
                {
                    var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    var packet = udpClient.Receive(ref remoteEndPoint);
                    if (!packetParser.TryParse(packet, validateChecksum, out var frame))
                    {
                        DroppedPacketCount++;
                        continue;
                    }

                    if (cameraIdFilter >= 0 && frame.CameraId != cameraIdFilter)
                    {
                        DroppedPacketCount++;
                        continue;
                    }

                    frame.SourceId = sourceId;
                    lastFrame = frame;
                    LastPacketHex = BitConverter.ToString(packet);
                    LastRemoteEndpoint = remoteEndPoint.ToString();
                    ReceivedCount++;
                }
                catch (SocketException)
                {
                    break;
                }
            }
        }

        public bool TryGetObservedState(out CameraObservedFrame frame)
        {
            return TryGetObservedFrame(out frame);
        }

        public bool TryGetObservedFrame(out CameraObservedFrame frame)
        {
            frame = lastFrame;
            return lastFrame.Pose.TimestampTicks != 0L;
        }

        public CameraCommandFrame CaptureCommandFrame()
        {
            return new CameraCommandFrame
            {
                SourceId = lastFrame.SourceId,
                CameraId = lastFrame.CameraId,
                Pose = lastFrame.Pose,
                Lens = lastFrame.Lens,
                Timing = lastFrame.Timing
            };
        }

        public bool TryGetLensState(out LensState lens)
        {
            lens = lastFrame.Lens;
            return lastFrame.Lens.FocalLengthMm > 0d || lastFrame.Lens.FocusDistanceMeters > 0d || lastFrame.Lens.IrisFNumber > 0d;
        }

        private void OnDisable()
        {
            udpClient?.Dispose();
            udpClient = null;
        }

        private void Rebind()
        {
            udpClient?.Dispose();
            udpClient = null;
            LastBindError = string.Empty;
            TryBind();
        }

        private void TryBind()
        {
            LastBindError = string.Empty;
            try
            {
                var localAddress = string.IsNullOrWhiteSpace(bindAddress) ? IPAddress.Any : IPAddress.Parse(bindAddress);
                udpClient = new UdpClient(AddressFamily.InterNetwork);
                udpClient.Client.ExclusiveAddressUse = false;
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(new IPEndPoint(localAddress, listenPort));
                udpClient.Client.Blocking = false;
                if (joinMulticastGroup && !string.IsNullOrWhiteSpace(multicastGroupIpAddress))
                {
                    try
                    {
                        var multicastAddress = IPAddress.Parse(multicastGroupIpAddress);
                        if (string.IsNullOrWhiteSpace(multicastInterfaceAddress))
                        {
                            udpClient.JoinMulticastGroup(multicastAddress);
                        }
                        else
                        {
                            udpClient.JoinMulticastGroup(multicastAddress, IPAddress.Parse(multicastInterfaceAddress));
                        }
                    }
                    catch (Exception exception)
                    {
                        LastBindError = $"Multicast join failed: {exception.Message}";
                        Debug.LogWarning($"SyncFreeD input source multicast join failed. Unicast receive remains available: {exception.Message}", this);
                    }
                }
            }
            catch (Exception exception)
            {
                LastBindError = exception.Message;
                Debug.LogWarning($"SyncFreeD input source failed to bind: {exception.Message}", this);
                udpClient?.Dispose();
                udpClient = null;
            }
        }
    }
}
