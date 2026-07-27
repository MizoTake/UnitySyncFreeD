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
        [SerializeField] private FreeDPacketDecodingPreset packetDecodingPreset = FreeDPacketDecodingPreset.RawUnsigned24;
        [SerializeField] private string builtInPacketDecodingProfileId = string.Empty;
        [SerializeField] private FreeDPacketDecodingProfile customPacketDecodingProfile = new FreeDPacketDecodingProfile();
        [SerializeField] private MonoBehaviour commandSourceBehaviour;

        private readonly FreeDPacketParser packetParser = new FreeDPacketParser();
        private FreeDPacketDecodingProfile packetDecodingProfile = FreeDPacketDecodingProfiles.Create(FreeDPacketDecodingPreset.RawUnsigned24);
        private FreeDUdpReceiveHub receiveHub;
        private CameraObservedFrame lastFrame;

        public string SourceId => sourceId;
        public int CameraId => lastFrame.CameraId;
        public CameraCapabilities Capabilities => packetDecodingProfile != null ? packetDecodingProfile.Capabilities : CameraCapabilities.None;
        public string LastPacketHex { get; private set; } = string.Empty;
        public string LastRemoteEndpoint { get; private set; } = string.Empty;
        public string LastBindError { get; private set; } = string.Empty;
        public string LastProfileError { get; private set; } = string.Empty;
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
        public FreeDPacketDecodingPreset PacketDecodingPreset => packetDecodingPreset;
        public string BuiltInPacketDecodingProfileId => builtInPacketDecodingProfileId;
        public string EffectivePacketDecodingProfileName => packetDecodingProfile?.ProfileName ?? string.Empty;
        public event Action<CameraObservedFrame> ObservedFrameUpdated;

        private void Reset()
        {
            if (applyProfileOnEnable)
            {
                ApplyProfileValues();
            }
        }

        private void OnEnable()
        {
            if (applyProfileOnEnable)
            {
                ApplyProfileValues();
            }
            RefreshPacketDecodingProfile();
            TryBind();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (applyProfileOnEnable)
            {
                ApplyProfileValues();
            }
            RefreshPacketDecodingProfile();

            if (!Application.isPlaying || !isActiveAndEnabled)
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
            return lastFrame.Pose.TimestampTicks != 0L && lastFrame.Validity.IsLensValid;
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
            if (!ApplyProfileValues())
            {
                return;
            }
            RefreshPacketDecodingProfile();

            if (Application.isPlaying && isActiveAndEnabled)
            {
                Rebind();
            }
        }

        private bool ApplyProfileValues()
        {
            if (inputProfileAsset == null || inputProfileAsset.Value == null)
            {
                return false;
            }

            var profile = inputProfileAsset.Value;
            listenPort = profile.ListenPort;
            bindAddress = profile.BindAddress ?? string.Empty;
            validateChecksum = profile.ValidateChecksum;
            cameraIdFilter = profile.CameraIdFilter;
            joinMulticastGroup = profile.JoinMulticastGroup;
            multicastGroupIpAddress = profile.MulticastGroupIpAddress ?? "239.0.0.1";
            multicastInterfaceAddress = profile.MulticastInterfaceAddress ?? string.Empty;
            packetDecodingPreset = profile.PacketDecodingPreset;
            builtInPacketDecodingProfileId = profile.BuiltInPacketDecodingProfileId ?? string.Empty;
            customPacketDecodingProfile = profile.CustomPacketDecodingProfile;
            return true;
        }

        private void RefreshPacketDecodingProfile()
        {
            var candidate = packetDecodingPreset == FreeDPacketDecodingPreset.Custom && customPacketDecodingProfile != null ? customPacketDecodingProfile : FreeDPacketDecodingProfiles.Create(packetDecodingPreset, builtInPacketDecodingProfileId);
            if (packetDecodingPreset == FreeDPacketDecodingPreset.BuiltInDeviceProfile && candidate == null)
            {
                packetDecodingProfile = FreeDPacketDecodingProfiles.CreateFailClosedRaw();
                LastProfileError = $"Built-in D1 decoding profile id '{builtInPacketDecodingProfileId}' is not registered.";
                return;
            }
            if (FreeDPacketDecodingProfileValidator.TryValidate(candidate, out var profileError))
            {
                packetDecodingProfile = candidate;
                LastProfileError = string.Empty;
                return;
            }

            packetDecodingProfile = FreeDPacketDecodingProfiles.CreateFailClosedRaw();
            LastProfileError = profileError;
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
            if (!packetParser.TryParse(packet, packetDecodingProfile, validateChecksum, out var frame, out var failureReason))
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
