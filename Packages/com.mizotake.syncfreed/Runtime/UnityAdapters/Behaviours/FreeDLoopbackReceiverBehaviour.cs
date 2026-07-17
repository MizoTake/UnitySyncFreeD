using System;
using System.Net;
using System.Net.Sockets;
using MizoTake.SyncFreeD.ScriptableObjects;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class FreeDLoopbackReceiverBehaviour : MonoBehaviour
    {
        [SerializeField] private FreeDUdpInputProfileAsset inputProfileAsset;
        [SerializeField] private bool applyProfileOnEnable = true;
        private string bindAddress = "127.0.0.1";
        private int listenPort = 40000;

        private UdpClient udpClient;

        public byte[] LastPacket { get; private set; } = Array.Empty<byte>();
        public string LastPacketHex { get; private set; } = string.Empty;
        public int ReceivedCount { get; private set; }
        public string LastRemoteEndpoint { get; private set; } = string.Empty;
        public long LastReceivedAtUtcTicks { get; private set; }
        public bool IsBound => udpClient != null;
        public FreeDUdpInputProfileAsset InputProfileAsset => inputProfileAsset;
        public bool ApplyProfileOnEnable => applyProfileOnEnable;
        public string BindAddress => bindAddress;
        public int ListenPort => listenPort;

        private void Reset()
        {
            if (applyProfileOnEnable)
            {
                ApplyProfileValues();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (applyProfileOnEnable)
            {
                ApplyProfileValues();
            }

            if (Application.isPlaying && isActiveAndEnabled)
            {
                Rebind();
            }
        }
#endif

        private void OnEnable()
        {
            if (applyProfileOnEnable)
            {
                ApplyProfileValues();
            }

            TryBind();
        }

        private void TryBind()
        {
            try
            {
                var localAddress = string.IsNullOrWhiteSpace(bindAddress) ? IPAddress.Any : IPAddress.Parse(bindAddress);
                udpClient = new UdpClient(new IPEndPoint(localAddress, listenPort));
                udpClient.Client.Blocking = false;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"SyncFreeD loopback receiver failed to bind: {exception.Message}", this);
                udpClient = null;
            }
        }

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
                    LastPacket = udpClient.Receive(ref remoteEndPoint);
                    LastPacketHex = BitConverter.ToString(LastPacket);
                    LastRemoteEndpoint = remoteEndPoint.ToString();
                    LastReceivedAtUtcTicks = DateTime.UtcNow.Ticks;
                    ReceivedCount++;
                }
                catch (SocketException)
                {
                    break;
                }
            }
        }

        public void SetInputProfileAsset(FreeDUdpInputProfileAsset profileAsset, bool applyImmediately)
        {
            inputProfileAsset = profileAsset;
            if (applyImmediately)
            {
                ApplyProfile();
            }
        }

        public void ApplyProfile()
        {
            if (!ApplyProfileValues())
            {
                return;
            }

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
            bindAddress = profile.BindAddress ?? string.Empty;
            listenPort = profile.ListenPort;
            return true;
        }

        private void Rebind()
        {
            udpClient?.Dispose();
            udpClient = null;
            TryBind();
        }

        private void OnDisable()
        {
            udpClient?.Dispose();
            udpClient = null;
        }
    }
}
