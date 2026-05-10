using System;
using System.Net;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Outputs;
using MizoTake.SyncFreeD.Networking;
using MizoTake.SyncFreeD.ScriptableObjects;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class FreeDInputSourceBehaviour : MonoBehaviour, ICameraFrameProvider, ILensDataSource
    {
        [SerializeField] private string sourceId = "FreeDInput";
        [SerializeField] private FreeDUdpInputProfileAsset inputProfileAsset;
        [SerializeField] private bool applyProfileOnEnable = true;
        [SerializeField] private int listenPort = 40000;
        [SerializeField] private string bindAddress = string.Empty;
        [SerializeField] private bool validateChecksum = true;
        [SerializeField] private int cameraIdFilter = -1;
        [SerializeField] private bool joinMulticastGroup;
        [SerializeField] private string multicastGroupIpAddress = "239.0.0.1";
        [SerializeField] private string multicastInterfaceAddress = string.Empty;
        [SerializeField] private MonoBehaviour commandSourceBehaviour;

        private readonly FreeDPacketParser packetParser = new FreeDPacketParser();
        private FreeDUdpReceiveHub receiveHub;
        private CameraObservedFrame lastFrame;

        public string SourceId => sourceId;
        public int CameraId => lastFrame.CameraId;
        public CameraCapabilities Capabilities => CameraCapabilities.PanTilt | CameraCapabilities.Roll | CameraCapabilities.Position | CameraCapabilities.Zoom | CameraCapabilities.Focus | CameraCapabilities.Iris | CameraCapabilities.ExternalTracking;
        public string LastPacketHex { get; private set; } = string.Empty;
        public string LastRemoteEndpoint { get; private set; } = string.Empty;
        public string LastBindError { get; private set; } = string.Empty;
        public int ReceivedCount { get; private set; }
        public int DroppedPacketCount { get; private set; }
        public int LastPacketLength { get; private set; }
        public FreeDPacketFailureReason LastDropReason { get; private set; }
        public int LastDroppedPacketLength { get; private set; }
        public string LastDroppedPacketHex { get; private set; } = string.Empty;
        public string LastDroppedRemoteEndpoint { get; private set; } = string.Empty;
        public bool IsBound => receiveHub != null && receiveHub.IsBound;
        public FreeDUdpInputProfileAsset InputProfileAsset => inputProfileAsset;
        public bool ApplyProfileOnEnable => applyProfileOnEnable;
        public int ListenPort => listenPort;
        public string BindAddress => bindAddress;
        public bool ValidateChecksum => validateChecksum;
        public int CameraIdFilter => cameraIdFilter;
        public bool JoinMulticastGroup => joinMulticastGroup;
        public string MulticastGroupIpAddress => multicastGroupIpAddress;
        public string MulticastInterfaceAddress => multicastInterfaceAddress;
        public event Action<CameraObservedFrame> ObservedFrameUpdated;

        private void Reset()
        {
            if (applyProfileOnEnable)
            {
                ApplyProfile();
            }
        }

        private void OnEnable()
        {
            if (applyProfileOnEnable)
            {
                ApplyProfile();
            }
            TryBind();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (applyProfileOnEnable)
            {
                ApplyProfile();
            }

            if (!Application.isPlaying)
            {
                return;
            }

            Rebind();
        }
#endif

        private void Update()
        {
            receiveHub?.Poll();
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
            var commandSource = ResolveCommandSource();
            if (commandSource != null)
            {
                return commandSource.CaptureCommandFrame();
            }

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

        public void SetInputProfileAsset(FreeDUdpInputProfileAsset profileAsset, bool applyImmediately)
        {
            inputProfileAsset = profileAsset;
            if (applyImmediately)
            {
                ApplyProfile();
            }
        }

        private void OnDisable()
        {
            receiveHub?.Release(HandleReceivedPacket);
            receiveHub = null;
        }

        private void Rebind()
        {
            receiveHub?.Release(HandleReceivedPacket);
            receiveHub = null;
            LastBindError = string.Empty;
            TryBind();
        }

        private void TryBind()
        {
            LastBindError = string.Empty;
            receiveHub = FreeDUdpReceiveHub.Acquire(bindAddress, listenPort, joinMulticastGroup, multicastGroupIpAddress, multicastInterfaceAddress, HandleReceivedPacket, out var bindError);
            if (receiveHub == null)
            {
                LastBindError = bindError ?? string.Empty;
                Debug.LogWarning($"SyncFreeD input source failed to bind: {LastBindError}", this);
            }
            else if (!string.IsNullOrWhiteSpace(bindError))
            {
                LastBindError = bindError;
                Debug.LogWarning($"SyncFreeD input source multicast join failed. Unicast receive remains available: {bindError}", this);
            }
        }

        public void ApplyProfile()
        {
            if (inputProfileAsset == null || inputProfileAsset.Value == null)
            {
                return;
            }

            var profile = inputProfileAsset.Value;
            listenPort = profile.ListenPort;
            bindAddress = profile.BindAddress ?? string.Empty;
            validateChecksum = profile.ValidateChecksum;
            cameraIdFilter = profile.CameraIdFilter;
            joinMulticastGroup = profile.JoinMulticastGroup;
            multicastGroupIpAddress = profile.MulticastGroupIpAddress ?? "239.0.0.1";
            multicastInterfaceAddress = profile.MulticastInterfaceAddress ?? string.Empty;
        }

        private ICameraFrameProvider ResolveCommandSource()
        {
            if (commandSourceBehaviour is ICameraFrameProvider commandSource && !ReferenceEquals(commandSource, this))
            {
                return commandSource;
            }

            return null;
        }

        private void HandleReceivedPacket(byte[] packet, IPEndPoint remoteEndPoint)
        {
            if (!packetParser.TryParse(packet, validateChecksum, out var frame, out var failureReason))
            {
                RecordDroppedPacket(packet, remoteEndPoint, failureReason);
                return;
            }

            if (cameraIdFilter >= 0 && frame.CameraId != cameraIdFilter)
            {
                RecordDroppedPacket(packet, remoteEndPoint, FreeDPacketFailureReason.CameraIdFiltered);
                return;
            }

            frame.SourceId = sourceId;
            lastFrame = frame;
            LastPacketHex = BitConverter.ToString(packet);
            LastPacketLength = packet.Length;
            LastRemoteEndpoint = remoteEndPoint.ToString();
            ReceivedCount++;
            ObservedFrameUpdated?.Invoke(lastFrame);
        }

        private void RecordDroppedPacket(byte[] packet, IPEndPoint remoteEndPoint, FreeDPacketFailureReason reason)
        {
            DroppedPacketCount++;
            LastDropReason = reason;
            LastDroppedPacketLength = packet != null ? packet.Length : 0;
            LastDroppedPacketHex = packet != null ? BitConverter.ToString(packet) : string.Empty;
            LastDroppedRemoteEndpoint = remoteEndPoint != null ? remoteEndPoint.ToString() : string.Empty;
        }
    }
}
